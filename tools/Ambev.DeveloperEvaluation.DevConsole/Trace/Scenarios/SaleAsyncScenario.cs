namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// SAL-ASY and SAL-BUS: a queued sale answers 202 and is stored by the worker, then read once the projection has it
/// (SAL-PRJ); a queued sale with an unknown product fails fast into the error queue (SAL-BUS-05, SAL-BUS-07).
/// </summary>
public sealed class SaleAsyncScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "sale-async";

    /// <inheritdoc />
    public string Description => "202, worker stores the sale, GET before and after; a rejected sale reaches the error queue";

    // Work item: TASK-058 (FEAT-017), TASK-059 (FEAT-017), TASK-078 (FEAT-003)
    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);

        var accepted = await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references),
            configure: request => request.Headers.Add("Prefer", "respond-async"));
        var saleId = (await context.ReadJsonAsync(accepted)).GetProperty("data").GetProperty("id").GetString()!;

        await context.SendAsync(HttpMethod.Get, $"/api/sales/{saleId}");
        await context.AwaitAsync("SAL-ASY-07", ("saleId", saleId));
        await context.AwaitAsync("SAL-PRJ-01", ("saleId", saleId), ("eventType", "SaleCreated"));
        await context.SendAsync(HttpMethod.Get, $"/api/sales/{saleId}");
        await context.AwaitAsync("SAL-CON-04", ("saleId", saleId), ("eventType", "SaleCreated"));

        var rejected = await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references, productOverride: Guid.NewGuid()),
            configure: request => request.Headers.Add("Prefer", "respond-async"));
        var rejectedId = (await context.ReadJsonAsync(rejected)).GetProperty("data").GetProperty("id").GetString()!;
        var queued = context.Find("SAL-BUS-01", ("saleId", rejectedId));
        var messageId = queued?.Values.First(value => value.Name == "messageId").Value;
        if (messageId is not null)
            await context.AwaitAsync("SAL-BUS-07", ("messageId", messageId));
        else
            context.Out.WriteLine($"!!! SAL-BUS-01 not seen for sale {rejectedId}; cannot wait for SAL-BUS-07");
        await context.SendAsync(HttpMethod.Get, $"/api/sales/{rejectedId}");
    }
}
