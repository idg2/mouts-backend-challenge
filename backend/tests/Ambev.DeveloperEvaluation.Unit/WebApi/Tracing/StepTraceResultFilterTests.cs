#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-046 (FEAT-017), TASK-069 (FEAT-018)
/// <summary>
/// Contains unit tests for the <see cref="StepTraceResultFilter"/>.
/// </summary>
[Collection("StepTrace")]
public class StepTraceResultFilterTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    public StepTraceResultFilterTests()
    {
        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    // Work item: TASK-069 (FEAT-018)
    [Theory(DisplayName = "Given a result When executing Then RSP-01 names the outcome and the shape key follows, or nothing for an error body")]
    [MemberData(nameof(Results))]
    public void Given_Result_When_Executing_Then_OutcomeAndShape(IActionResult result, string? outcome, string? shapeKey)
    {
        // Arrange
        var context = new ResultExecutingContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), [], result, controller: new object());

        // Act
        new StepTraceResultFilter().OnResultExecuting(context);

        // Assert
        if (outcome is null)
        {
            _events.Should().BeEmpty("an ErrorResponse result is traced by its producer, not by the filter");
            return;
        }

        _events[0].Key.Should().Be("CMN-RSP-01");
        _events[0].Values.Should().Contain(("outcome", outcome));
        if (shapeKey is null)
            _events.Should().HaveCount(1);
        else
            _events[1].Key.Should().Be(shapeKey);
    }

    public static IEnumerable<object?[]> Results()
    {
        yield return [new OkObjectResult(new ApiResponse { Success = true }), "success", "CMN-RSP-02"];
        yield return [new BadRequestObjectResult(ErrorResponse.Of(ErrorResponse.ValidationError, ["x"])), null, null];
        yield return [new NoContentResult(), "other", null];
    }
}
#endif
