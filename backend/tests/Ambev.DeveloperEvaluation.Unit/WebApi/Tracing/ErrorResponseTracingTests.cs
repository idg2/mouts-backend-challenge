#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-070 (FEAT-018)
/// <summary>
/// Checks that the three error-body producers outside the middleware trace the outcome and their shape key.
/// </summary>
[Collection("StepTrace")]
public class ErrorResponseTracingTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    public ErrorResponseTracingTests()
    {
        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    /// <summary>
    /// Tests that BadRequest(ValidationResult) traces the requestValidator outcome and CMN-RSP-04.
    /// </summary>
    [Fact(DisplayName = "Given a failed request validation When the controller answers Then RSP-01 requestValidator and RSP-04 are traced")]
    public void Given_FailedRequestValidation_When_BadRequest_Then_Rsp01AndRsp04()
    {
        // Arrange
        var controller = new TestController();

        // Act
        controller.Answer(new ValidationResult([new ValidationFailure("Name", "required")]));

        // Assert
        _events.Select(step => step.Key).Should().Equal("CMN-RSP-01", "CMN-RSP-04");
        _events[0].Values.Should().Contain(("outcome", "requestValidator"));
    }

    /// <summary>
    /// Tests that the model-state factory traces the modelBinding outcome and CMN-RSP-03.
    /// </summary>
    [Fact(DisplayName = "Given invalid model state When creating the response Then RSP-01 modelBinding and RSP-03 are traced")]
    public void Given_InvalidModelState_When_Create_Then_Rsp01AndRsp03()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("_page", "invalid");

        // Act
        ModelStateErrorResponse.Create(modelState);

        // Assert
        _events.Select(step => step.Key).Should().Equal("CMN-RSP-01", "CMN-RSP-03");
        _events[0].Values.Should().Contain(("outcome", "modelBinding"));
    }

    /// <summary>
    /// Tests that the status-code handler traces the statusCode outcome and CMN-RSP-11.
    /// </summary>
    [Fact(DisplayName = "Given a bodiless 401 When creating the response Then RSP-01 statusCode and RSP-11 are traced")]
    public void Given_Bodiless401_When_Create_Then_Rsp01AndRsp11()
    {
        // Act
        StatusCodeErrorResponse.Create(401);

        // Assert
        _events.Select(step => step.Key).Should().Equal("CMN-RSP-01", "CMN-RSP-11");
        _events[0].Values.Should().Contain(("outcome", "statusCode"));
        _events[1].Values.Should().Contain(("status", "401"));
    }

    // Work item: TASK-070 (FEAT-018)
    /// <summary>
    /// Exposes the protected BadRequest(ValidationResult) of <see cref="BaseController"/> to the test.
    /// </summary>
    private sealed class TestController : BaseController
    {
        public IActionResult Answer(ValidationResult validationResult) => BadRequest(validationResult);
    }
}
#endif
