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
        var sale = new Sale
        {
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Acme Market",
            BranchId = Guid.NewGuid(),
            BranchName = "Downtown",
            TotalAmount = 30m
        };

        await using (var context = _fixture.CreateContext())
        {
            context.Sales.Add(sale);
            await context.SaveChangesAsync();
        }

        // One save per item keeps the stored order; a single save would let EF insert them sorted by key.
        foreach (var line in lines)
        {
            await using var context = _fixture.CreateContext();
            context.Set<SaleItem>().Add(new SaleItem
            {
                Id = line.Id,
                LineNumber = line.LineNumber,
                SaleId = sale.Id,
                ProductId = Guid.NewGuid(),
                ProductDescription = "Beer 350ml",
                UnitPrice = 10m,
                Quantity = 1,
                TotalAmount = 10m
            });
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
}
