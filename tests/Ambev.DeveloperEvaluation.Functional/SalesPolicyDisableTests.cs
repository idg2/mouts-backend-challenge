using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

// Work item: TD-043
/// <summary>
/// Proves that disabling a discount policy leaves the sales dated before the disable editable. It runs on its own
/// fixture, and so on its own databases, because it disables the seeded default policy.
/// </summary>
public class SalesPolicyDisableTests : IClassFixture<SalesApiFixture>
{
    private readonly SalesApiFixture _api;

    public SalesPolicyDisableTests(SalesApiFixture api)
    {
        _api = api;
    }

    // Work item: TD-043
    [Fact(DisplayName = "Given a sale priced by the default policy When the policy is disabled Then a line of the sale can still be cancelled")]
    public async Task Given_SaleUnderDefaultPolicy_When_PolicyDisabled_Then_LineCanStillBeCancelled()
    {
        // Arrange
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(10m);
        var created = await DataAsync(await _api.Client.PostAsJsonAsync("/api/sales", new
        {
            customerId,
            branchId,
            items = new[] { new { productId, quantity = 4 }, new { productId, quantity = 4 }, new { productId, quantity = 4 } }
        }));
        var policyId = created.GetProperty("items")[0].GetProperty("discountPolicyId").GetGuid();
        var disable = await _api.Client.PostAsJsonAsync("/api/discount-policies/disable", new { ids = new[] { policyId } });
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        var items = created.GetProperty("items").EnumerateArray()
            .Select((item, index) => new { id = item.GetProperty("id").GetGuid(), productId, quantity = 4, isCancelled = index == 0 })
            .ToList();

        // Act
        var response = await _api.Client.PutAsJsonAsync($"/api/sales/{created.GetProperty("id").GetGuid()}",
            new { customerId, branchId, isCancelled = false, items });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sale = await DataAsync(response);
        Assert.Equal(10m, sale.GetProperty("items")[1].GetProperty("discountPercentage").GetDecimal());
        Assert.Equal(72m, sale.GetProperty("totalAmount").GetDecimal());
    }

    private static async Task<JsonElement> DataAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
}
