using System.Text.Json;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-065 (FEAT-001), TD-032
/// <summary>
/// SAL-CRT-14 to 16 and SAL-UPD-18 to 20 over HTTP with the default policy: a tier reached only by the product total, a
/// discount equal to the ceiling, one 0.01 above it, a discount below four units, 21 units across two lines, and a
/// cancellation that lowers the tier of the remaining lines. Then a product policy that starts two seconds later takes
/// precedence over the default for that product, until it is disabled and the default prices the product again.
/// </summary>
public sealed class SaleDiscountScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "sale-discount";

    // Work item: TASK-065 (FEAT-001), TD-032
    /// <inheritdoc />
    public string Description => "tiers by product total, ceiling, limit, cancel with recalculation, product policy, disabled policy";

    // Work item: TASK-065 (FEAT-001), TD-032
    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);
        var beer = references.ProductIds[0];
        var soda = references.ProductIds[1];
        var water = references.ProductIds[2];

        // Three lines of four units: twelve in total, so every line gets 20%.
        var sale = (await context.ReadJsonAsync(await context.SendAsync(HttpMethod.Post, "/api/sales",
            SaleFixtures.LinesBody(references, (beer, 4, null), (beer, 4, null), (beer, 4, null))))).GetProperty("data");

        // The ceiling itself is accepted; 0.01 above it is DiscountAboveAllowed; below four units the ceiling is 0.
        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, (soda, 5, 10m)));
        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, (soda, 5, 10.01m)));
        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, (soda, 3, 1m)));

        // Twenty-one units of one product across two lines: QuantityLimitExceeded on both lines.
        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, (beer, 12, null), (beer, 9, null)));

        // Cancelling the first line leaves eight units: the other two lines drop to 10%.
        var saleId = sale.GetProperty("id").GetString()!;
        var items = sale.GetProperty("items").EnumerateArray().ToList();
        await context.SendAsync(HttpMethod.Put, $"/api/sales/{saleId}", UpdateBody(references, items, cancelledLine: 0));

        // A water policy starting in two seconds (at most 5 units, 5% from 2 units) wins over the default once it starts.
        var startsAt = DateTime.UtcNow.AddSeconds(2);
        var waterPolicy = await context.SendAsync(HttpMethod.Post, "/api/discount-policies",
            DiscountPolicyScenario.PolicyBody(water, startsAt, maxQuantity: 5, (2, null, 5m)));
        var waterPolicyId = (await context.ReadJsonAsync(waterPolicy)).GetProperty("data").GetProperty("id").GetString();
        var wait = startsAt - DateTime.UtcNow + TimeSpan.FromMilliseconds(200);
        if (wait > TimeSpan.Zero)
        {
            context.Out.WriteLine($"... waiting {wait.TotalMilliseconds:0} ms for the water policy to start");
            await Task.Delay(wait);
        }

        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, (water, 3, null)));
        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, (water, 6, null)));

        // Once the water policy is disabled the default prices water again: six units are allowed and get 10%.
        await context.SendAsync(HttpMethod.Post, "/api/discount-policies/disable", new { ids = new[] { waterPolicyId } });
        await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, (water, 6, null)));
    }

    private static object UpdateBody(SaleReferences references, IReadOnlyList<JsonElement> items, int cancelledLine) => new
    {
        customerId = references.CustomerId,
        branchId = references.BranchId,
        isCancelled = false,
        items = items.Select((item, index) => new
        {
            id = item.GetProperty("id").GetString(),
            productId = item.GetProperty("productId").GetString(),
            quantity = item.GetProperty("quantity").GetInt32(),
            discountPercentage = (decimal?)null,
            isCancelled = index == cancelledLine
        }).ToList()
    };
}
