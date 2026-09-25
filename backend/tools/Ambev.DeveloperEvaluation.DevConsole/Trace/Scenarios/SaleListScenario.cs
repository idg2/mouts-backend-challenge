namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// SAL-LST: three sales, then a filter, a range, an order, a page past the end, and a bad filter.
/// </summary>
public sealed class SaleListScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "sale-list";

    /// <inheritdoc />
    public string Description => "list with filters, _min/_max, order, page past the end, bad filter";

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);
        for (var index = 1; index <= 3; index++)
            await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references, items: index));

        var order = Uri.EscapeDataString("totalAmount desc");
        await context.SendAsync(HttpMethod.Get, $"/api/sales?customerId={references.CustomerId}&_order={order}&_page=1&_size=2");
        await context.SendAsync(HttpMethod.Get, $"/api/sales?customerId={references.CustomerId}&_minTotalAmount=30&_maxTotalAmount=60");
        await context.SendAsync(HttpMethod.Get, $"/api/sales?customerId={references.CustomerId}&_page=50&_size=10");
        await context.SendAsync(HttpMethod.Get, "/api/sales?noSuchField=1");
        await context.SendAsync(HttpMethod.Get, "/api/sales?_size=0");
    }
}
