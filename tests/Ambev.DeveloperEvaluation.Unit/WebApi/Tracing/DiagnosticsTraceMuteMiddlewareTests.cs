#if DEBUG
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-081 (FEAT-019)
/// <summary>
/// Contains unit tests for the <see cref="DiagnosticsTraceMuteMiddleware"/> class.
/// </summary>
public class DiagnosticsTraceMuteMiddlewareTests
{
    // Work item: TASK-081 (FEAT-019)
    [Theory(DisplayName = "Given a request path When the middleware runs Then only diagnostics requests are muted")]
    [InlineData("/api/diagnostics", true)]
    [InlineData("/api/diagnostics/trace", true)]
    [InlineData("/API/Diagnostics/outbox", true)]
    [InlineData("/api/sales", false)]
    [InlineData("/api/diagnosticsx", false)]
    public async Task Given_RequestPath_When_MiddlewareRuns_Then_OnlyDiagnosticsMuted(string path, bool muted)
    {
        // Arrange
        bool? seenByNext = null;
        var middleware = new DiagnosticsTraceMuteMiddleware(_ =>
        {
            seenByNext = TraceBuffer.Muted;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        seenByNext.Should().Be(muted);
        TraceBuffer.Muted.Should().BeFalse("the flag must not leak to the caller of the middleware");
    }
}
#endif
