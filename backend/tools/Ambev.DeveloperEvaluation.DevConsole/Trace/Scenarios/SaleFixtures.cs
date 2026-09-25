namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// The references a sale needs: one customer, one branch, and three products, registered for this run.
/// </summary>
/// <param name="CustomerId">The customer</param>
/// <param name="BranchId">The branch</param>
/// <param name="ProductIds">Three products with unit price 10</param>
public sealed record SaleReferences(Guid CustomerId, Guid BranchId, IReadOnlyList<Guid> ProductIds);

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// Registers the references and builds sale bodies over them.
/// </summary>
public static class SaleFixtures
{
    /// <summary>
    /// Registers one customer, one branch, and three products.
    /// </summary>
    /// <param name="context">The scenario context</param>
    /// <returns>The registered references</returns>
    public static async Task<SaleReferences> CreateReferencesAsync(ScenarioContext context)
    {
        var suffix = context.Faker.Random.AlphaNumeric(4);
        var customerId = await CreateAsync(context, "/api/customers", new { name = $"Sale customer {context.RunId} {suffix}", document = context.NewCpf() });
        var branchId = await CreateAsync(context, "/api/branches", new { name = $"Sale branch {context.RunId} {suffix}" });
        var productIds = new List<Guid>();
        for (var index = 1; index <= 3; index++)
            productIds.Add(await CreateAsync(context, "/api/products",
                new { code = $"SALE-{context.RunId}-{suffix.ToUpperInvariant()}-{index}", description = $"Sale product {index}", unitPrice = 10m }));

        return new SaleReferences(customerId, branchId, productIds);
    }

    /// <summary>
    /// Builds a sale body with the given number of items, two units each, no discount.
    /// </summary>
    /// <param name="references">The references</param>
    /// <param name="items">How many of the three products to include</param>
    /// <param name="productOverride">Replaces the first item's product id, to provoke an unknown reference</param>
    /// <returns>The body, shaped as CreateSaleRequest</returns>
    public static object SaleBody(SaleReferences references, int items = 3, Guid? productOverride = null)
    {
        var lines = references.ProductIds.Take(items)
            .Select((productId, index) => new
            {
                productId = index == 0 && productOverride is Guid replacement ? replacement : productId,
                quantity = 2,
                discountPercentage = 0m,
                discountAmount = 0m,
                totalAmount = 20m
            })
            .ToList();
        return new { customerId = references.CustomerId, branchId = references.BranchId, totalAmount = 20m * lines.Count, items = lines };
    }

    private static async Task<Guid> CreateAsync(ScenarioContext context, string route, object body)
    {
        var response = await context.SendAsync(HttpMethod.Post, route, body);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{route} answered {(int)response.StatusCode}; the sale scenarios need it to succeed.");

        return Guid.Parse((await context.ReadJsonAsync(response)).GetProperty("data").GetProperty("id").GetString()!);
    }
}
