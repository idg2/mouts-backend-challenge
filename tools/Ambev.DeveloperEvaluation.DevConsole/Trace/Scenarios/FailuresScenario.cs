using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Ambev.DeveloperEvaluation.WebApi.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TD-030
/// <summary>
/// Walks the failure and redelivery paths no other scenario reaches. Six come from real triggers (the seed run again,
/// a direct outbox write, a command and an event sent twice, a row of an unknown type, an event that skipped the relay);
/// the failed relay cycle and the unhandled exception come from the faults <see cref="TraceHost"/> lets a scenario arm.
/// </summary>
public sealed class FailuresScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "failures";

    /// <inheritdoc />
    public string Description =>
        "seed skip, outbox write outside a transaction, redelivered command and event, unknown outbox row, failed relay cycle, retried delivery, unhandled exception";

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        await SeedAgainAsync(context);
        await WriteOutsideTransactionAsync(context);
        var (saleId, rowId) = await RedeliverCommandAsync(context);
        await RedeliverEventAsync(context, saleId, rowId);
        await UnknownRowAsync(context);
        await FailRelayCycleAsync(context);
        await RetryDeliveryAsync(context);
        await FailRequestAsync(context);
    }

    // USR-SED-06: the administrator the host start seeded already exists.
    private static async Task SeedAgainAsync(ScenarioContext context)
    {
        Step(context, "AdminSeeder.SeedAsync again, after the start already seeded the administrator");
        await using var scope = context.Services.CreateAsyncScope();
        var settings = scope.ServiceProvider.GetRequiredService<AdminSeedSettings>();
        await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync(settings, CancellationToken.None);
        await context.AwaitAsync("USR-SED-06");
    }

    // SAL-OBW-02: an outbox write with no command transaction around it.
    private static async Task WriteOutsideTransactionAsync(ScenarioContext context)
    {
        Step(context, "IOutbox.EnqueueAsync outside a transaction");
        await using var scope = context.Services.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<IOutbox>().EnqueueAsync(new SaleDeleted(Guid.NewGuid()));
            context.Out.WriteLine("!!! the outbox accepted a write outside a transaction");
        }
        catch (InvalidOperationException exception)
        {
            context.Out.WriteLine($"<- {exception.GetType().Name}: {exception.Message}");
        }

        await context.AwaitAsync("SAL-OBW-02");
    }

    // SAL-CRT-06: the queued command of a stored sale arrives again.
    private static async Task<(string SaleId, string? RowId)> RedeliverCommandAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);
        var accepted = await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references),
            configure: request => request.Headers.Add("Prefer", "respond-async"));
        var saleId = (await context.ReadJsonAsync(accepted)).GetProperty("data").GetProperty("id").GetString()!;
        await context.AwaitAsync("SAL-ASY-07", ("saleId", saleId));
        var dispatched = await context.AwaitAsync("SAL-DSP-03", ("saleId", saleId), ("eventType", "SaleCreated"));
        await context.AwaitAsync("SAL-PRJ-01", ("saleId", saleId), ("eventType", "SaleCreated"));

        // The same lines SaleFixtures.SaleBody sent, now with the id the 202 answered.
        var command = new CreateSaleCommand
        {
            Id = Guid.Parse(saleId),
            CustomerId = references.CustomerId,
            BranchId = references.BranchId,
            Items = references.ProductIds.Select(productId => new CreateSaleItemInput { ProductId = productId, Quantity = 2 }).ToList()
        };
        Step(context, $"the same CreateSaleCommand sent on the bus again, sale {saleId}");
        await context.Services.GetRequiredService<IBus>().SendLocal(command);
        await context.AwaitAsync("SAL-CRT-06", ("saleId", saleId));
        return (saleId, dispatched?.Values.First(value => value.Name == "rowId").Value);
    }

    // SAL-PRJ-02: the SaleCreated the read model already holds arrives again.
    private static async Task RedeliverEventAsync(ScenarioContext context, string saleId, string? rowId)
    {
        if (rowId is null)
        {
            context.Out.WriteLine($"!!! SAL-DSP-03 not seen for sale {saleId}; cannot send its SaleCreated again");
            return;
        }

        Step(context, $"ProcessedAt cleared on outbox row {rowId}, so the relay sends SaleCreated of sale {saleId} again");
        await using var scope = context.Services.CreateAsyncScope();
        var id = Guid.Parse(rowId);
        await scope.ServiceProvider.GetRequiredService<DefaultContext>().OutboxMessages
            .Where(message => message.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(message => message.ProcessedAt, (DateTime?)null));
        await context.AwaitAsync("SAL-PRJ-02", ("saleId", saleId));
    }

    // SAL-DSP-07: a pending row whose type is not registered holds the relay until it is removed.
    private static async Task UnknownRowAsync(ScenarioContext context)
    {
        var row = new OutboxMessage { Id = Guid.NewGuid(), Type = $"TraceUnknownEvent{context.RunId}", Payload = "{}", OccurredAt = DateTime.UtcNow };
        Step(context, $"outbox row {row.Id} of the unregistered type {row.Type}");
        await using var scope = context.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        db.OutboxMessages.Add(row);
        await db.SaveChangesAsync();
        await context.AwaitAsync("SAL-DSP-07", ("rowId", row.Id.ToString("D")));

        Step(context, $"outbox row {row.Id} deleted, so the later rows flow again");
        await db.OutboxMessages.Where(message => message.Id == row.Id).ExecuteDeleteAsync();
    }

    // SAL-RLY-07: a relay cycle fails outside the dispatch of a row.
    private static async Task FailRelayCycleAsync(ScenarioContext context)
    {
        Step(context, "the next outbox relay cycle fails (fault armed in the trace host)");
        context.Faults.FailNextRelayCycle();
        await context.AwaitAsync("SAL-RLY-07");
    }

    // SAL-BUS-06: an event sent on the bus without the outbox relay lacks the sequence header, so the projection
    // throws an InvalidOperationException, which is not a fail-fast exception and is retried until the error queue.
    private static async Task RetryDeliveryAsync(ScenarioContext context)
    {
        var saleId = Guid.NewGuid().ToString("D");
        Step(context, $"SaleCancelled of sale {saleId} sent on the bus directly, without the outbox sequence header");
        await context.Services.GetRequiredService<IBus>().SendLocal(new SaleCancelled(Guid.Parse(saleId)));
        var queued = await context.AwaitAsync("SAL-BUS-01", ("saleId", saleId));
        var messageId = queued?.Values.First(value => value.Name == "messageId").Value;
        if (messageId is not null)
            await context.AwaitAsync("SAL-BUS-07", ("messageId", messageId));
    }

    // CMN-RSP-10: an exception no middleware expects.
    private static async Task FailRequestAsync(ScenarioContext context)
    {
        Step(context, "the next controller action throws (fault armed in the trace host)");
        context.Faults.FailNextRequest();
        await context.SendAsync(HttpMethod.Get, "/api/branches");
        await context.AwaitAsync("CMN-RSP-10");
    }

    private static void Step(ScenarioContext context, string what)
    {
        context.Out.WriteLine();
        context.Out.WriteLine($"-> {what}");
    }
}
