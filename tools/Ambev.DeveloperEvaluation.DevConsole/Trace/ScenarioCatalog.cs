namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-056 (FEAT-017), TD-030
/// <summary>
/// The scenarios in menu order, and the documented keys the summary reports as expected misses instead of gaps.
/// </summary>
public static class ScenarioCatalog
{
    // Work item: TASK-056 (FEAT-017), TASK-057 (FEAT-017), TASK-058 (FEAT-017), TASK-065 (FEAT-001), TD-030
    /// <summary>Gets the scenarios in the order the menu shows and "all" runs.</summary>
    public static IReadOnlyList<IScenario> All { get; } = new List<IScenario>
    {
        new Scenarios.ConventionsScenario(),
        new Scenarios.AuthScenario(),
        new Scenarios.UsersScenario(),
        new Scenarios.RegistryScenario(
            "customers", "/api/customers",
            context => new { name = $"Customer {context.RunId} {context.Faker.Name.FullName()} {context.Faker.Random.AlphaNumeric(4)}", document = context.NewCpf() },
            (context, current) => new { name = $"Customer {context.RunId} updated", document = current.GetProperty("document").GetString() },
            uniqueField: "document", filterField: "name"),
        new Scenarios.RegistryScenario(
            "branches", "/api/branches",
            context => new { name = $"Branch {context.RunId} {context.Faker.Address.City()} {context.Faker.Random.AlphaNumeric(4)}" },
            (context, current) => new { name = $"Branch {context.RunId} updated" },
            uniqueField: null, filterField: "name"),
        new Scenarios.RegistryScenario(
            "products", "/api/products",
            context => new { code = $"P-{context.RunId}-{context.Faker.Random.AlphaNumeric(4).ToUpperInvariant()}", description = $"Product {context.RunId} {context.Faker.Commerce.ProductName()}", unitPrice = 10m },
            (context, current) => new { code = current.GetProperty("code").GetString(), description = $"Product {context.RunId} updated", unitPrice = 12.5m },
            uniqueField: "code", filterField: "description"),
        new Scenarios.DiscountPolicyScenario(),
        new Scenarios.SaleCreateScenario(),
        new Scenarios.SaleAsyncScenario(),
        new Scenarios.SaleUpdateScenario(),
        new Scenarios.SaleDeleteScenario(),
        new Scenarios.SaleListScenario(),
        new Scenarios.SaleDiscountScenario(),
        new Scenarios.FailuresScenario()
    };

    // Work item: TASK-072 (FEAT-018), TASK-078 (FEAT-003), TD-030
    /// <summary>
    /// Gets the keys a full run does not exercise on purpose. None today: the failures scenario reaches the failure and
    /// redelivery paths, so any documented key a full run misses is a gap.
    /// </summary>
    public static IReadOnlySet<string> ExpectedMisses { get; } = new HashSet<string>();
}
