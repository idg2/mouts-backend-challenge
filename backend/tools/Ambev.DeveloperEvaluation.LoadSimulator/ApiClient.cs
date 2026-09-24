using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.LoadSimulator;

// Work item: TASK-040 (FEAT-006)
/// <summary>
/// The API calls the simulator makes. Setup calls fail loudly with the API's answer; sale posts return the status code.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;

    /// <summary>
    /// Initializes a new instance of ApiClient
    /// </summary>
    /// <param name="http">A client whose BaseAddress is the API base URL</param>
    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Registers a Manager user for this run with a random password, logs in once, and uses the token from then on.
    /// </summary>
    /// <param name="runId">The run id, used to make the e-mail and username unique</param>
    public async Task AuthenticateAsNewManagerAsync(string runId)
    {
        var email = $"loadsim-{runId}@example.com";
        var password = $"Ls!9{Guid.NewGuid():N}";
        await CreateAsync("api/users", new
        {
            username = $"loadsim-{runId}",
            password,
            phone = "+5511999999999",
            email,
            status = 1, // Active
            role = 2 // Manager
        }, "Create user");

        var login = await ReadAsync(await _http.PostAsJsonAsync("api/auth", new { email, password }), "Log in");
        var token = login.GetProperty("data").GetProperty("token").GetString();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Registers one customer, one branch, and three products for this run and returns a three-item sale body over them.
    /// </summary>
    /// <param name="runId">The run id, used to make names and product codes unique</param>
    /// <returns>The sale body every request of the run posts</returns>
    public async Task<object> CreateSaleBodyAsync(string runId)
    {
        var customerId = await CreateAsync(
            "api/customers", new { name = $"Load customer {runId}", document = Cpf.Generate(Random.Shared) }, "Create customer");
        var branchId = await CreateAsync("api/branches", new { name = $"Load branch {runId}" }, "Create branch");

        var items = new List<object>();
        for (var index = 1; index <= 3; index++)
        {
            var productId = await CreateAsync(
                "api/products",
                new { code = $"LOAD-{runId}-{index}", description = $"Load product {index}", unitPrice = 10m },
                "Create product");
            items.Add(new { productId, quantity = 2, discountPercentage = 0m, discountAmount = 0m, totalAmount = 20m });
        }

        return new { customerId, branchId, totalAmount = 60m, items };
    }

    /// <summary>
    /// Counts the stored sales.
    /// </summary>
    /// <returns>The total number of sales</returns>
    public async Task<int> CountSalesAsync()
    {
        var page = await ReadAsync(await _http.GetAsync("api/sales?_size=1"), "Count sales");
        return page.GetProperty("totalCount").GetInt32();
    }

    /// <summary>
    /// Posts one sale and returns the status code.
    /// </summary>
    /// <param name="sale">The sale body</param>
    /// <param name="respondAsync">True to send Prefer: respond-async</param>
    /// <returns>The HTTP status code</returns>
    public async Task<int> PostSaleAsync(object sale, bool respondAsync)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/sales") { Content = JsonContent.Create(sale) };
        if (respondAsync)
            request.Headers.Add("Prefer", "respond-async");

        using var response = await _http.SendAsync(request);
        await response.Content.ReadAsByteArrayAsync();
        return (int)response.StatusCode;
    }

    private async Task<Guid> CreateAsync(string path, object body, string action)
    {
        var created = await ReadAsync(await _http.PostAsJsonAsync(path, body), action);
        return created.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response, string action)
    {
        using (response)
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"{action} failed with {(int)response.StatusCode}: {text}");

            return JsonDocument.Parse(text).RootElement.Clone();
        }
    }
}
