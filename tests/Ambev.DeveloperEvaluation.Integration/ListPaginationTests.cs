using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: BUG-008 (FEAT-010), TASK-077 (FEAT-003)
/// <summary>
/// Contains integration tests for the paging of the customer, branch, and product repositories.
/// </summary>
public class ListPaginationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public ListPaginationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that a page number whose offset does not fit an int returns an empty page with the total count.
    /// </summary>
    [Theory(DisplayName = "Given a page whose offset overflows an int When listing Then returns an empty page")]
    [InlineData("customers")]
    [InlineData("branches")]
    [InlineData("products")]
    public async Task Given_PageOffsetOverflowingInt_When_Listing_Then_ReturnsEmptyPage(string resource)
    {
        // Arrange
        await SeedOneAsync(resource);

        // Act
        var (count, totalCount) = await ListAsync(resource, int.MaxValue, 100);

        // Assert
        Assert.Equal(0, count);
        Assert.True(totalCount >= 1);
    }

    /// <summary>
    /// Tests that the first page still returns the stored rows.
    /// </summary>
    [Theory(DisplayName = "Given stored rows When listing the first page Then returns them")]
    [InlineData("customers")]
    [InlineData("branches")]
    [InlineData("products")]
    public async Task Given_StoredRows_When_ListingFirstPage_Then_ReturnsThem(string resource)
    {
        // Arrange
        await SeedOneAsync(resource);

        // Act
        var (count, totalCount) = await ListAsync(resource, 1, 100);

        // Assert
        Assert.True(count >= 1);
        Assert.Equal(totalCount, count);
    }

    // Work item: BUG-008 (FEAT-010), FEAT-013, FEAT-012, TASK-077 (FEAT-003)
    private async Task SeedOneAsync(string resource)
    {
        await using var context = _fixture.CreateContext();
        switch (resource)
        {
            case "customers":
                context.Customers.Add(new Customer { Name = "Acme Market", Document = TestDocuments.Next() });
                break;
            case "branches":
                context.Branches.Add(new Branch { Name = "Downtown" });
                break;
            case "products":
                context.Products.Add(new Product { Code = $"BEER-{Guid.NewGuid():N}", Description = "Beer 350ml", UnitPrice = 10m });
                break;
        }

        await context.SaveChangesAsync();
    }

    // Work item: TASK-025 (FEAT-011), TASK-077 (FEAT-003)
    private async Task<(int Count, int TotalCount)> ListAsync(string resource, int page, int size)
    {
        var query = new ListQuery { Page = page, Size = size };
        await using var context = _fixture.CreateContext();
        switch (resource)
        {
            case "customers":
                var customers = await new CustomerRepository(context).ListAsync(query);
                return (customers.Items.Count, customers.TotalCount);
            case "branches":
                var branches = await new BranchRepository(context).ListAsync(query);
                return (branches.Items.Count, branches.TotalCount);
            case "products":
                var products = await new ProductRepository(context).ListAsync(query);
                return (products.Items.Count, products.TotalCount);
            default:
                throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown resource");
        }
    }
}
