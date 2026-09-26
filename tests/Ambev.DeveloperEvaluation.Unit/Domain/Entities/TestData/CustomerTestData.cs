using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;
using Bogus.Extensions.Brazil;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

// Work item: TD-042
/// <summary>
/// Generates customers with the Bogus library: a person's full name and a valid CPF without format symbols.
/// </summary>
public static class CustomerTestData
{
    private static readonly Faker<Customer> CustomerFaker = new Faker<Customer>()
        .RuleFor(customer => customer.Id, faker => faker.Random.Guid())
        .RuleFor(customer => customer.Name, faker => faker.Person.FullName)
        .RuleFor(customer => customer.Document, faker => faker.Person.Cpf(includeFormatSymbols: false));

    /// <summary>
    /// Generates a customer that passes validation.
    /// </summary>
    public static Customer GenerateValidCustomer() => CustomerFaker.Generate();
}
