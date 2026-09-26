using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-065 (FEAT-001), TD-032
/// <summary>
/// DSC-CRT, DSC-GET, DSC-LST, DSC-DIS: create a product policy, read it, an unknown id and the empty id, list with a
/// filter and an order, a range on validFrom, an unknown filter, a page size of zero, a start in the past, overlapping
/// tiers, and a Customer-role create (403). Then a second policy of the product, a disable with an unknown id (404,
/// nothing disabled), a Customer-role disable (403), the disable of both, and the product's list without and with
/// includeDisabled, plus an invalid includeDisabled (400). Policies are scoped to fresh product ids, so they never
/// price another scenario's sales.
/// </summary>
public sealed class DiscountPolicyScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "discount-policy";

    // Work item: TASK-065 (FEAT-001), TD-032
    /// <inheritdoc />
    public string Description => "create, get, list, start in the past, overlapping tiers, 403, disable (404, 200), includeDisabled";

    // Work item: TASK-065 (FEAT-001), TD-032
    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var productId = Guid.NewGuid();
        var created = await context.SendAsync(HttpMethod.Post, "/api/discount-policies",
            PolicyBody(productId, DateTime.UtcNow.AddMinutes(10), maxQuantity: 10, (2, null, 5m)));
        var id = (await context.ReadJsonAsync(created)).GetProperty("data").GetProperty("id").GetString();

        await context.SendAsync(HttpMethod.Get, $"/api/discount-policies/{id}");
        await context.SendAsync(HttpMethod.Get, $"/api/discount-policies/{Guid.NewGuid()}");
        await context.SendAsync(HttpMethod.Get, $"/api/discount-policies/{Guid.Empty}");

        var order = Uri.EscapeDataString("validFrom desc");
        await context.SendAsync(HttpMethod.Get, $"/api/discount-policies?productId={productId}&_order={order}&_page=1&_size=5");
        await context.SendAsync(HttpMethod.Get, "/api/discount-policies?_minValidFrom=2026-01-01&_maxValidFrom=2026-01-01");
        await context.SendAsync(HttpMethod.Get, "/api/discount-policies?noSuchField=1");
        await context.SendAsync(HttpMethod.Get, "/api/discount-policies?_size=0");

        await context.SendAsync(HttpMethod.Post, "/api/discount-policies",
            PolicyBody(productId, DateTime.UtcNow.AddMinutes(-1), maxQuantity: 10, (2, null, 5m)));
        await context.SendAsync(HttpMethod.Post, "/api/discount-policies",
            PolicyBody(productId, DateTime.UtcNow.AddMinutes(10), maxQuantity: 20, (4, 10, 10m), (10, 20, 20m)));

        // The API binds enums as numbers, so role and status are sent as the enum values.
        var email = $"dsc-customer-{context.RunId}@example.com";
        const string password = "Cust0mer@Pass";
        await context.SendAsync(HttpMethod.Post, "/api/users", new
        {
            username = $"dsc-customer-{context.RunId}",
            email,
            password,
            phone = "+5511999990005",
            role = UserRole.Customer,
            status = UserStatus.Active
        });
        var customerToken = await context.LoginAsync(email, password);
        await context.SendAsync(HttpMethod.Post, "/api/discount-policies",
            PolicyBody(productId, DateTime.UtcNow.AddMinutes(10), maxQuantity: 10, (2, null, 5m)), token: customerToken);

        // Disabling is all or nothing: an unknown id answers 404 and the known one stays active.
        var second = await context.SendAsync(HttpMethod.Post, "/api/discount-policies",
            PolicyBody(productId, DateTime.UtcNow.AddMinutes(20), maxQuantity: 10, (3, null, 7m)));
        var secondId = (await context.ReadJsonAsync(second)).GetProperty("data").GetProperty("id").GetString();
        await context.SendAsync(HttpMethod.Post, "/api/discount-policies/disable", new { ids = new[] { id, Guid.NewGuid().ToString() } });
        await context.SendAsync(HttpMethod.Post, "/api/discount-policies/disable", new { ids = new[] { id } }, token: customerToken);
        await context.SendAsync(HttpMethod.Post, "/api/discount-policies/disable", new { ids = new[] { id, secondId } });

        // The list hides both disabled policies unless includeDisabled=true, which shows them with disabledAt.
        await context.SendAsync(HttpMethod.Get, $"/api/discount-policies?productId={productId}");
        await context.SendAsync(HttpMethod.Get, $"/api/discount-policies?productId={productId}&includeDisabled=true");
        await context.SendAsync(HttpMethod.Get, $"/api/discount-policies?productId={productId}&includeDisabled=maybe");
    }

    /// <summary>
    /// Builds a create body for a product-scoped policy (no branch, no end).
    /// </summary>
    /// <param name="productId">The product scope, or null for every product</param>
    /// <param name="validFrom">The UTC start</param>
    /// <param name="maxQuantity">The most units of the product per sale</param>
    /// <param name="tiers">The tiers: minimum, maximum or null, and percentage</param>
    /// <returns>The body, shaped as CreateDiscountPolicyRequest</returns>
    public static object PolicyBody(Guid? productId, DateTime validFrom, int maxQuantity, params (int Min, int? Max, decimal Percentage)[] tiers) => new
    {
        productId,
        branchId = (Guid?)null,
        validFrom,
        validTo = (DateTime?)null,
        maxQuantityPerProduct = maxQuantity,
        tiers = tiers.Select(tier => new { minQuantity = tier.Min, maxQuantity = tier.Max, percentage = tier.Percentage }).ToList()
    };
}
