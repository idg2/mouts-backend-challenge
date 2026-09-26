#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-046 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepTraceActionFilter"/>.
/// </summary>
[Collection("StepTrace")]
public class StepTraceActionFilterTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    public StepTraceActionFilterTests()
    {
        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    [Fact(DisplayName = "Given an action with Authorize roles When executing Then AUT-04 reads the metadata and PIP-12 the result")]
    public async Task Given_AuthorizedAction_When_Executing_Then_MetadataAndResultTraced()
    {
        // Arrange
        var descriptor = new ActionDescriptor
        {
            EndpointMetadata = [new AuthorizeAttribute { Roles = "Admin,Manager" }],
            RouteValues = new Dictionary<string, string?> { ["controller"] = "Sales", ["action"] = "CreateSale" }
        };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor);
        var executing = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?> { ["request"] = new object() }, controller: new object());
        var executed = new ActionExecutedContext(actionContext, [], controller: new object()) { Result = new OkObjectResult("x") };

        // Act
        await new StepTraceActionFilter().OnActionExecutionAsync(executing, () => Task.FromResult(executed));

        // Assert
        _events.Select(e => e.Key).Should().Equal("CMN-PIP-03", "CMN-AUT-04", "CMN-AUT-07", "CMN-PIP-12");
        _events[1].Values.Should().Contain(("authorize", "True")).And.Contain(("roles", "Admin,Manager"));
        _events[2].Values.Should().Contain(("controller", "Sales")).And.Contain(("action", "CreateSale"));
        _events[3].Values.Should().Contain(("result", nameof(OkObjectResult))).And.Contain(("status", "200"));
    }

    [Fact(DisplayName = "Given an anonymous action When executing Then AUT-04 says no Authorize")]
    public async Task Given_AnonymousAction_When_Executing_Then_NoAuthorize()
    {
        // Arrange
        var descriptor = new ActionDescriptor { EndpointMetadata = [new AllowAnonymousAttribute()], RouteValues = new Dictionary<string, string?>() };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor);
        var executing = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: new object());
        var executed = new ActionExecutedContext(actionContext, [], controller: new object());

        // Act
        await new StepTraceActionFilter().OnActionExecutionAsync(executing, () => Task.FromResult(executed));

        // Assert
        _events.Single(e => e.Key == "CMN-AUT-04").Values.Should().Contain(("authorize", "False")).And.Contain(("anonymous", "True"));
    }
}
#endif
