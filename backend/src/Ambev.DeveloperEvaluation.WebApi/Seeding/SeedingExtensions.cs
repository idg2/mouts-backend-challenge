using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.WebApi.Seeding;

// Work item: BUG-012
/// <summary>
/// Prepares the database before the API serves requests: applies the pending migrations and seeds the administrator.
/// </summary>
public static class SeedingExtensions
{
    /// <summary>
    /// Reads the administrator settings, failing startup when one is missing, and registers the seeder.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <returns>The same builder</returns>
    public static WebApplicationBuilder AddAdminSeed(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(AdminSeedSettings.FromConfiguration(builder.Configuration));
        builder.Services.AddScoped<AdminSeeder>();

        return builder;
    }

    // Work item: TASK-048 (FEAT-017)
    /// <summary>
    /// Applies the pending migrations, then seeds the administrator. It runs before the host starts, so the outbox relay
    /// and the endpoints never meet a database without its tables.
    /// </summary>
    /// <param name="app">The built web application</param>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DefaultContext>().Database.MigrateAsync();
        StepTrace.Step("USR-SED-02", "Apply pending migrations", [("applied", true)]);

        var settings = scope.ServiceProvider.GetRequiredService<AdminSeedSettings>();
        await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync(settings, CancellationToken.None);
    }
}
