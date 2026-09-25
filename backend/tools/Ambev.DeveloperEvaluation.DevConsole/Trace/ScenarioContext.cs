using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Bogus;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// What every scenario needs: the in-process client, the administrator token, the waiter, Bogus data, and printing
/// of each request and response around the trace lines.
/// </summary>
public sealed class ScenarioContext
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions Printed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly string[] Secrets = ["password", "token"];
    // Work item: TASK-059 (FEAT-017)
    // The properties of a FluentValidation failure that echo the rejected value.
    private static readonly string[] FailureValues = ["attemptedValue", "formattedMessagePlaceholderValues"];
    // Work item: TASK-059 (FEAT-017)
    private const string UnparsedBody = "[unparsed body]";
    private readonly StepWaiter _waiter;
    private readonly TimeSpan _waitTimeout;

    // Work item: TASK-056 (FEAT-017), TASK-057 (FEAT-017)
    /// <summary>
    /// Initializes a new instance of ScenarioContext
    /// </summary>
    /// <param name="client">The TestServer client</param>
    /// <param name="adminToken">The seeded administrator's token, or empty before the login</param>
    /// <param name="waiter">The waiter fed by the console sink</param>
    /// <param name="waitTimeout">How long a wait for a traced key lasts</param>
    /// <param name="runId">The short id that prefixes every name this run creates</param>
    /// <param name="output">Where scenario messages go</param>
    /// <param name="adminEmail">The seeded administrator's e-mail (Seed:Admin:Email)</param>
    /// <param name="adminPassword">The seeded administrator's password (Seed:Admin:Password)</param>
    public ScenarioContext(
        HttpClient client, string adminToken, StepWaiter waiter, TimeSpan waitTimeout, string runId, TextWriter output,
        string adminEmail, string adminPassword)
    {
        Client = client;
        AdminToken = adminToken;
        _waiter = waiter;
        _waitTimeout = waitTimeout;
        RunId = runId;
        Out = output;
        AdminEmail = adminEmail;
        AdminPassword = adminPassword;
        Faker = new Faker("pt_BR");
    }

    /// <summary>Gets the TestServer client.</summary>
    public HttpClient Client { get; }

    /// <summary>Gets the seeded administrator's token.</summary>
    public string AdminToken { get; }

    // Work item: TASK-057 (FEAT-017)
    /// <summary>Gets the seeded administrator's e-mail, from the resolved configuration.</summary>
    public string AdminEmail { get; }

    // Work item: TASK-057 (FEAT-017)
    /// <summary>Gets the seeded administrator's password, from the resolved configuration; never printed.</summary>
    public string AdminPassword { get; }

    /// <summary>Gets the short id that prefixes every name this run creates.</summary>
    public string RunId { get; }

    /// <summary>Gets where scenario messages go.</summary>
    public TextWriter Out { get; }

    /// <summary>Gets the data generator.</summary>
    public Faker Faker { get; }

    /// <summary>Returns a valid CPF, digits only.</summary>
    public string NewCpf() => Cpf.Generate(Random.Shared);

    /// <summary>
    /// Sends one request, printing what it is about to do and what came back.
    /// </summary>
    /// <param name="method">The HTTP method</param>
    /// <param name="path">The path, such as /api/sales</param>
    /// <param name="body">The JSON body, or a string sent as is</param>
    /// <param name="token">Null for the administrator token, empty for no token, or another user's token</param>
    /// <param name="contentType">Overrides application/json for a string body</param>
    /// <param name="configure">Extra headers</param>
    /// <returns>The response</returns>
    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body = null, string? token = null, string? contentType = null, Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(method, path);
        var bearer = token ?? AdminToken;
        if (bearer.Length > 0)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        string summary;
        if (body is string raw)
        {
            request.Content = new StringContent(raw, Encoding.UTF8, contentType ?? "application/json");
            summary = Truncate(Redact(raw));
        }
        else if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
            summary = Truncate(Redact(JsonSerializer.Serialize(body, Json)));
        }
        else
        {
            summary = string.Empty;
        }

        configure?.Invoke(request);
        Out.WriteLine();
        Out.WriteLine($"-> {method} {path} {summary}");
        var response = await Client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        Out.WriteLine($"<- {(int)response.StatusCode} {Truncate(Redact(responseBody))}");
        return response;
    }

    /// <summary>Reads the response body as JSON.</summary>
    public async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    /// <summary>
    /// Waits for a traced key carrying the given values, printing the wait and its outcome.
    /// </summary>
    /// <returns>The event, or null after the timeout</returns>
    public async Task<StepEvent?> AwaitAsync(string key, params (string Name, string Value)[] match)
    {
        var description = $"{key} {string.Join(' ', match.Select(pair => $"{pair.Name}={pair.Value}"))}";
        Out.WriteLine($"... waiting for {description}");
        var found = await _waiter.WaitAsync(key, _waitTimeout, match);
        Out.WriteLine(found is null
            ? $"!!! timeout after {_waitTimeout} waiting for {description}"
            : $"... seen {description} at {found.At:HH:mm:ss.ffffff}");
        return found;
    }

    /// <summary>Finds an already traced event.</summary>
    public StepEvent? Find(string key, params (string Name, string Value)[] match) => _waiter.Find(key, match);

    /// <summary>
    /// Logs in and returns the token, or an empty string when the API refused.
    /// </summary>
    public async Task<string> LoginAsync(string email, string password)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/auth", new { email, password }, token: "");
        if (!response.IsSuccessStatusCode)
            return string.Empty;

        return (await ReadJsonAsync(response)).GetProperty("data").GetProperty("token").GetString() ?? string.Empty;
    }

    // Work item: TASK-056 (FEAT-017), TASK-058 (FEAT-017), TASK-059 (FEAT-017)
    /// <summary>
    /// Masks every JSON property named password or token, at any depth and in any letter case, and the attemptedValue
    /// and formattedMessagePlaceholderValues of a validation failure whose propertyName contains password, so request
    /// and response summaries never print a credential. A body that looks like JSON (starts with { or [) but cannot be
    /// parsed, or has a duplicate property name, prints as a fixed placeholder; other text comes back unchanged.
    /// </summary>
    /// <param name="text">The body</param>
    /// <returns>The body with the secrets replaced by "***"</returns>
    public static string Redact(string text)
    {
        try
        {
            var root = JsonNode.Parse(text);
            return Mask(root) ? root!.ToJsonString(Printed) : text;
        }
        catch (JsonException)
        {
            var trimmed = text.TrimStart();
            return trimmed.StartsWith('{') || trimmed.StartsWith('[') ? UnparsedBody : text;
        }
        catch (ArgumentException)
        {
            // Enumerating an object parsed with a duplicate property name throws.
            return UnparsedBody;
        }
    }

    // Work item: TASK-056 (FEAT-017), TASK-058 (FEAT-017), TASK-059 (FEAT-017)
    private static bool Mask(JsonNode? node)
    {
        var masked = false;
        switch (node)
        {
            case JsonObject jsonObject:
                var failsPassword = jsonObject
                    .Any(property => string.Equals(property.Key, "propertyName", StringComparison.OrdinalIgnoreCase)
                                     && property.Value is JsonValue value
                                     && value.TryGetValue<string>(out var propertyName)
                                     && propertyName.Contains("password", StringComparison.OrdinalIgnoreCase));
                foreach (var name in jsonObject.Select(property => property.Key).ToList())
                {
                    if (Secrets.Contains(name, StringComparer.OrdinalIgnoreCase)
                        || (failsPassword && FailureValues.Contains(name, StringComparer.OrdinalIgnoreCase)))
                    {
                        jsonObject[name] = "***";
                        masked = true;
                    }
                    else
                    {
                        masked |= Mask(jsonObject[name]);
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                    masked |= Mask(item);
                break;
        }

        return masked;
    }

    private static string Truncate(string text) =>
        text.Length <= 160 ? text : string.Concat(text.AsSpan(0, 160), "…");
}
