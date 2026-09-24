using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Middleware;

// Work item: BUG-002
/// <summary>
/// Contains unit tests for the <see cref="ValidationExceptionMiddleware"/> class.
/// </summary>
public class ValidationExceptionMiddlewareTests
{
    /// <summary>
    /// Tests that an UnauthorizedAccessException thrown downstream becomes a 401 response carrying the exception message.
    /// </summary>
    [Fact(DisplayName = "Given an UnauthorizedAccessException When invoking the middleware Then returns 401 with the message")]
    public async Task Given_UnauthorizedAccessException_When_InvokeAsync_Then_Returns401WithMessage()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new UnauthorizedAccessException("Invalid credentials"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Invalid credentials", body.RootElement.GetProperty("message").GetString());
    }
}
