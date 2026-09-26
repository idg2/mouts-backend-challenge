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

    // Work item: TASK-058 (FEAT-017), TASK-065 (FEAT-001)
    /// <summary>
    /// Builds a sale body with the given number of items, two units each, and no requested discount: two units are
    /// below the first tier of the default policy, so every line is priced at 0% and totals 20.
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
                quantity = 2
            })
            .ToList();
        return new { customerId = references.CustomerId, branchId = references.BranchId, items = lines };
    }

    // Work item: TASK-065 (FEAT-001)
    /// <summary>
    /// Builds a sale body from explicit lines. A null discount is sent as null and asks for the ceiling.
    /// </summary>
    /// <param name="references">The customer and branch of the sale</param>
    /// <param name="lines">The product, quantity, and requested discount of each line</param>
    /// <returns>The body, shaped as CreateSaleRequest</returns>
    public static object LinesBody(SaleReferences references, params (Guid ProductId, int Quantity, decimal? DiscountPercentage)[] lines) => new
    {
        customerId = references.CustomerId,
        branchId = references.BranchId,
        items = lines
            .Select(line => new { productId = line.ProductId, quantity = line.Quantity, discountPercentage = line.DiscountPercentage })
            .ToList()
    };

    private static async Task<Guid> CreateAsync(ScenarioContext context, string route, object body)
    {
        var response = await context.SendAsync(HttpMethod.Post, route, body);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{route} answered {(int)response.StatusCode}; the sale scenarios need it to succeed.");

        return Guid.Parse((await context.ReadJsonAsync(response)).GetProperty("data").GetProperty("id").GetString()!);
    }
}
