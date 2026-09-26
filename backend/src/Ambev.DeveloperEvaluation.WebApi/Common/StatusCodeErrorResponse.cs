using Ambev.DeveloperEvaluation.Common.Tracing;
using Microsoft.AspNetCore.WebUtilities;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

// Work item: TASK-070 (FEAT-018)
/// <summary>
/// Builds the error body of a 4xx or 5xx response that left the pipeline without a body: the JWT challenge (401)
/// and forbidden result (403), a route miss (404), a wrong method (405), a wrong Content-Type (415), and anything
/// else the framework answers bare. Registered through UseStatusCodePages in Program.cs.
/// </summary>
public static class StatusCodeErrorResponse
{
    /// <summary>
    /// Creates the body for a status: the catalog row for the known statuses, HttpError with the reason phrase
    /// for the rest.
    /// </summary>
    /// <param name="statusCode">The response status</param>
    /// <returns>The body</returns>
    public static ErrorResponse Create(int statusCode)
    {
        StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "statusCode")]);
        StepTrace.Step("CMN-RSP-11", "Bodiless status filled with the error body", [("status", statusCode)]);

        return statusCode switch
        {
            StatusCodes.Status401Unauthorized => ErrorResponse.Of(ErrorResponse.AuthenticationError, ["A valid bearer token is required"]),
            StatusCodes.Status403Forbidden => ErrorResponse.Of(ErrorResponse.AuthorizationError, ["The authenticated user is not allowed to perform this action"]),
            StatusCodes.Status404NotFound => ErrorResponse.Of(ErrorResponse.ResourceNotFound, ["No endpoint matches the request path"]),
            StatusCodes.Status405MethodNotAllowed => ErrorResponse.Of(ErrorResponse.MethodNotAllowed, ["The HTTP method is not allowed on this path"]),
            StatusCodes.Status415UnsupportedMediaType => ErrorResponse.Of(ErrorResponse.UnsupportedMediaType, ["The request Content-Type is not supported"]),
            _ => ErrorResponse.Of(ErrorResponse.HttpError, [ReasonPhrases.GetReasonPhrase(statusCode)])
        };
    }
}
