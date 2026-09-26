using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;

// Work item: TASK-057 (FEAT-017)
/// <summary>
/// AUT-LGN: the administrator logs in; a wrong password, an inactive user, and an invalid e-mail are refused.
/// </summary>
public sealed class AuthScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "auth";

    /// <inheritdoc />
    public string Description => "admin login, wrong password, inactive user, invalid e-mail";

    /// <inheritdoc />
    public async Task RunAsync(ScenarioContext context)
    {
        await context.LoginAsync(context.AdminEmail, context.AdminPassword);
        await context.LoginAsync(context.AdminEmail, "wrong-password");

        var email = $"inactive-{context.RunId}@example.com";
        const string password = "Inact1ve@Pass";
        await context.SendAsync(HttpMethod.Post, "/api/users", new
        {
            username = $"inactive-{context.RunId}",
            email,
            password,
            phone = "+5511999990002",
            role = UserRole.Customer,
            status = UserStatus.Inactive
        });
        await context.LoginAsync(email, password);

        await context.LoginAsync("not-an-email", "x");
    }
}
