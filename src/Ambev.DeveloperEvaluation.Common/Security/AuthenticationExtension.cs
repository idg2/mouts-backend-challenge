#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using System.Security.Claims;
using System.Threading.Tasks;
#endif
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Ambev.DeveloperEvaluation.Common.Security
{
    public static class AuthenticationExtension
    {
        // Work item: TASK-046 (FEAT-017)
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            var secretKey = configuration["Jwt:SecretKey"]?.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

            var key = Encoding.ASCII.GetBytes(secretKey);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
#if DEBUG
                // Trace-only events (StepTrace): each returns a completed task, so the default challenge and forbid still run.
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        StepTrace.Step("CMN-AUT-01", "Read the bearer token",
                            [("hasBearer", context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)),
                             ("path", context.Request.Path.Value)]);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        StepTrace.Step("CMN-AUT-02", "Token valid?", [("valid", true)]);
                        StepTrace.Step("CMN-AUT-03", "Principal with nameid, unique_name, and role",
                            [("userId", context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                             ("role", context.Principal?.FindFirst(ClaimTypes.Role)?.Value),
                             ("expires", context.SecurityToken.ValidTo)]);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        StepTrace.Step("CMN-AUT-02", "Token valid?", [("valid", false), ("failure", context.Exception.GetType().Name)]);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        StepTrace.Step("CMN-AUT-05", "Authenticated?",
                            [("authenticated", false), ("failure", context.AuthenticateFailure?.GetType().Name), ("error", context.Error)]);
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        // The handler never sets context.Principal on Forbidden; the authentication middleware already
                        // put the authenticated user on HttpContext.User before authorization forbade the request.
                        StepTrace.Step("CMN-AUT-06", "Role allowed?",
                            [("allowed", false),
                             ("userId", context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                             ("role", context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value)]);
                        return Task.CompletedTask;
                    }
                };
#endif
            });

            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }
    }
}
