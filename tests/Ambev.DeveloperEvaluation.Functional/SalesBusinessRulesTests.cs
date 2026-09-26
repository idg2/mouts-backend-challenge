using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

// Work item: TD-043
/// <summary>
/// Proves the sales rules of the challenge through HTTP, against the whole API and the seeded default discount policy:
/// 4 or more identical items get 10%, 10 to 20 get 20%, more than 20 are rejected, and below 4 there is no discount.
/// Every expected amount is written out by hand; nothing is computed with production code.
/// </summary>
public class SalesBusinessRulesTests : IClassFixture<SalesApiFixture>
{
    private readonly SalesApiFixture _api;

    public SalesBusinessRulesTests(SalesApiFixture api)
    {
        _api = api;
    }

    // Work item: TD-043
    [Theory(DisplayName = "Given identical items at 10.00 When creating a sale Then the tier of the quantity sets the discount and the totals")]
    [InlineData(1, 0, 0, 10)]
    [InlineData(3, 0, 0, 30)]
    [InlineData(4, 10, 4, 36)]
    [InlineData(9, 10, 9, 81)]
    [InlineData(10, 20, 20, 80)]
    [InlineData(20, 20, 40, 160)]
    public async Task Given_IdenticalItems_When_CreatingSale_Then_TierSetsDiscountAndTotals(
        int quantity, decimal percentage, decimal discountAmount, decimal totalAmount)
    {
        // Arrange
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(10m);

        // Act
        var response = await _api.Client.PostAsJsonAsync("/api/sales", Sale(customerId, branchId, (productId, quantity, null)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await DataAsync(response);
        var item = sale.GetProperty("items")[0];
        Assert.Equal(percentage, item.GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(discountAmount, item.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(totalAmount, item.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(totalAmount, sale.GetProperty("totalAmount").GetDecimal());
    }

    // Work item: TD-043
    [Fact(DisplayName = "Given identical items split across lines When creating a sale Then the tier comes from their total")]
    public async Task Given_IdenticalItemsAcrossLines_When_CreatingSale_Then_TierComesFromTotal()
    {
        // Arrange
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(10m);

        // Act
        var response = await _api.Client.PostAsJsonAsync("/api/sales",
            Sale(customerId, branchId, (productId, 4, null), (productId, 4, null), (productId, 4, null)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await DataAsync(response);
        foreach (var item in sale.GetProperty("items").EnumerateArray())
        {
            Assert.Equal(20m, item.GetProperty("discountPercentage").GetDecimal());
            Assert.Equal(8m, item.GetProperty("discountAmount").GetDecimal());
            Assert.Equal(32m, item.GetProperty("totalAmount").GetDecimal());
        }

        Assert.Equal(96m, sale.GetProperty("totalAmount").GetDecimal());
    }

    // Work item: TD-043
    [Theory(DisplayName = "Given more than 20 identical items When creating a sale Then 400 QuantityLimitExceeded")]
    [InlineData(new[] { 21 })]
    [InlineData(new[] { 12, 9 })]
    public async Task Given_MoreThanTwentyIdenticalItems_When_CreatingSale_Then_QuantityLimitExceeded(int[] quantities)
    {
        // Arrange
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(10m);
        var lines = quantities.Select(quantity => (productId, quantity, (decimal?)null)).ToArray();

        // Act
        var response = await _api.Client.PostAsJsonAsync("/api/sales", Sale(customerId, branchId, lines));

        // Assert
        await AssertValidationErrorAsync(response, "QuantityLimitExceeded");
    }

    // Work item: TD-043
    [Theory(DisplayName = "Given a requested discount When creating a sale Then up to the tier ceiling is applied and above it is 400 DiscountAboveAllowed")]
    [InlineData(5, 5, true)]
    [InlineData(5, 10, true)]
    [InlineData(5, 10.01, false)]
    [InlineData(3, 1, false)]
    public async Task Given_RequestedDiscount_When_CreatingSale_Then_CeilingHolds(int quantity, decimal requested, bool accepted)
    {
        // Arrange
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(10m);

        // Act
        var response = await _api.Client.PostAsJsonAsync("/api/sales", Sale(customerId, branchId, (productId, quantity, requested)));

        // Assert
        if (!accepted)
        {
            await AssertValidationErrorAsync(response, "DiscountAboveAllowed");
            return;
        }

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await DataAsync(response)).GetProperty("items")[0];
        Assert.Equal(requested, item.GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(10m, item.GetProperty("discountCeilingPercentage").GetDecimal());
    }

    // Work item: TD-043
    [Fact(DisplayName = "Given two products When creating a sale Then each is priced on its own total and the sale sums the items")]
    public async Task Given_TwoProducts_When_CreatingSale_Then_EachPricedOnItsOwnTotal()
    {
        // Arrange
        var (customerId, branchId, beer) = await _api.CreateReferencesAsync(10m);
        var soda = await _api.CreateProductAsync(7.5m);

        // Act
        var response = await _api.Client.PostAsJsonAsync("/api/sales", Sale(customerId, branchId, (beer, 10, null), (soda, 3, null)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await DataAsync(response);
        var items = sale.GetProperty("items");
        Assert.Equal(20m, items[0].GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(80m, items[0].GetProperty("totalAmount").GetDecimal());
        Assert.Equal(0m, items[1].GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(22.5m, items[1].GetProperty("totalAmount").GetDecimal());
        Assert.Equal(102.5m, sale.GetProperty("totalAmount").GetDecimal());
    }

    // Work item: TD-043
    [Fact(DisplayName = "Given catalog references When creating a sale Then names, description, and unit price are copied from the catalog")]
    public async Task Given_CatalogReferences_When_CreatingSale_Then_ValuesCopiedFromCatalog()
    {
        // Arrange
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(12.34m);
        var customer = await DataAsync(await _api.Client.GetAsync($"/api/customers/{customerId}"));
        var branch = await DataAsync(await _api.Client.GetAsync($"/api/branches/{branchId}"));
        var product = await DataAsync(await _api.Client.GetAsync($"/api/products/{productId}"));

        // Act
        var response = await _api.Client.PostAsJsonAsync("/api/sales", Sale(customerId, branchId, (productId, 2, null)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await DataAsync(response);
        Assert.Equal(customer.GetProperty("name").GetString(), sale.GetProperty("customerName").GetString());
        Assert.Equal(branch.GetProperty("name").GetString(), sale.GetProperty("branchName").GetString());
        var item = sale.GetProperty("items")[0];
        Assert.Equal(product.GetProperty("description").GetString(), item.GetProperty("productDescription").GetString());
        Assert.Equal(12.34m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(24.68m, sale.GetProperty("totalAmount").GetDecimal());
    }

    // Work item: TD-043
    [Fact(DisplayName = "Given three lines of four identical items When one is cancelled Then the others drop to 10% and leave it out of the total")]
    public async Task Given_ThreeLinesOfFour_When_OneCancelled_Then_OthersDropToTenPercent()
    {
        // Arrange
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(10m);
        var created = await DataAsync(await _api.Client.PostAsJsonAsync("/api/sales",
            Sale(customerId, branchId, (productId, 4, null), (productId, 4, null), (productId, 4, null))));
        var saleId = created.GetProperty("id").GetGuid();
        var items = created.GetProperty("items").EnumerateArray()
            .Select((item, index) => new
            {
                id = item.GetProperty("id").GetGuid(),
                productId,
                quantity = 4,
                discountPercentage = (decimal?)null,
                isCancelled = index == 0
            })
            .ToList();

        // Act
        var response = await _api.Client.PutAsJsonAsync($"/api/sales/{saleId}",
            new { customerId, branchId, isCancelled = false, items });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sale = await DataAsync(response);
        var updated = sale.GetProperty("items");
        Assert.True(updated[0].GetProperty("isCancelled").GetBoolean());
        Assert.Equal(20m, updated[0].GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(10m, updated[1].GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(36m, updated[1].GetProperty("totalAmount").GetDecimal());
        Assert.Equal(10m, updated[2].GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(72m, sale.GetProperty("totalAmount").GetDecimal());
    }

    // Work item: TD-043
    [Fact(DisplayName = "Given a policy for one product When selling it Then its own cap and tier apply and another product keeps the challenge rules")]
    public async Task Given_ProductPolicy_When_Selling_Then_OwnRulesApplyAndOthersKeepDefault()
    {
        // Arrange
        var (customerId, branchId, beer) = await _api.CreateReferencesAsync(10m);
        var soda = await _api.CreateProductAsync(10m);
        var validFrom = DateTime.UtcNow.AddSeconds(1);
        var policy = await _api.Client.PostAsJsonAsync("/api/discount-policies", new
        {
            productId = beer,
            branchId = (Guid?)null,
            validFrom,
            maxQuantityPerProduct = 50,
            tiers = new[] { new { minQuantity = 12, maxQuantity = (int?)null, percentage = 30m } }
        });
        Assert.Equal(HttpStatusCode.Created, policy.StatusCode);
        var policyId = (await DataAsync(policy)).GetProperty("id").GetGuid();
        await Task.Delay(validFrom - DateTime.UtcNow + TimeSpan.FromMilliseconds(200));

        // Act
        var response = await _api.Client.PostAsJsonAsync("/api/sales", Sale(customerId, branchId, (beer, 30, null), (soda, 10, null)));
        var aboveDefaultCap = await _api.Client.PostAsJsonAsync("/api/sales", Sale(customerId, branchId, (soda, 30, null)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var items = (await DataAsync(response)).GetProperty("items");
        Assert.Equal(policyId, items[0].GetProperty("discountPolicyId").GetGuid());
        Assert.Equal(30m, items[0].GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(210m, items[0].GetProperty("totalAmount").GetDecimal());
        Assert.NotEqual(policyId, items[1].GetProperty("discountPolicyId").GetGuid());
        Assert.Equal(20m, items[1].GetProperty("discountPercentage").GetDecimal());
        await AssertValidationErrorAsync(aboveDefaultCap, "QuantityLimitExceeded");
    }

    private static object Sale(Guid customerId, Guid branchId, params (Guid ProductId, int Quantity, decimal? DiscountPercentage)[] lines) => new
    {
        customerId,
        branchId,
        items = lines.Select(line => new { productId = line.ProductId, quantity = line.Quantity, discountPercentage = line.DiscountPercentage }).ToList()
    };

    private static async Task<JsonElement> DataAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string error)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ValidationError", body.GetProperty("type").GetString());
        Assert.Equal(error, body.GetProperty("error").GetString());
    }
}
