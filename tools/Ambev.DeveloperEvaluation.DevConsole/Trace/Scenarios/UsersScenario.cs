using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-057 (FEAT-017)
/// <summary>
/// USR-CRT, USR-GET, USR-DEL: create a manager, read it, read an unknown id and a malformed one, delete it, delete it
/// again.
/// </summary>
public sealed class UsersScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "users";

    /// <inheritdoc />
    public string Description => "create, get, get unknown, get malformed id, delete, delete again";

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        var created = await context.SendAsync(HttpMethod.Post, "/api/users", new
        {
            username = $"manager-{context.RunId}",
            email = $"manager-{context.RunId}@example.com",
            password = "Manag3r@Pass",
            phone = "+5511999990003",
            role = UserRole.Manager,
            status = UserStatus.Active
        });
        var id = (await context.ReadJsonAsync(created)).GetProperty("data").GetProperty("id").GetString();

        await context.SendAsync(HttpMethod.Get, $"/api/users/{id}");
        await context.SendAsync(HttpMethod.Get, $"/api/users/{Guid.NewGuid()}");
        await context.SendAsync(HttpMethod.Get, "/api/users/not-a-guid");
        await context.SendAsync(HttpMethod.Delete, $"/api/users/{id}");
        await context.SendAsync(HttpMethod.Delete, $"/api/users/{id}");
    }
}
