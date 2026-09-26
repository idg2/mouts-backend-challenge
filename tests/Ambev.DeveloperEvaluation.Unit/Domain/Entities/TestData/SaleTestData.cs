using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

// Work item: TD-042, TD-043
/// <summary>
/// Generates sales with the Bogus library. Sale has no public setter, so the faker builds the lines and the header and
/// the sale comes from <see cref="Sale.Create"/>. Each line has its own product and 1 to 20 units, within the default
/// policy maximum, and requests no discount.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker<SaleLine> LineFaker = new Faker<SaleLine>()
        .CustomInstantiator(faker => new SaleLine(
            ItemId: null,
            ProductId: faker.Random.Guid(),
            ProductDescription: faker.Commerce.ProductName(),
            UnitPrice: faker.Finance.Amount(0.01m, 1000m, 2),
            Quantity: faker.Random.Int(1, 20),
            RequestedDiscountPercentage: null,
            IsCancelled: false));

    /// <summary>
    /// Generates an unpriced sale with one to five lines; call <see cref="Sale.ApplyDiscounts"/> before validating it.
    /// </summary>
    public static Sale GenerateValidSale()
    {
        var faker = new Faker();
        return Sale.Create(
            faker.Random.Guid(),
            faker.Date.Recent().ToUniversalTime(),
            faker.Random.Guid(),
            faker.Person.FullName,
            faker.Random.Guid(),
            faker.Address.City(),
            LineFaker.Generate(faker.Random.Int(1, 5)));
    }
}
