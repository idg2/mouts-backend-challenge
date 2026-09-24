using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: FEAT-013
/// <summary>
/// Contains integration tests for the product code lookup and uniqueness in <see cref="ProductRepository"/>.
/// </summary>
public class ProductRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public ProductRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that a product is found by its code.
    /// </summary>
    [Fact(DisplayName = "Given a stored product When getting by its code Then returns it")]
    public async Task Given_StoredProduct_When_GettingByCode_Then_ReturnsIt()
    {
        // Arrange
        var stored = await StoreAsync(NewCode());

        // Act
        await using var context = _fixture.CreateContext();
        var found = await new ProductRepository(context).GetByCodeAsync(stored.Code);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(stored.Id, found.Id);
    }

    /// <summary>
    /// Tests that a create racing past the handler check still fails as a duplicate entry, not as a database error.
    /// </summary>
    [Fact(DisplayName = "Given a code already stored When creating a product with it Then throws duplicate entry exception")]
    public async Task Given_CodeAlreadyStored_When_Creating_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var stored = await StoreAsync(NewCode());
        await using var context = _fixture.CreateContext();
        var repository = new ProductRepository(context);

        // Act
        var act = () => repository.CreateAsync(new Product { Code = stored.Code, Description = "Other", UnitPrice = 5m });

        // Assert
        var exception = await Assert.ThrowsAsync<DuplicateEntryException>(act);
        Assert.Equal($"Product with code {stored.Code} already exists", exception.Message);
    }

    /// <summary>
    /// Tests that an update racing past the handler check still fails as a duplicate entry, not as a database error.
    /// </summary>
    [Fact(DisplayName = "Given a code used by another product When updating to it Then throws duplicate entry exception")]
    public async Task Given_CodeUsedByAnotherProduct_When_Updating_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var other = await StoreAsync(NewCode());
        var product = await StoreAsync(NewCode());
        await using var context = _fixture.CreateContext();
        var repository = new ProductRepository(context);
        var tracked = await repository.GetByIdAsync(product.Id);
        tracked!.Code = other.Code;

        // Act
        var act = () => repository.UpdateAsync(tracked);

        // Assert
        await Assert.ThrowsAsync<DuplicateEntryException>(act);
    }

    private async Task<Product> StoreAsync(string code)
    {
        await using var context = _fixture.CreateContext();
        var product = new Product { Code = code, Description = "Beer 350ml", UnitPrice = 10m };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static string NewCode() => $"BEER-{Guid.NewGuid():N}";
}
