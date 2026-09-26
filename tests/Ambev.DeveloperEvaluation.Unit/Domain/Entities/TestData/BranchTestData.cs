using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

// Work item: TD-042
/// <summary>
/// Generates branches with the Bogus library, named after a city.
/// </summary>
public static class BranchTestData
{
    private static readonly Faker<Branch> BranchFaker = new Faker<Branch>()
        .RuleFor(branch => branch.Id, faker => faker.Random.Guid())
        .RuleFor(branch => branch.Name, faker => faker.Address.City());

    /// <summary>
    /// Generates a branch that passes validation.
    /// </summary>
    public static Branch GenerateValidBranch() => BranchFaker.Generate();
}
