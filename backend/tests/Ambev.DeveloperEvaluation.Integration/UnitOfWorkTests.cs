using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TD-006
/// <summary>
/// Contains integration tests for the explicit transactions of <see cref="UnitOfWork"/>.
/// </summary>
public class UnitOfWorkTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public UnitOfWorkTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    // Work item: TD-006, FEAT-012
    /// <summary>
    /// Tests that a repository write inside a rolled back transaction is not stored.
    /// </summary>
    [Fact(DisplayName = "Given a write inside a transaction When rolling back Then nothing is stored")]
    public async Task Given_WriteInsideTransaction_When_RollingBack_Then_NothingIsStored()
    {
        // Arrange
        var name = $"td006-rollback-{Guid.NewGuid():N}";
        await using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);
        await unitOfWork.BeginTransactionAsync();
        await new CustomerRepository(context).CreateAsync(new Customer { Name = name, Document = TestDocuments.Next() });

        // Act
        await unitOfWork.RollbackTransactionAsync();

        // Assert
        Assert.False(await CustomerExistsAsync(name));
    }

    // Work item: TD-006, FEAT-012
    /// <summary>
    /// Tests that a repository write inside a committed transaction is stored.
    /// </summary>
    [Fact(DisplayName = "Given a write inside a transaction When committing Then it is stored")]
    public async Task Given_WriteInsideTransaction_When_Committing_Then_ItIsStored()
    {
        // Arrange
        var name = $"td006-commit-{Guid.NewGuid():N}";
        await using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);
        await unitOfWork.BeginTransactionAsync();
        await new CustomerRepository(context).CreateAsync(new Customer { Name = name, Document = TestDocuments.Next() });

        // Act
        await unitOfWork.CommitTransactionAsync();

        // Assert
        Assert.True(await CustomerExistsAsync(name));
    }

    private async Task<bool> CustomerExistsAsync(string name)
    {
        await using var context = _fixture.CreateContext();
        return await context.Customers.AnyAsync(customer => customer.Name == name);
    }
}
