using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TD-010 (FEAT-010)
/// <summary>
/// Contains integration tests for the <see cref="SaleRepository"/> class.
/// </summary>
public class SaleRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public SaleRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that a sale is read back with its items ordered by line number, whatever order they were stored in
    /// and whatever their ids.
    /// </summary>
    [Fact(DisplayName = "Given items stored out of line order When getting the sale Then items come ordered by line number")]
    public async Task Given_ItemsStoredOutOfLineOrder_When_GetById_Then_ItemsAreOrderedByLineNumber()
    {
        // Arrange
        // Stored in this order; ids ascend while line numbers do not, so neither id nor storage order matches.
        var lines = new[]
        {
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000001"), LineNumber: 3),
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000002"), LineNumber: 1),
            (Id: Guid.Parse("00000000-0000-0000-0000-000000000003"), LineNumber: 2)
        };
        var sale = Persisted.New<Sale>(new
        {
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Acme Market",
            BranchId = Guid.NewGuid(),
            BranchName = "Downtown",
            TotalAmount = 30m
        });

        await using (var context = _fixture.CreateContext())
        {
            context.Sales.Add(sale);
            await context.SaveChangesAsync();
        }

        // One save per item keeps the stored order; a single save would let EF insert them sorted by key.
        foreach (var line in lines)
        {
            await using var context = _fixture.CreateContext();
            context.Set<SaleItem>().Add(Persisted.New<SaleItem>(new
            {
                Id = line.Id,
                LineNumber = line.LineNumber,
                SaleId = sale.Id,
                ProductId = Guid.NewGuid(),
                ProductDescription = "Beer 350ml",
                UnitPrice = 10m,
                Quantity = 1,
                TotalAmount = 10m
            }));
            await context.SaveChangesAsync();
        }

        // Act
        Sale? loaded;
        await using (var context = _fixture.CreateContext())
        {
            loaded = await new SaleRepository(context).GetByIdAsync(sale.Id);
        }

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(new[] { 1, 2, 3 }, loaded.Items.Select(i => i.LineNumber));
    }

    // Work item: TASK-037 (FEAT-006), TD-039
    /// <summary>
    /// Tests that a sale created with a preset id is stored under that id instead of the column default.
    /// </summary>
    [Fact(DisplayName = "Given a preset sale id When creating the sale Then the stored sale has that id")]
    public async Task Given_PresetSaleId_When_Creating_Then_StoredSaleHasThatId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sale = Persisted.New<Sale>(new
        {
            Id = id,
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Acme Market",
            BranchId = Guid.NewGuid(),
            BranchName = "Downtown",
            TotalAmount = 10m,
            Items = new List<SaleItem>
            {
                Persisted.New<SaleItem>(new
                {
                    LineNumber = 1,
                    ProductId = Guid.NewGuid(),
                    ProductDescription = "Beer 350ml",
                    UnitPrice = 10m,
                    Quantity = 1,
                    TotalAmount = 10m
                })
            }
        });

        // Act
        await using (var context = _fixture.CreateContext())
        {
            await new SaleRepository(context).CreateAsync(sale);
        }

        // Assert
        await using (var context = _fixture.CreateContext())
        {
            var loaded = await new SaleRepository(context).GetByIdAsync(id);
            Assert.NotNull(loaded);
            Assert.Equal(id, loaded.Id);
            Assert.Single(loaded.Items);
        }
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that a stored sale changed by SyncItems saves the kept item's change, deletes the removed item, and inserts
    /// the added one: EF sees the item list through the aggregate's private field.
    /// </summary>
    [Fact(DisplayName = "Given a stored sale When its items are synced and saved Then the stored items follow the lines")]
    public async Task Given_StoredSale_When_ItemsSyncedAndSaved_Then_StoredItemsFollowTheLines()
    {
        // Arrange
        var sale = Sale.Create(Guid.Empty, DateTime.UtcNow, Guid.NewGuid(), "Acme Market", Guid.NewGuid(), "Downtown",
        [
            new SaleLine(null, Guid.NewGuid(), "Beer 350ml", 10m, 1, null, false),
            new SaleLine(null, Guid.NewGuid(), "Soda 2L", 5m, 2, null, false)
        ]);
        await using (var context = _fixture.CreateContext())
        {
            await new SaleRepository(context).CreateAsync(sale);
        }

        Guid sodaId;
        Guid beerId;

        // Act
        await using (var context = _fixture.CreateContext())
        {
            var repository = new SaleRepository(context);
            var loaded = await repository.GetByIdAsync(sale.Id);
            Assert.NotNull(loaded);
            var soda = loaded.Items.Single(item => item.ProductDescription == "Soda 2L");
            sodaId = soda.Id;
            beerId = loaded.Items.Single(item => item.ProductDescription == "Beer 350ml").Id;
            loaded.SyncItems(
            [
                new SaleLine(soda.Id, soda.ProductId, soda.ProductDescription, soda.UnitPrice, 3, null, false),
                new SaleLine(null, Guid.NewGuid(), "Water 500ml", 3m, 1, null, false)
            ]);
            await repository.UpdateAsync(loaded);
        }

        // Assert
        await using (var context = _fixture.CreateContext())
        {
            var reloaded = await new SaleRepository(context).GetByIdAsync(sale.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(new[] { "Soda 2L", "Water 500ml" }, reloaded.Items.Select(item => item.ProductDescription));
            Assert.Equal(new[] { 1, 2 }, reloaded.Items.Select(item => item.LineNumber));
            Assert.Equal(3, reloaded.Items[0].Quantity);
            Assert.Equal(sodaId, reloaded.Items[0].Id);
            Assert.DoesNotContain(reloaded.Items, item => item.Id == beerId);
        }
    }
}
