using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-025 (FEAT-011)
/// <summary>
/// Contains integration tests for the list query of the customer, branch, product, and sale repositories.
/// Each test tags its rows with a unique marker and filters on it, because the test class shares one database.
/// </summary>
public class RepositoryListQueryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public RepositoryListQueryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that the total counts the filtered rows and the page follows the requested order.
    /// </summary>
    [Fact(DisplayName = "Given a filter, an order, and the second page When listing products Then the total counts filtered rows and the page follows the order")]
    public async Task Given_FilterOrderAndSecondPage_When_ListingProducts_Then_TotalCountsFilteredRowsAndPageFollowsOrder()
    {
        // Arrange
        var marker = await SeedProductsAsync();
        var query = new ListQuery
        {
            Page = 2,
            Size = 2,
            Filters = [new("Description", FilterOperator.Like, $"{marker}%")],
            Order = [new("UnitPrice", true)]
        };

        // Act
        var (products, totalCount) = await ListProductsAsync(query);

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal([$"{marker}-1"], products.Select(product => product.Code));
    }

    /// <summary>
    /// Tests that a page past the filtered rows is empty and still reports their count.
    /// </summary>
    [Fact(DisplayName = "Given a page past the filtered rows When listing products Then returns an empty page with the filtered count")]
    public async Task Given_PagePastFilteredRows_When_ListingProducts_Then_ReturnsEmptyPageWithFilteredCount()
    {
        // Arrange
        var marker = await SeedProductsAsync();
        var query = new ListQuery { Page = 3, Size = 2, Filters = [new("Description", FilterOperator.Like, $"{marker}%")] };

        // Act
        var (products, totalCount) = await ListProductsAsync(query);

        // Assert
        Assert.Empty(products);
        Assert.Equal(3, totalCount);
    }

    // Work item: TASK-025 (FEAT-011), FEAT-012
    /// <summary>
    /// Tests that customers and branches are ordered by name when no order is requested.
    /// </summary>
    [Theory(DisplayName = "Given no order When listing customers or branches Then orders by name")]
    [InlineData("customers")]
    [InlineData("branches")]
    public async Task Given_NoOrder_When_ListingCustomersOrBranches_Then_OrdersByName(string resource)
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        string[] letters = ["e", "c", "a", "d", "b"];
        await using (var context = _fixture.CreateContext())
        {
            if (resource == "customers")
                context.Customers.AddRange(letters.Select(letter => new Customer { Name = $"{marker} {letter}", Document = TestDocuments.Next() }));
            else
                context.Branches.AddRange(letters.Select(letter => new Branch { Name = $"{marker} {letter}" }));
            await context.SaveChangesAsync();
        }
        var query = new ListQuery { Page = 1, Size = 10, Filters = [new("Name", FilterOperator.Like, $"{marker}%")] };

        // Act
        await using var listContext = _fixture.CreateContext();
        var names = resource == "customers"
            ? (await new CustomerRepository(listContext).ListAsync(query)).Items.Select(customer => customer.Name)
            : (await new BranchRepository(listContext).ListAsync(query)).Items.Select(branch => branch.Name);

        // Assert
        Assert.Equal(letters.Order().Select(letter => $"{marker} {letter}"), names);
    }

    /// <summary>
    /// Tests that sales are ordered by sale number when no order is requested.
    /// </summary>
    [Fact(DisplayName = "Given no order When listing sales Then orders by sale number")]
    public async Task Given_NoOrder_When_ListingSales_Then_OrdersBySaleNumber()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await using (var context = _fixture.CreateContext())
        {
            context.Sales.AddRange(NewSale(marker), NewSale(marker));
            await context.SaveChangesAsync();
        }
        var query = new ListQuery { Page = 1, Size = 10, Filters = [new("CustomerName", FilterOperator.Like, marker)] };

        // Act
        await using var listContext = _fixture.CreateContext();
        var (sales, totalCount) = await new SaleRepository(listContext).ListAsync(query);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.True(sales[0].SaleNumber < sales[1].SaleNumber);
    }

    private async Task<string> SeedProductsAsync()
    {
        var marker = Guid.NewGuid().ToString("N");
        await using var context = _fixture.CreateContext();
        context.Products.AddRange(
            new Product { Code = $"{marker}-1", Description = $"{marker} beer", UnitPrice = 5m },
            new Product { Code = $"{marker}-2", Description = $"{marker} beer", UnitPrice = 10m },
            new Product { Code = $"{marker}-3", Description = $"{marker} beer", UnitPrice = 20m },
            new Product { Code = $"{Guid.NewGuid():N}", Description = "other beer", UnitPrice = 30m });
        await context.SaveChangesAsync();
        return marker;
    }

    private async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListProductsAsync(ListQuery query)
    {
        await using var context = _fixture.CreateContext();
        return await new ProductRepository(context).ListAsync(query);
    }

    private static Sale NewSale(string marker) => new()
    {
        SaleDate = DateTime.UtcNow,
        CustomerId = Guid.NewGuid(),
        CustomerName = marker,
        BranchId = Guid.NewGuid(),
        BranchName = "Downtown",
        TotalAmount = 10m,
        Items =
        [
            new SaleItem
            {
                LineNumber = 1,
                ProductId = Guid.NewGuid(),
                ProductDescription = "Beer 350ml",
                UnitPrice = 10m,
                Quantity = 1,
                TotalAmount = 10m
            }
        ]
    };
}
