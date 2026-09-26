using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: BUG-001, TD-007 (FEAT-010), TASK-069 (FEAT-018)
/// <summary>
/// Contains unit tests for the <see cref="BaseController"/> class.
/// </summary>
public class BaseControllerTests
{
    // Work item: TD-007 (FEAT-010)
    /// <summary>
    /// Tests that Ok returns the given response as the body, without wrapping it in another ApiResponse (BUG-004).
    /// </summary>
    [Fact(DisplayName = "Given an ApiResponse When calling Ok Then the body is that response, not wrapped again")]
    public void Given_ApiResponse_When_Ok_Then_BodyIsNotWrappedAgain()
    {
        // Arrange
        var response = new ApiResponseWithData<string> { Success = true, Message = "ok", Data = "value" };
        var controller = new TestController();

        // Act
        var result = controller.OkResponse(response);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    // Work item: TASK-069 (FEAT-018)
    /// <summary>
    /// Tests that BadRequest(ValidationResult) answers 400 with a ValidationError body built from the failures.
    /// </summary>
    [Fact(DisplayName = "Given a failed ValidationResult When calling BadRequest Then the body is a ValidationError with the messages")]
    public void Given_FailedValidationResult_When_BadRequest_Then_ValidationErrorBody()
    {
        // Arrange
        var validationResult = new ValidationResult(
        [
            new ValidationFailure("Name", "'Name' must not be empty.") { ErrorCode = "NotEmptyValidator" }
        ]);
        var controller = new TestController();

        // Act
        var result = controller.BadRequestResult(validationResult);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Equal(ErrorResponse.ValidationError, body.Type);
        Assert.Equal("NotEmptyValidator", body.Error);
        Assert.Equal(new[] { "Name: 'Name' must not be empty." }, JsonSerializer.Deserialize<string[]>(body.Detail));
    }

    // Work item: TASK-069 (FEAT-018)
    /// <summary>
    /// Tests that NotFound(message) answers 404 with a ResourceNotFound body.
    /// </summary>
    [Fact(DisplayName = "Given a message When calling NotFound Then the body is a ResourceNotFound with that message")]
    public void Given_Message_When_NotFound_Then_ResourceNotFoundBody()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.NotFoundResult("Sale 1 not found");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var body = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(ErrorResponse.ResourceNotFound, body.Type);
        Assert.Equal(new[] { "Sale 1 not found" }, JsonSerializer.Deserialize<string[]>(body.Detail));
    }

    // Work item: TASK-069 (FEAT-018)
    /// <summary>
    /// Tests that BadRequest(message) answers 400 with a ValidationError body.
    /// </summary>
    [Fact(DisplayName = "Given a message When calling BadRequest Then the body is a ValidationError with that message")]
    public void Given_Message_When_BadRequest_Then_ValidationErrorBody()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.BadRequestResult("Bad input");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Equal(ErrorResponse.ValidationError, body.Type);
        Assert.Equal(new[] { "Bad input" }, JsonSerializer.Deserialize<string[]>(body.Detail));
    }

    /// <summary>
    /// Exposes the protected members of <see cref="BaseController"/> to the tests.
    /// </summary>
    private sealed class TestController : BaseController
    {
        // Work item: TD-007 (FEAT-010)
        public IActionResult OkResponse(object value) => Ok(value);

        // Work item: TASK-069 (FEAT-018)
        public IActionResult BadRequestResult(ValidationResult validationResult) => BadRequest(validationResult);

        // Work item: TASK-069 (FEAT-018)
        public IActionResult BadRequestResult(string message) => BadRequest(message);

        // Work item: TASK-069 (FEAT-018)
        public IActionResult NotFoundResult(string message) => NotFound(message);
    }
}
