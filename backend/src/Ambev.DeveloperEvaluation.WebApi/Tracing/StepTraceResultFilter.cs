#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-046 (FEAT-017)
/// <summary>
/// Traces CMN-RSP-01 with the outcome kind and then the shape the response takes: the envelope (CMN-RSP-02),
/// ProblemDetails from model binding or an unsupported content type (CMN-RSP-03), or the raw FluentValidation
/// failure list (CMN-RSP-04). Always runs, so short-circuited results are seen too. Debug builds only.
/// </summary>
public sealed class StepTraceResultFilter : IAlwaysRunResultFilter
{
    /// <inheritdoc />
    public void OnResultExecuting(ResultExecutingContext context)
    {
        switch (context.Result)
        {
            case BadRequestObjectResult { Value: IEnumerable<ValidationFailure> failures }:
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "requestValidator")]);
                StepTrace.Step("CMN-RSP-04", "400 with the FluentValidation failure list", [("status", 400), ("errors", failures.Count())]);
                break;
            case ObjectResult { Value: ApiResponse envelope } objectResult:
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "success")]);
                StepTrace.Step("CMN-RSP-02", "Envelope with success, message, and data",
                    [("status", objectResult.StatusCode), ("success", envelope.Success), ("paginated", IsPaginated(envelope))]);
                break;
            case ObjectResult { Value: ProblemDetails problem }:
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "modelBinding")]);
                StepTrace.Step("CMN-RSP-03", "ProblemDetails 400 or 415",
                    [("status", problem.Status), ("errors", (problem as ValidationProblemDetails)?.Errors.Count)]);
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
