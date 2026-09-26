namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// The scenarios in menu order, and the documented keys that only an infrastructure failure or a redelivery produces,
/// which the summary reports as expected misses instead of gaps.
/// </summary>
public static class ScenarioCatalog
{
    // Work item: TASK-056 (FEAT-017), TASK-057 (FEAT-017), TASK-058 (FEAT-017), TASK-065 (FEAT-001)
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
        new Scenarios.SaleDiscountScenario()
    };

    /// <summary>Gets the keys a full run does not exercise on purpose.</summary>
    public static IReadOnlySet<string> ExpectedMisses { get; } = new HashSet<string>
    {
        "SAL-CRT-06",   // a redelivered queued command
        "SAL-OBW-02",   // an outbox write outside a transaction
        "SAL-RLY-07",   // a failed relay cycle
        "SAL-DSP-07",   // a failed dispatch
        "SAL-BUS-06",   // a retried delivery (only validation failures are provoked, and they fail fast)
        "USR-SED-06"    // the seed skip: the database is wiped, so the administrator is always created
    };
}
