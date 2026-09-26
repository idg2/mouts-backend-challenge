using System.Net;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-065 (FEAT-001), TD-032, TD-043
/// <summary>
/// SAL-CRT-14 to 16 and SAL-UPD-18 to 20 over HTTP with the default policy, the same cases the functional tests prove:
/// every challenge boundary (3 units without a discount, 4 and 9 at 10%, 10 and 20 at 20%), a tier reached only by the
/// product total, two products each on its own total, a discount equal to the ceiling, one 0.01 above it, a discount
/// below four units, 21 units on one line and across two lines, and a cancellation that lowers the tier of the
/// remaining lines. Then a product policy that starts two seconds later takes precedence over the default for that
/// product; once it is disabled the default prices new sales of the product again, while a sale made under it stays
/// editable and keeps its rules. Each case prints whether the response matched; a mismatch is a !!! line.
/// </summary>
public sealed class SaleDiscountScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "sale-discount";

    // Work item: TASK-065 (FEAT-001), TD-032, TD-043
    /// <inheritdoc />
    public string Description => "every challenge boundary, tiers by product total, ceiling, limit, cancel with recalculation, product policy, disabled policy";

    // Work item: TASK-065 (FEAT-001), TD-032, TD-043
    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);
        var beer = references.ProductIds[0];
        var soda = references.ProductIds[1];
        var water = references.ProductIds[2];

        // Every boundary of the challenge rules, one sale each.
        foreach (var (quantity, percentage) in new[] { (3, 0m), (4, 10m), (9, 10m), (10, 20m), (20, 20m) })
            await ExpectDiscountAsync(context, await Post(context, references, (soda, quantity, null)), line: 0, percentage);

        // Three lines of four units: twelve in total, so every line gets 20%.
        var tiered = await Post(context, references, (beer, 4, null), (beer, 4, null), (beer, 4, null));
        await ExpectDiscountAsync(context, tiered, line: 2, 20m);
        var sale = (await context.ReadJsonAsync(tiered)).GetProperty("data");

        // Two products, each on its own total: ten beers get 20%, three sodas nothing.
        var mixed = await Post(context, references, (beer, 10, null), (soda, 3, null));
        await ExpectDiscountAsync(context, mixed, line: 0, 20m);
        await ExpectDiscountAsync(context, mixed, line: 1, 0m);

        // The ceiling itself is accepted; 0.01 above it is DiscountAboveAllowed; below four units the ceiling is 0.
        await ExpectDiscountAsync(context, await Post(context, references, (soda, 5, 10m)), line: 0, 10m);
        ExpectStatus(context, await Post(context, references, (soda, 5, 10.01m)), HttpStatusCode.BadRequest, "10.01% on 5 units");
        ExpectStatus(context, await Post(context, references, (soda, 3, 1m)), HttpStatusCode.BadRequest, "1% on 3 units");

        // Twenty-one units of one product, on one line and across two lines: QuantityLimitExceeded.
        ExpectStatus(context, await Post(context, references, (beer, 21, null)), HttpStatusCode.BadRequest, "21 units on one line");
        ExpectStatus(context, await Post(context, references, (beer, 12, null), (beer, 9, null)), HttpStatusCode.BadRequest, "21 units on two lines");

        // Cancelling the first line leaves eight units: the other two lines drop to 10%.
        var saleId = sale.GetProperty("id").GetString()!;
        var items = sale.GetProperty("items").EnumerateArray().ToList();
        var cancelled = await context.SendAsync(HttpMethod.Put, $"/api/sales/{saleId}", UpdateBody(references, items, cancelledLine: 0));
        await ExpectDiscountAsync(context, cancelled, line: 1, 10m);

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

        await ExpectDiscountAsync(context, await Post(context, references, (water, 3, null)), line: 0, 5m);
        ExpectStatus(context, await Post(context, references, (water, 6, null)), HttpStatusCode.BadRequest, "6 units above the water policy maximum");
        var underWaterPolicy = await Post(context, references, (water, 2, null), (water, 2, null));
        await ExpectDiscountAsync(context, underWaterPolicy, line: 0, 5m);

        // Once the water policy is disabled the default prices new water sales again: six units are allowed and get 10%.
        await context.SendAsync(HttpMethod.Post, "/api/discount-policies/disable", new { ids = new[] { waterPolicyId } });
        await ExpectDiscountAsync(context, await Post(context, references, (water, 6, null)), line: 0, 10m);

        // A sale made under the water policy stays editable and keeps it: cancelling one line leaves two units, which
        // the water policy prices at 5% (the default would give nothing below four units).
        var older = (await context.ReadJsonAsync(underWaterPolicy)).GetProperty("data");
        var olderItems = older.GetProperty("items").EnumerateArray().ToList();
        var edited = await context.SendAsync(HttpMethod.Put, $"/api/sales/{older.GetProperty("id").GetString()}",
            UpdateBody(references, olderItems, cancelledLine: 0));
        await ExpectDiscountAsync(context, edited, line: 1, 5m);
    }

    // Work item: TD-043
    private static Task<HttpResponseMessage> Post(
        ScenarioContext context, SaleReferences references, params (Guid ProductId, int Quantity, decimal? DiscountPercentage)[] lines) =>
        context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.LinesBody(references, lines));

    // Work item: TD-043
    // Prints whether a line of the stored sale carries the expected discount; a mismatch or a failed request is a !!! line.
    private static async Task ExpectDiscountAsync(ScenarioContext context, HttpResponseMessage response, int line, decimal expected)
    {
        decimal? actual = null;
        if (response.IsSuccessStatusCode)
            actual = (await context.ReadJsonAsync(response)).GetProperty("data").GetProperty("items")[line].GetProperty("discountPercentage").GetDecimal();

        context.Out.WriteLine(actual == expected
            ? $"... line {line + 1} got {expected}% as expected"
            : $"!!! line {line + 1} expected {expected}%, got {(actual is null ? $"status {(int)response.StatusCode}" : $"{actual}%")}");
    }

    // Work item: TD-043
    // Prints whether a request answered the expected status; a mismatch is a !!! line.
    private static void ExpectStatus(ScenarioContext context, HttpResponseMessage response, HttpStatusCode expected, string what) =>
        context.Out.WriteLine(response.StatusCode == expected
            ? $"... {what}: {(int)expected} as expected"
            : $"!!! {what}: expected {(int)expected}, got {(int)response.StatusCode}");

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
