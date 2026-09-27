using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Common.HealthChecks;
using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using Ambev.DeveloperEvaluation.WebApi.Seeding;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
#if DEBUG
using Ambev.DeveloperEvaluation.WebApi.Features.Diagnostics;
#endif

namespace Ambev.DeveloperEvaluation.WebApi;

public class Program
{
    // Work item: TD-006, TASK-033 (FEAT-016), TASK-034 (FEAT-016), TASK-038 (FEAT-006), BUG-012, TASK-046 (FEAT-017), TASK-047 (FEAT-017), TASK-070 (FEAT-018), TD-014, TD-043, TASK-084 (FEAT-019)
    public static void Main(string[] args)
    {
        try
        {
            Log.Information("Starting web application");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.AddDefaultLogging();

            builder.Services.AddControllers();
            // Work item: TASK-070 (FEAT-018)
            // Model-state failures answer with the general-api body, and bodiless client errors (415, ...) are left
            // bare for UseStatusCodePages below instead of being wrapped in ProblemDetails.
            builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.SuppressMapClientErrors = true;
                options.InvalidModelStateResponseFactory = context =>
                    new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(ModelStateErrorResponse.Create(context.ModelState));
            });
#if DEBUG
            // Trace-only MVC filters (StepTrace). The action filter takes the lowest order so it runs before the
            // ApiController model-state filter short-circuits a 400.
            builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
            {
                options.Filters.Add<Tracing.StepTraceActionFilter>(int.MinValue);
                options.Filters.Add<Tracing.StepTraceResultFilter>();
            });
#endif
            builder.Services.AddEndpointsApiExplorer();

            builder.AddBasicHealthChecks();
            builder.Services.AddSwaggerWithJwtBearer();

            // Work item: TD-043
            // Checked here, like every other required key, so a missing value names the key instead of failing inside
            // Npgsql on the first query.
            const string defaultConnectionKey = "ConnectionStrings:DefaultConnection";
            var defaultConnection = builder.Configuration[defaultConnectionKey];
            if (string.IsNullOrWhiteSpace(defaultConnection))
                throw new InvalidOperationException(
                    $"{defaultConnectionKey} is not configured. Set it in appsettings or via the ConnectionStrings__DefaultConnection environment variable.");

            builder.Services.AddDbContext<DefaultContext>(options =>
            {
                options.UseNpgsql(
                    defaultConnection,
                    b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
                );
#if DEBUG
                options.AddInterceptors(new Ambev.DeveloperEvaluation.ORM.Tracing.StepTraceCommandInterceptor());
#endif
            });

            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.RegisterDependencies();

            builder.AddMessaging();

            builder.AddAdminSeed();
#if DEBUG
            builder.AddDiagnostics();
#endif

            builder.Services.AddAutoMapper(typeof(Program).Assembly, typeof(ApplicationLayer).Assembly);

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(ApplicationLayer).Assembly,
                    typeof(Program).Assembly
                );
            });

            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

            var app = builder.Build();
            app.MigrateAndSeedAsync().GetAwaiter().GetResult();

#if DEBUG
            app.UseMiddleware<Tracing.DiagnosticsTraceMuteMiddleware>();
#endif
            app.UseRequestLogging();
#if DEBUG
            app.UseMiddleware<Tracing.StepTraceMiddleware>();
#endif
            app.UseMiddleware<ValidationExceptionMiddleware>();
            // Work item: TASK-070 (FEAT-018)
            app.UseStatusCodePages(context =>
                StatusCodeErrorResponse.Create(context.HttpContext.Response.StatusCode)
                    .WriteAsync(context.HttpContext, context.HttpContext.Response.StatusCode));

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseBasicHealthChecks();

            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
