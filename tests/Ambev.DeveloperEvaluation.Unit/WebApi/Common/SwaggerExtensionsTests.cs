using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: TD-014
/// <summary>
/// Contains unit tests for <see cref="SwaggerExtensions"/>: Swagger UI must be able to send the JWT, so every
/// protected endpoint can be called from it.
/// </summary>
public class SwaggerExtensionsTests
{
    /// <summary>
    /// Tests that the options carry an HTTP bearer scheme for JWTs, sent in the Authorization header.
    /// </summary>
    [Fact(DisplayName = "Given the Swagger options When configured Then they define a JWT bearer scheme")]
    public void Given_SwaggerOptions_When_Configured_Then_DefineJwtBearerScheme()
    {
        // Arrange
        var options = new SwaggerGenOptions();

        // Act
        SwaggerExtensions.ConfigureJwtBearer(options);

        // Assert
        var scheme = options.SwaggerGeneratorOptions.SecuritySchemes.Should().ContainKey(SwaggerExtensions.BearerSchemeId).WhoseValue;
        scheme.Type.Should().Be(SecuritySchemeType.Http);
        scheme.Scheme.Should().Be("bearer");
        scheme.BearerFormat.Should().Be("JWT");
        scheme.In.Should().Be(ParameterLocation.Header);
    }

    /// <summary>
    /// Tests that the options require that scheme on every operation, so Swagger UI sends the token once authorized.
    /// </summary>
    [Fact(DisplayName = "Given the Swagger options When configured Then every operation requires the bearer scheme")]
    public void Given_SwaggerOptions_When_Configured_Then_RequireBearerScheme()
    {
        // Arrange
        var options = new SwaggerGenOptions();

        // Act
        SwaggerExtensions.ConfigureJwtBearer(options);

        // Assert
        var requirement = options.SwaggerGeneratorOptions.SecurityRequirements.Should().ContainSingle().Subject;
        requirement.Keys.Should().ContainSingle()
            .Which.Reference.Id.Should().Be(SwaggerExtensions.BearerSchemeId);
    }
}
