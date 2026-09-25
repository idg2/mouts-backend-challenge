namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// SAL-DEL: delete a sale (SaleDeleted through the relay and the consumer), delete it again (404), a bad id (400).
/// </summary>
public sealed class SaleDeleteScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "sale-delete";

    /// <inheritdoc />
    public string Description => "delete, SaleDeleted through the relay, delete again, bad id";

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);
        var created = await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references));
        var saleId = (await context.ReadJsonAsync(created)).GetProperty("data").GetProperty("id").GetString()!;

        await context.SendAsync(HttpMethod.Delete, $"/api/sales/{saleId}");
        await context.AwaitAsync("SAL-CON-04", ("saleId", saleId), ("eventType", "SaleDeleted"));
        await context.SendAsync(HttpMethod.Delete, $"/api/sales/{saleId}");
        await context.SendAsync(HttpMethod.Delete, "/api/sales/not-a-guid");
    }
}
