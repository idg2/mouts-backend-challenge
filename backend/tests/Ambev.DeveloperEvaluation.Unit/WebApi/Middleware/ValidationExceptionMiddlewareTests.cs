using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Middleware;

// Work item: BUG-002, TD-007 (FEAT-010), BUG-003, TASK-068 (FEAT-018)
/// <summary>
/// Contains unit tests for the <see cref="ValidationExceptionMiddleware"/> class.
/// </summary>
public class ValidationExceptionMiddlewareTests
{
    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that a ValidationException with failures becomes a 400 whose error is the first code and whose detail
    /// lists every message.
    /// </summary>
    [Fact(DisplayName = "Given a ValidationException with two failures When invoking the middleware Then returns 400 ValidationError with both messages")]
    public async Task Given_ValidationExceptionWithFailures_When_InvokeAsync_Then_Returns400WithMessages()
    {
        // Arrange
        var failures = new[]
        {
            new ValidationFailure("Items[0].Quantity", "items[0]: above the maximum") { ErrorCode = "QuantityLimitExceeded" },
            new ValidationFailure("Items[1].DiscountPercentage", "items[1]: above the ceiling") { ErrorCode = "DiscountAboveAllowed" }
        };
        var middleware = new ValidationExceptionMiddleware(_ => throw new ValidationException(failures));
        var context = NewContext();

        // Act
        await middleware.InvokeAsync(context, NullLogger<ValidationExceptionMiddleware>.Instance);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("ValidationError", body.type);
        Assert.Equal("QuantityLimitExceeded", body.error);
        Assert.Equal(new[] { "Items[0].Quantity: items[0]: above the maximum", "Items[1].DiscountPercentage: items[1]: above the ceiling" }, body.detail);
    }

    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that a ValidationException built from a message alone still gives one detail element (Review Focus 1).
    /// </summary>
    [Fact(DisplayName = "Given a ValidationException without failures When invoking the middleware Then detail holds the message")]
    public async Task Given_ValidationExceptionWithoutFailures_When_InvokeAsync_Then_DetailHoldsMessage()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new ValidationException("Nothing to validate"));
        var context = NewContext();

        // Act
        await middleware.InvokeAsync(context, NullLogger<ValidationExceptionMiddleware>.Instance);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("ValidationError", body.error);
        Assert.Equal(new[] { "Nothing to validate" }, body.detail);
    }

    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that an UnauthorizedAccessException thrown downstream becomes a 401 AuthenticationError carrying the message.
    /// </summary>
    [Fact(DisplayName = "Given an UnauthorizedAccessException When invoking the middleware Then returns 401 AuthenticationError")]
    public async Task Given_UnauthorizedAccessException_When_InvokeAsync_Then_Returns401()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new UnauthorizedAccessException("Invalid credentials"));
        var context = NewContext();

        // Act
        await middleware.InvokeAsync(context, NullLogger<ValidationExceptionMiddleware>.Instance);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("AuthenticationError", body.type);
        Assert.Equal("AuthenticationError", body.error);
        Assert.Equal(new[] { "Invalid credentials" }, body.detail);
    }

    // Work item: TD-007 (FEAT-010), TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that a KeyNotFoundException thrown downstream becomes a 404 ResourceNotFound carrying the message.
    /// </summary>
    [Fact(DisplayName = "Given a KeyNotFoundException When invoking the middleware Then returns 404 ResourceNotFound")]
    public async Task Given_KeyNotFoundException_When_InvokeAsync_Then_Returns404()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new KeyNotFoundException("Sale with ID 1 not found"));
        var context = NewContext();

        // Act
        await middleware.InvokeAsync(context, NullLogger<ValidationExceptionMiddleware>.Instance);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("ResourceNotFound", body.type);
        Assert.Equal(new[] { "Sale with ID 1 not found" }, body.detail);
    }

    // Work item: BUG-003, TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that a DuplicateEntryException thrown downstream becomes a 409 DuplicateEntry carrying the message.
    /// </summary>
    [Fact(DisplayName = "Given a DuplicateEntryException When invoking the middleware Then returns 409 DuplicateEntry")]
    public async Task Given_DuplicateEntryException_When_InvokeAsync_Then_Returns409()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new DuplicateEntryException("User with email a@b.com already exists"));
        var context = NewContext();

        // Act
        await middleware.InvokeAsync(context, NullLogger<ValidationExceptionMiddleware>.Instance);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("DuplicateEntry", body.type);
        Assert.Equal(new[] { "User with email a@b.com already exists" }, body.detail);
    }

    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that any other exception becomes a 500 ServerError with the fixed detail, and that the exception is logged
    /// once at Error level. The body never carries the exception message.
    /// </summary>
    [Fact(DisplayName = "Given an unexpected exception When invoking the middleware Then returns 500 ServerError and logs it")]
    public async Task Given_UnexpectedException_When_InvokeAsync_Then_Returns500AndLogs()
    {
        // Arrange
        var exception = new InvalidOperationException("secret internal state");
        var middleware = new ValidationExceptionMiddleware(_ => throw exception);
        var context = NewContext();
        var logger = Substitute.For<ILogger<ValidationExceptionMiddleware>>();

        // Act
        await middleware.InvokeAsync(context, logger);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("ServerError", body.type);
        Assert.Equal("ServerError", body.error);
        Assert.Equal(new[] { "An unexpected error occurred" }, body.detail);
        var errorLogs = logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log)
                        && (LogLevel)call.GetArguments()[0]! == LogLevel.Error
                        && ReferenceEquals(call.GetArguments()[3], exception))
            .ToList();
        Assert.Single(errorLogs);
    }

    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that a Kestrel request error (a body over the size limit, a bad chunked body) keeps its own status with
    /// the catalog body instead of becoming a 500, and is not logged as an unhandled exception.
    /// </summary>
    [Fact(DisplayName = "Given a BadHttpRequestException When invoking the middleware Then returns its status with the catalog body and no error log")]
    public async Task Given_BadHttpRequestException_When_InvokeAsync_Then_ReturnsItsStatus()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge));
        var context = NewContext();
        var logger = Substitute.For<ILogger<ValidationExceptionMiddleware>>();

        // Act
        await middleware.InvokeAsync(context, logger);

        // Assert
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Equal("HttpError", body.type);
        Assert.Equal(new[] { "Payload Too Large" }, body.detail);
        Assert.DoesNotContain(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)call.GetArguments()[0]! == LogLevel.Error);
    }

    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// Tests that an exception thrown after the response has started is rethrown, because the body cannot be replaced
    /// (Review Focus 4).
    /// </summary>
    [Fact(DisplayName = "Given an exception after the response started When invoking the middleware Then it is rethrown")]
    public async Task Given_ExceptionAfterResponseStarted_When_InvokeAsync_Then_Rethrows()
    {
        // Arrange
        var middleware = new ValidationExceptionMiddleware(_ => throw new InvalidOperationException("too late"));
        var context = NewContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        // Act
        var act = () => middleware.InvokeAsync(context, NullLogger<ValidationExceptionMiddleware>.Instance);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<(string type, string error, string[] detail)> ReadBodyAsync(HttpContext context)
    {
        Assert.Equal("application/json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        var root = body.RootElement;
        Assert.Equal(3, root.EnumerateObject().Count());
        return (
            root.GetProperty("type").GetString()!,
            root.GetProperty("error").GetString()!,
            JsonSerializer.Deserialize<string[]>(root.GetProperty("detail").GetString()!)!);
    }

    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// A response feature that reports the response as started, so the middleware cannot rewrite it.
    /// </summary>
    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
