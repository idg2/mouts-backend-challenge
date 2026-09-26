using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

// Work item: TD-042
/// <summary>
/// Generates products with the Bogus library: an alphanumeric code, a product name, and a unit price in cents.
/// </summary>
public static class ProductTestData
{
    private static readonly Faker<Product> ProductFaker = new Faker<Product>()
        .RuleFor(product => product.Id, faker => faker.Random.Guid())
        .RuleFor(product => product.Code, faker => faker.Random.AlphaNumeric(8).ToUpperInvariant())
        .RuleFor(product => product.Description, faker => faker.Commerce.ProductName())
        .RuleFor(product => product.UnitPrice, faker => faker.Finance.Amount(0.01m, 1000m, 2));

    /// <summary>
    /// Generates a product that passes validation.
    /// </summary>
    public static Product GenerateValidProduct() => ProductFaker.Generate();
}
