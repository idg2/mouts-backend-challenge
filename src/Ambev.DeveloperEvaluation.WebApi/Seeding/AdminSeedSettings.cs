using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.WebApi.Seeding;

// Work item: BUG-012
/// <summary>
/// The administrator the API creates at startup when no user has its e-mail. Every value is required; the create user
/// rules validate them when the administrator is created.
/// </summary>
public sealed class AdminSeedSettings
{
    /// <summary>
    /// Configuration key of the administrator's username.
    /// </summary>
    public const string UsernameKey = "Seed:Admin:Username";

    /// <summary>
    /// Configuration key of the administrator's e-mail, which is also its login.
    /// </summary>
    public const string EmailKey = "Seed:Admin:Email";

    /// <summary>
    /// Configuration key of the administrator's password.
    /// </summary>
    public const string PasswordKey = "Seed:Admin:Password";

    /// <summary>
    /// Configuration key of the administrator's phone.
    /// </summary>
    public const string PhoneKey = "Seed:Admin:Phone";

    private AdminSeedSettings(string username, string email, string password, string phone)
    {
        Username = username;
        Email = email;
        Password = password;
        Phone = phone;
    }

    /// <summary>
    /// Gets the administrator's username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the administrator's e-mail.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Gets the administrator's password.
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Gets the administrator's phone.
    /// </summary>
    public string Phone { get; }

    /// <summary>
    /// Reads the settings, throwing <see cref="InvalidOperationException"/> naming the first missing key.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The settings</returns>
    public static AdminSeedSettings FromConfiguration(IConfiguration configuration) =>
        new(
            Required(configuration, UsernameKey),
            Required(configuration, EmailKey),
            Required(configuration, PasswordKey),
            Required(configuration, PhoneKey));

    // Work item: TASK-048 (FEAT-017)
    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        StepTrace.Step("USR-SED-01", "Every Seed:Admin key set?", [("key", key), ("set", !string.IsNullOrWhiteSpace(value))]);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"{key} is not configured. Set it in appsettings or via the {key.Replace(":", "__")} environment variable.");

        return value;
    }
}
