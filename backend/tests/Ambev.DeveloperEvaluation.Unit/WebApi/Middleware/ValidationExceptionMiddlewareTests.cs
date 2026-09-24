using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Middleware;

// Work item: BUG-002, TD-007 (FEAT-010)
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

    // Work item: TD-007 (FEAT-010)
    /// <summary>
    /// Tests that a KeyNotFoundException thrown downstream becomes a 404 response carrying the exception message.
    /// </summary>
    [Fact(DisplayName = "Given a KeyNotFoundException When invoking the middleware Then returns 404 with the message")]
    public async Task Given_KeyNotFoundException_When_InvokeAsync_Then_Returns404WithMessage()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new KeyNotFoundException("Sale with ID 1 not found"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Sale with ID 1 not found", body.RootElement.GetProperty("message").GetString());
    }
}
