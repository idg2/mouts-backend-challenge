using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: TASK-070 (FEAT-018)
/// <summary>
/// Contains unit tests for the <see cref="ModelStateErrorResponse"/> class.
/// </summary>
public class ModelStateErrorResponseTests
{
    /// <summary>
    /// Tests that each model-state error becomes one "key: message" element and the error code is InvalidBody.
    /// </summary>
    [Fact(DisplayName = "Given two model-state errors When creating the response Then detail has key: message per error")]
    public void Given_TwoErrors_When_Create_Then_KeyMessagePerError()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("_page", "The value 'x' is not valid.");
        modelState.AddModelError("items[0].quantity", "The JSON value could not be converted to System.Int32.");

        // Act
        var response = ModelStateErrorResponse.Create(modelState);

        // Assert
        Assert.Equal(ErrorResponse.ValidationError, response.Type);
        Assert.Equal("InvalidBody", response.Error);
        Assert.Equal(
            new[] { "_page: The value 'x' is not valid.", "items[0].quantity: The JSON value could not be converted to System.Int32." },
            JsonSerializer.Deserialize<string[]>(response.Detail));
    }

    /// <summary>
    /// Tests that an entry with an empty key and an exception instead of a message uses the exception message alone
    /// (a malformed JSON body, Review Focus 5).
    /// </summary>
    [Fact(DisplayName = "Given an empty key with an exception When creating the response Then detail is the exception message")]
    public void Given_EmptyKeyWithException_When_Create_Then_ExceptionMessage()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, new JsonException("'x' is an invalid start of a value."), new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)));

        // Act
        var response = ModelStateErrorResponse.Create(modelState);

        // Assert
        Assert.Equal(new[] { "'x' is an invalid start of a value." }, JsonSerializer.Deserialize<string[]>(response.Detail));
    }
}
