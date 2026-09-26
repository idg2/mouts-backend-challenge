using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-057 (FEAT-017)
/// <summary>
/// CMN-HLT, CMN-RSP-03, CMN-AUT: health checks, a malformed body, a wrong content type, no token, a bad token,
/// and a Customer-role token on a write.
/// </summary>
public sealed class ConventionsScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "conventions";

    /// <inheritdoc />
    public string Description => "health checks, malformed JSON, wrong content type, 401, bad token, 403";

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        await context.SendAsync(HttpMethod.Get, "/health", token: "");
        await context.SendAsync(HttpMethod.Get, "/health/live", token: "");
        await context.SendAsync(HttpMethod.Get, "/health/ready", token: "");

        await context.SendAsync(HttpMethod.Post, "/api/branches", "{ not json", contentType: "application/json");
        await context.SendAsync(HttpMethod.Post, "/api/branches", "name=x", contentType: "text/plain");

        await context.SendAsync(HttpMethod.Get, "/api/branches", token: "");
        await context.SendAsync(HttpMethod.Get, "/api/branches", token: "not.a.token");

        // The API binds enums as numbers (no string enum converter), so role and status are sent as the enum values.
        var email = $"customer-{context.RunId}@example.com";
        const string password = "Cust0mer@Pass";
        await context.SendAsync(HttpMethod.Post, "/api/users", new
        {
            username = $"customer-{context.RunId}",
            email,
            password,
            phone = "+5511999990001",
            role = UserRole.Customer,
            status = UserStatus.Active
        });
        var customerToken = await context.LoginAsync(email, password);
        await context.SendAsync(HttpMethod.Post, "/api/branches", new { name = $"Forbidden {context.RunId}" }, token: customerToken);
        await context.SendAsync(HttpMethod.Get, "/api/branches", token: customerToken);
    }
}
