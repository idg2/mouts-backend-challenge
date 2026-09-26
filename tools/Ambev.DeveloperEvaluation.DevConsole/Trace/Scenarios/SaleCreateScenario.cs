namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// SAL-CRT end to end: the synchronous create, then the SaleCreated row through the outbox, the relay, the bus, and
/// the consumer; an invalid body; an unknown product.
/// </summary>
public sealed class SaleCreateScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "sale-create";

    /// <inheritdoc />
    public string Description => "synchronous create, SaleCreated through outbox, relay, and consumer; 400s";

    // Work item: TASK-058 (FEAT-017), TASK-065 (FEAT-001)
    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);

        var created = await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references));
        var saleId = (await context.ReadJsonAsync(created)).GetProperty("data").GetProperty("id").GetString()!;

        var row = await context.AwaitAsync("SAL-OBW-06", ("saleId", saleId), ("eventType", "SaleCreated"));
        var rowId = row?.Values.First(value => value.Name == "rowId").Value;
        if (rowId is not null)
            await context.AwaitAsync("SAL-DSP-05", ("rowId", rowId));
        await context.AwaitAsync("SAL-CON-04", ("saleId", saleId), ("eventType", "SaleCreated"));

        await context.SendAsync(HttpMethod.Post, "/api/sales",
            new { customerId = references.CustomerId, branchId = references.BranchId, items = Array.Empty<object>() });
        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references, productOverride: Guid.NewGuid()));
    }
}
