#if DEBUG
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// The StepTrace sink is one static field for the whole process, so the tests that read the trace run alone, after
/// every parallel test class.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class StepTraceCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "StepTrace";
}

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// Proves the Debug-only diagnostics routes over HTTP: the anonymous status, the Admin-only outbox and trace reads
/// after a cursor, and that reading diagnostics never records itself in the trace.
/// </summary>
[Collection(StepTraceCollection.Name)]
public class DiagnosticsTests : IClassFixture<SalesApiFixture>
{
    private readonly SalesApiFixture _api;

    public DiagnosticsTests(SalesApiFixture api)
    {
        _api = api;
    }

    // Work item: TASK-084 (FEAT-019)
    [Fact(DisplayName = "Given no token When reading the status Then 200 with the trace enabled")]
    public async Task Given_NoToken_When_ReadingStatus_Then_TraceEnabled()
    {
        // Arrange
        using var anonymous = _api.CreateAnonymousClient();

        // Act
        var response = await anonymous.GetAsync("/api/diagnostics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await DataAsync(response)).GetProperty("traceEnabled").GetBoolean());
    }

    // Work item: TASK-084 (FEAT-019)
    [Theory(DisplayName = "Given no token When reading the outbox or the trace Then 401")]
    [InlineData("/api/diagnostics/outbox")]
    [InlineData("/api/diagnostics/trace")]
    public async Task Given_NoToken_When_ReadingOutboxOrTrace_Then_Unauthorized(string route)
    {
        // Arrange
        using var anonymous = _api.CreateAnonymousClient();

        // Act
        var response = await anonymous.GetAsync(route);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Work item: TASK-084 (FEAT-019)
    [Theory(DisplayName = "Given a Manager token When reading the outbox or the trace Then 403")]
    [InlineData("/api/diagnostics/outbox")]
    [InlineData("/api/diagnostics/trace")]
    public async Task Given_ManagerToken_When_ReadingOutboxOrTrace_Then_Forbidden(string route)
    {
        // Arrange
        using var manager = await ManagerClientAsync();

        // Act
        var response = await manager.GetAsync(route);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Work item: TASK-084 (FEAT-019)
    [Theory(DisplayName = "Given a malformed cursor When reading the outbox or the trace Then 400 ValidationError")]
    [InlineData("/api/diagnostics/outbox?after=-1")]
    [InlineData("/api/diagnostics/outbox?after=abc")]
    [InlineData("/api/diagnostics/trace?after=-1")]
    [InlineData("/api/diagnostics/trace?after=abc")]
    public async Task Given_MalformedCursor_When_Reading_Then_BadRequest(string route)
    {
        // Act
        var response = await _api.Client.GetAsync(route);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ValidationError", body.GetProperty("type").GetString());
    }

    // Work item: TASK-084 (FEAT-019)
    [Fact(DisplayName = "Given a sale created after the cursor When reading the outbox Then its SaleCreated row is returned")]
    public async Task Given_SaleAfterCursor_When_ReadingOutbox_Then_SaleCreatedReturned()
    {
        // Arrange
        var head = (await DataAsync(await _api.Client.GetAsync("/api/diagnostics/outbox"))).GetProperty("head").GetInt64();
        var saleId = await CreateSaleAsync();

        // Act
        var page = await DataAsync(await _api.Client.GetAsync($"/api/diagnostics/outbox?after={head}"));

        // Assert
        var row = page.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("type").GetString() == "SaleCreated"
                            && item.GetProperty("payload").GetProperty("Sale").GetProperty("SaleId").GetGuid() == saleId);
        Assert.True(row.GetProperty("sequence").GetInt64() > head);
        Assert.True(page.GetProperty("head").GetInt64() >= row.GetProperty("sequence").GetInt64());
    }

    // Work item: TASK-084 (FEAT-019)
    [Fact(DisplayName = "Given a sale created after the cursor When reading the trace Then its steps are returned and no diagnostics request is")]
    public async Task Given_SaleAfterCursor_When_ReadingTrace_Then_SaleStepsAndNoDiagnosticsRequest()
    {
        // Arrange
        var head = (await DataAsync(await _api.Client.GetAsync("/api/diagnostics/trace"))).GetProperty("head").GetInt64();
        await CreateSaleAsync();
        await _api.Client.GetAsync("/api/diagnostics/outbox");

        // Act
        var page = await DataAsync(await _api.Client.GetAsync($"/api/diagnostics/trace?after={head}"));

        // Assert
        var items = page.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, item => item.GetProperty("key").GetString() == "SAL-CRT-01");
        Assert.All(items, item => Assert.True(item.GetProperty("cursor").GetInt64() > head));
        Assert.DoesNotContain(items, item =>
            item.GetProperty("key").GetString() == "CMN-PIP-01"
            && item.GetProperty("values").EnumerateArray().Any(value =>
                value.GetProperty("name").GetString() == "path"
                && value.GetProperty("value").GetString()!.StartsWith("/api/diagnostics", StringComparison.OrdinalIgnoreCase)));
        Assert.All(items, item => Assert.DoesNotContain("/", item.GetProperty("file").GetString()!));
    }

    private async Task<Guid> CreateSaleAsync()
    {
        var (customerId, branchId, productId) = await _api.CreateReferencesAsync(10m);
        var response = await _api.Client.PostAsJsonAsync("/api/sales",
            new { customerId, branchId, items = new[] { new { productId, quantity = 4 } } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await DataAsync(response)).GetProperty("id").GetGuid();
    }

    private async Task<HttpClient> ManagerClientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"manager{suffix}@example.com";
        const string password = "Str0ng@Pass";
        var created = await _api.Client.PostAsJsonAsync("/api/users", new
        {
            username = $"manager{suffix}", password, phone = "+5511999998888", email, status = 1, role = 2
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var client = _api.CreateAnonymousClient();
        var login = await client.PostAsJsonAsync("/api/auth", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = (await DataAsync(login)).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> DataAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
}
#endif
