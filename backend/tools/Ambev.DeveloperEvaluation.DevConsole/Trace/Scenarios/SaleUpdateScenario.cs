using System.Text.Json;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// SAL-UPD: cancel one item (ItemCancelled), cancel the sale (SaleCancelled), an item of another sale (400),
/// an unknown sale (404); each event through the relay and the consumer.
/// </summary>
public sealed class SaleUpdateScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "sale-update";

    /// <inheritdoc />
    public string Description => "cancel an item, cancel the sale, foreign item id, unknown sale; events through the relay";

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var references = await SaleFixtures.CreateReferencesAsync(context);
        var sale = (await context.ReadJsonAsync(await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references)))).GetProperty("data");
        var other = (await context.ReadJsonAsync(await context.SendAsync(HttpMethod.Post, "/api/sales", SaleFixtures.SaleBody(references, items: 1)))).GetProperty("data");
        var saleId = sale.GetProperty("id").GetString()!;

        var items = sale.GetProperty("items").EnumerateArray().Select(item => Item(item, cancelled: false)).ToList();
        items[0] = Item(sale.GetProperty("items")[0], cancelled: true);
        await context.SendAsync(HttpMethod.Put, $"/api/sales/{saleId}", Body(sale, references, items, cancelled: false));
        await context.AwaitAsync("SAL-CON-04", ("saleId", saleId), ("eventType", "SaleModified"));
        await context.AwaitAsync("SAL-CON-04", ("saleId", saleId), ("eventType", "ItemCancelled"));

        await context.SendAsync(HttpMethod.Put, $"/api/sales/{saleId}", Body(sale, references, items, cancelled: true));
        await context.AwaitAsync("SAL-CON-04", ("saleId", saleId), ("eventType", "SaleCancelled"));

        var foreign = new List<object>(items) { Item(other.GetProperty("items")[0], cancelled: false) };
        await context.SendAsync(HttpMethod.Put, $"/api/sales/{saleId}", Body(sale, references, foreign, cancelled: true));
        await context.SendAsync(HttpMethod.Put, $"/api/sales/{Guid.NewGuid()}", Body(sale, references, items, cancelled: false));
    }

    private static object Item(JsonElement item, bool cancelled) => new
    {
        id = item.GetProperty("id").GetString(),
        productId = item.GetProperty("productId").GetString(),
        quantity = item.GetProperty("quantity").GetInt32(),
        discountPercentage = item.GetProperty("discountPercentage").GetDecimal(),
        discountAmount = item.GetProperty("discountAmount").GetDecimal(),
        totalAmount = item.GetProperty("totalAmount").GetDecimal(),
        isCancelled = cancelled
    };

    private static object Body(JsonElement sale, SaleReferences references, IReadOnlyList<object> items, bool cancelled) => new
    {
        customerId = references.CustomerId,
        branchId = references.BranchId,
        totalAmount = sale.GetProperty("totalAmount").GetDecimal(),
        isCancelled = cancelled,
        items
    };
}
