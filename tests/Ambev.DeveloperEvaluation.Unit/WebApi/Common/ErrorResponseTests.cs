using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: TASK-067 (FEAT-018)
/// <summary>
/// Contains unit tests for the <see cref="ErrorResponse"/> class.
/// </summary>
public class ErrorResponseTests
{
    /// <summary>
    /// Tests that Validation takes the error from the first failure's code and one detail element per failure, in order.
    /// </summary>
    [Fact(DisplayName = "Given two failures When building a validation response Then error is the first code and detail lists both messages")]
    public void Given_TwoFailures_When_Validation_Then_FirstCodeAndOrderedMessages()
    {
        // Arrange
        var failures = new[]
        {
            new ValidationFailure("Items[2].DiscountPercentage", "items[2]: above the ceiling") { ErrorCode = "DiscountAboveAllowed" },
            new ValidationFailure("Items[0].Quantity", "items[0]: above the maximum") { ErrorCode = "QuantityLimitExceeded" }
        };

        // Act
        var response = ErrorResponse.Validation(failures);

        // Assert
        Assert.Equal(ErrorResponse.ValidationError, response.Type);
        Assert.Equal("DiscountAboveAllowed", response.Error);
        Assert.Equal(new[] { "Items[2].DiscountPercentage: items[2]: above the ceiling", "Items[0].Quantity: items[0]: above the maximum" }, JsonSerializer.Deserialize<string[]>(response.Detail));
    }

    /// <summary>
    /// Tests that a failure with a property name prefixes its message with it, so a client knows which line or field
    /// failed even when the FluentValidation message does not say (child rules on sale items).
    /// </summary>
    [Fact(DisplayName = "Given a failure on a child property When building a validation response Then detail is property: message")]
    public void Given_FailureOnChildProperty_When_Validation_Then_DetailIsPropertyAndMessage()
    {
        // Arrange
        var failures = new[]
        {
            new ValidationFailure("Items[1].Quantity", "'Quantity' must be greater than '0'.") { ErrorCode = "GreaterThanValidator" },
            new ValidationFailure(string.Empty, "At least one item is required") { ErrorCode = "NotEmptyValidator" }
        };

        // Act
        var response = ErrorResponse.Validation(failures);

        // Assert
        Assert.Equal(
            new[] { "Items[1].Quantity: 'Quantity' must be greater than '0'.", "At least one item is required" },
            JsonSerializer.Deserialize<string[]>(response.Detail));
    }

    /// <summary>
    /// Tests that a failure without an error code falls back to the ValidationError type as the error.
    /// </summary>
    [Fact(DisplayName = "Given a failure without a code When building a validation response Then error falls back to ValidationError")]
    public void Given_FailureWithoutCode_When_Validation_Then_ErrorIsValidationError()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("Name", "required") { ErrorCode = null } };

        // Act
        var response = ErrorResponse.Validation(failures);

        // Assert
        Assert.Equal(ErrorResponse.ValidationError, response.Error);
        Assert.Equal(new[] { "Name: required" }, JsonSerializer.Deserialize<string[]>(response.Detail));
    }

    /// <summary>
    /// Tests that Of repeats the type as the error when no error is given, and keeps the messages in order.
    /// </summary>
    [Fact(DisplayName = "Given a type and messages When building with Of Then error repeats the type")]
    public void Given_TypeAndMessages_When_Of_Then_ErrorRepeatsType()
    {
        // Act
        var response = ErrorResponse.Of(ErrorResponse.ResourceNotFound, ["Sale with id 1 not found"]);

        // Assert
        Assert.Equal(ErrorResponse.ResourceNotFound, response.Type);
        Assert.Equal(ErrorResponse.ResourceNotFound, response.Error);
        Assert.Equal(new[] { "Sale with id 1 not found" }, JsonSerializer.Deserialize<string[]>(response.Detail));
    }

    /// <summary>
    /// Tests that an explicit error overrides the type in the error field.
    /// </summary>
    [Fact(DisplayName = "Given an explicit error When building with Of Then error is that value")]
    public void Given_ExplicitError_When_Of_Then_ErrorIsThatValue()
    {
        // Act
        var response = ErrorResponse.Of(ErrorResponse.ValidationError, ["$: invalid JSON"], "InvalidBody");

        // Assert
        Assert.Equal("InvalidBody", response.Error);
    }

    /// <summary>
    /// Tests that the detail array keeps apostrophes as they are, instead of the default \u0027 escape, because
    /// FluentValidation messages quote field names that way.
    /// </summary>
    [Fact(DisplayName = "Given a message with an apostrophe When building with Of Then detail keeps the apostrophe")]
    public void Given_MessageWithApostrophe_When_Of_Then_DetailKeepsApostrophe()
    {
        // Act
        var response = ErrorResponse.Of(ErrorResponse.ValidationError, ["'Name' must not be empty."]);

        // Assert
        Assert.Equal("[\"'Name' must not be empty.\"]", response.Detail);
    }

    /// <summary>
    /// Tests that an empty message list still gives one detail element, equal to the error, so detail is never an empty array.
    /// </summary>
    [Fact(DisplayName = "Given no messages When building with Of Then detail has one element equal to the error")]
    public void Given_NoMessages_When_Of_Then_DetailHasTheError()
    {
        // Act
        var response = ErrorResponse.Of(ErrorResponse.ServerError, []);

        // Assert
        Assert.Equal(new[] { ErrorResponse.ServerError }, JsonSerializer.Deserialize<string[]>(response.Detail));
    }

    /// <summary>
    /// Tests that WriteAsync sets the status code, the JSON content type, and writes the three camelCase fields with the
    /// inner quotes escaped as MVC does (backslash-quote, not \u0022).
    /// </summary>
    [Fact(DisplayName = "Given a response When writing to the context Then status, content type, and camelCase body are set")]
    public async Task Given_Response_When_WriteAsync_Then_StatusContentTypeAndBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var response = ErrorResponse.Of(ErrorResponse.DuplicateEntry, ["already exists"]);

        // Act
        await response.WriteAsync(context, StatusCodes.Status409Conflict);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        var raw = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("\"detail\":\"[\\\"already exists\\\"]\"", raw);
        using var body = JsonDocument.Parse(raw);
        Assert.Equal(3, body.RootElement.EnumerateObject().Count());
        Assert.Equal("DuplicateEntry", body.RootElement.GetProperty("type").GetString());
        Assert.Equal("DuplicateEntry", body.RootElement.GetProperty("error").GetString());
        Assert.Equal("[\"already exists\"]", body.RootElement.GetProperty("detail").GetString());
    }
}
