using Ambev.DeveloperEvaluation.Common.Tracing;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected IActionResult Created<T>(string routeName, object routeValues, T data) =>
        base.CreatedAtRoute(routeName, routeValues, new ApiResponseWithData<T> { Data = data, Success = true });

    // Work item: TASK-069 (FEAT-018)
    /// <summary>
    /// Answers 400 with the general-api body built from a request validator's failures. Every controller calls this
    /// when its request validator fails; the trace keys of the request-validator outcome live here.
    /// </summary>
    /// <param name="validationResult">The failed result</param>
    /// <returns>The 400 result</returns>
    protected IActionResult BadRequest(ValidationResult validationResult)
    {
        StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "requestValidator")]);
        StepTrace.Step("CMN-RSP-04", "400 error body from the request validator", [("status", 400), ("errors", validationResult.Errors.Count)]);
        return base.BadRequest(ErrorResponse.Validation(validationResult.Errors));
    }

    // Work item: TASK-069 (FEAT-018)
    protected IActionResult BadRequest(string message) =>
        base.BadRequest(ErrorResponse.Of(ErrorResponse.ValidationError, [message]));

    // Work item: TASK-069 (FEAT-018)
    protected IActionResult NotFound(string message = "Resource not found") =>
        base.NotFound(ErrorResponse.Of(ErrorResponse.ResourceNotFound, [message]));

    // Work item: BUG-004 (FEAT-010)
    protected IActionResult OkPaginated<T>(PaginatedList<T> pagedList) =>
            Ok(new PaginatedResponse<T>
            {
                Data = pagedList,
                CurrentPage = pagedList.CurrentPage,
                TotalPages = pagedList.TotalPages,
                TotalCount = pagedList.TotalCount,
                Success = true
            });
}
