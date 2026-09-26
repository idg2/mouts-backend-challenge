using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: FEAT-012
/// <summary>
/// Contains integration tests for the document lookup and uniqueness in <see cref="CustomerRepository"/>.
/// </summary>
public class CustomerRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public CustomerRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that a customer is found by its document.
    /// </summary>
    [Fact(DisplayName = "Given a stored customer When getting by its document Then returns it")]
    public async Task Given_StoredCustomer_When_GettingByDocument_Then_ReturnsIt()
    {
        // Arrange
        var stored = await StoreAsync(TestDocuments.Next());

        // Act
        await using var context = _fixture.CreateContext();
        var found = await new CustomerRepository(context).GetByDocumentAsync(stored.Document);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(stored.Id, found.Id);
    }

    /// <summary>
    /// Tests that a create racing past the handler check still fails as a duplicate entry, not as a database error.
    /// </summary>
    [Fact(DisplayName = "Given a document already stored When creating a customer with it Then throws duplicate entry exception")]
    public async Task Given_DocumentAlreadyStored_When_Creating_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var stored = await StoreAsync(TestDocuments.Next());
        await using var context = _fixture.CreateContext();
        var repository = new CustomerRepository(context);

        // Act
        var act = () => repository.CreateAsync(new Customer { Name = "Other", Document = stored.Document });

        // Assert
        var exception = await Assert.ThrowsAsync<DuplicateEntryException>(act);
        Assert.Equal($"Customer with document {stored.Document} already exists", exception.Message);
    }

    /// <summary>
    /// Tests that an update racing past the handler check still fails as a duplicate entry, not as a database error.
    /// </summary>
    [Fact(DisplayName = "Given a document used by another customer When updating to it Then throws duplicate entry exception")]
    public async Task Given_DocumentUsedByAnotherCustomer_When_Updating_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var other = await StoreAsync(TestDocuments.Next());
        var customer = await StoreAsync(TestDocuments.Next());
        await using var context = _fixture.CreateContext();
        var repository = new CustomerRepository(context);
        var tracked = await repository.GetByIdAsync(customer.Id);
        tracked!.Document = other.Document;

        // Act
        var act = () => repository.UpdateAsync(tracked);

        // Assert
        await Assert.ThrowsAsync<DuplicateEntryException>(act);
    }

    private async Task<Customer> StoreAsync(string document)
    {
        await using var context = _fixture.CreateContext();
        var customer = new Customer { Name = "Acme Market", Document = document };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }
}
