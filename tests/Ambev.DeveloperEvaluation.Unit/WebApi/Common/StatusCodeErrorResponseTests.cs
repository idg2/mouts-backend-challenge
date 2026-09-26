using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: TASK-070 (FEAT-018)
/// <summary>
/// Contains unit tests for the <see cref="StatusCodeErrorResponse"/> class.
/// </summary>
public class StatusCodeErrorResponseTests
{
    /// <summary>
    /// Tests that each catalog status maps to its type and fixed text, and any other status to HttpError with the
    /// reason phrase.
    /// </summary>
    [Theory(DisplayName = "Given a bodiless status When creating the response Then type and detail follow the catalog")]
    [InlineData(401, "AuthenticationError", "A valid bearer token is required")]
    [InlineData(403, "AuthorizationError", "The authenticated user is not allowed to perform this action")]
    [InlineData(404, "ResourceNotFound", "No endpoint matches the request path")]
    [InlineData(405, "MethodNotAllowed", "The HTTP method is not allowed on this path")]
    [InlineData(415, "UnsupportedMediaType", "The request Content-Type is not supported")]
    [InlineData(429, "HttpError", "Too Many Requests")]
    [InlineData(502, "HttpError", "Bad Gateway")]
    public void Given_Status_When_Create_Then_CatalogRow(int statusCode, string type, string detail)
    {
        // Act
        var response = StatusCodeErrorResponse.Create(statusCode);

        // Assert
        Assert.Equal(type, response.Type);
        Assert.Equal(type, response.Error);
        Assert.Equal(new[] { detail }, JsonSerializer.Deserialize<string[]>(response.Detail));
    }

    /// <summary>
    /// Tests that writing the 401 body keeps the WWW-Authenticate header the JWT challenge set (Review Focus 3).
    /// </summary>
    [Fact(DisplayName = "Given a 401 with WWW-Authenticate When writing the body Then the header survives")]
    public async Task Given_401WithChallengeHeader_When_WriteAsync_Then_HeaderSurvives()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";

        // Act
        await StatusCodeErrorResponse.Create(context.Response.StatusCode).WriteAsync(context, context.Response.StatusCode);

        // Assert
        Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.True(context.Response.Body.Length > 0);
    }
}
