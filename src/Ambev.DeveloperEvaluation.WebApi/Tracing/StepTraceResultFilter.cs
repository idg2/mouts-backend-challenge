#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-046 (FEAT-017), TASK-069 (FEAT-018)
/// <summary>
/// Traces CMN-RSP-01 with the outcome kind and then the shape the response takes: the envelope (CMN-RSP-02) or
/// another result. An ErrorResponse result is skipped because its producer (BaseController for the request
/// validator, ModelStateErrorResponse for model binding) already traced CMN-RSP-04 or CMN-RSP-03. Always runs, so
/// short-circuited results are seen too. Debug builds only.
/// </summary>
public sealed class StepTraceResultFilter : IAlwaysRunResultFilter
{
    // Work item: TASK-069 (FEAT-018)
    /// <inheritdoc />
    public void OnResultExecuting(ResultExecutingContext context)
    {
        switch (context.Result)
        {
            case ObjectResult { Value: ErrorResponse }:
                // Traced by its producer: BaseController.BadRequest(ValidationResult) or ModelStateErrorResponse.
                break;
            case ObjectResult { Value: ApiResponse envelope } objectResult:
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "success")]);
                StepTrace.Step("CMN-RSP-02", "Envelope with success, message, and data",
                    [("status", objectResult.StatusCode), ("success", envelope.Success), ("paginated", IsPaginated(envelope))]);
                break;
            default:
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?",
                    [("outcome", "other"), ("result", context.Result.GetType().Name)]);
                break;
        }
    }

    /// <inheritdoc />
    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static bool IsPaginated(ApiResponse envelope)
    {
        var type = envelope.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PaginatedResponse<>);
    }
}
#endif
