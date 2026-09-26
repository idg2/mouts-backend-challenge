using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

// Work item: TD-014
/// <summary>
/// Registers Swagger with a JWT bearer scheme, so Swagger UI shows an Authorize button and sends the token on every
/// request once the user pastes it.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// The id of the bearer security scheme in the OpenAPI document.
    /// </summary>
    public const string BearerSchemeId = "Bearer";

    /// <summary>
    /// Adds the Swagger generator with the JWT bearer scheme.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSwaggerWithJwtBearer(this IServiceCollection services) =>
        services.AddSwaggerGen(ConfigureJwtBearer);

    /// <summary>
    /// Defines the HTTP bearer scheme for JWTs and requires it on every operation. Anonymous endpoints still answer
    /// without a token; the requirement only makes Swagger UI send the token it holds.
    /// </summary>
    /// <param name="options">The Swagger generator options</param>
    public static void ConfigureJwtBearer(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "The token from POST /api/auth, without the \"Bearer \" prefix."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerSchemeId } }] = []
        });
    }
}
