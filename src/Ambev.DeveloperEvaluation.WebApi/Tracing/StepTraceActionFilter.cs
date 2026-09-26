#if DEBUG
using System.Security.Claims;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-046 (FEAT-017)
/// <summary>
/// Traces CMN-PIP-03 (model binding done), CMN-AUT-04 and CMN-AUT-07 (the endpoint's Authorize metadata and the
/// action about to run), and CMN-PIP-12 (the result). Registered with the lowest order so it runs before the
/// ApiController model-state filter. Debug builds only.
/// </summary>
public sealed class StepTraceActionFilter : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        StepTrace.Step("CMN-PIP-03", "Model binding reads route and JSON body",
            [("modelValid", context.ModelState.IsValid), ("arguments", context.ActionArguments.Count), ("errors", context.ModelState.ErrorCount)]);

        var metadata = context.ActionDescriptor.EndpointMetadata;
        var authorize = metadata.OfType<IAuthorizeData>().ToList();
        StepTrace.Step("CMN-AUT-04", "Endpoint has Authorize?",
            [("authorize", authorize.Count > 0), ("anonymous", metadata.OfType<IAllowAnonymous>().Any()), ("roles", string.Join(";", authorize.Select(data => data.Roles)))]);

        var routeValues = context.ActionDescriptor.RouteValues;
        var user = context.HttpContext.User;
        StepTrace.Step("CMN-AUT-07", "Controller action runs",
            [("controller", routeValues.TryGetValue("controller", out var controller) ? controller : null),
             ("action", routeValues.TryGetValue("action", out var action) ? action : null),
             ("authenticated", user.Identity?.IsAuthenticated == true),
             ("role", user.FindFirst(ClaimTypes.Role)?.Value),
             ("willRun", context.ModelState.IsValid)]);

        var executed = await next();
        StepTrace.Step("CMN-PIP-12", "Result mapped and wrapped, see CMN-RSP",
            [("result", executed.Result?.GetType().Name), ("status", (executed.Result as IStatusCodeActionResult)?.StatusCode), ("exception", executed.Exception?.GetType().Name)]);
    }
}
#endif
