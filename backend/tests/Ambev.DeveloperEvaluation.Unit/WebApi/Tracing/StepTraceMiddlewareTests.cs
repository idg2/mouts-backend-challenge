#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-046 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepTraceMiddleware"/>.
/// </summary>
[Collection("StepTrace")]
public class StepTraceMiddlewareTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    public StepTraceMiddlewareTests()
    {
        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    [Fact(DisplayName = "Given a request When the pipeline answers Then PIP-01 and PIP-13 carry method, path, and status")]
    public async Task Given_Request_When_Answered_Then_Pip01AndPip13()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/sales";
        var middleware = new StepTraceMiddleware(ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _events.Select(e => e.Key).Should().Equal("CMN-PIP-01", "CMN-PIP-13");
        _events[1].Values.Should().Contain(("status", "200")).And.Contain(("path", "/api/sales"));
    }

    [Fact(DisplayName = "Given a request When the pipeline throws Then PIP-13 prints 500 and the exception rethrows")]
    public async Task Given_Request_When_Throws_Then_Pip13WithFiveHundred()
    {
        // Arrange
        var middleware = new StepTraceMiddleware(_ => throw new InvalidOperationException("boom"));

        // Act
        var act = () => middleware.InvokeAsync(new DefaultHttpContext());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _events.Last().Key.Should().Be("CMN-PIP-13");
        _events.Last().Values.Should().Contain(("status", "500"));
    }
}
#endif
