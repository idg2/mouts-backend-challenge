using Ambev.DeveloperEvaluation.Common.Tracing;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

// Work item: TASK-070 (FEAT-018)
/// <summary>
/// Builds the error body of an invalid model state (a malformed JSON body, an unbindable route or query value).
/// Registered as the ApiController InvalidModelStateResponseFactory in Program.cs.
/// </summary>
public static class ModelStateErrorResponse
{
    /// <summary>The error code of every model-state failure.</summary>
    public const string InvalidBody = "InvalidBody";

    /// <summary>
    /// Creates the 400 body: one "key: message" element per model-state error, or the message alone when the key is
    /// empty; an error without a message uses its exception message.
    /// </summary>
    /// <param name="modelState">The invalid model state</param>
    /// <returns>The body</returns>
    public static ErrorResponse Create(ModelStateDictionary modelState)
    {
        var messages = modelState
            .SelectMany(entry => entry.Value!.Errors.Select(error => Describe(entry.Key, error)))
            .ToList();

        StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "modelBinding")]);
        StepTrace.Step("CMN-RSP-03", "400 error body from the model state", [("status", 400), ("errors", messages.Count)]);

        return ErrorResponse.Of(ErrorResponse.ValidationError, messages, InvalidBody);
    }

    private static string Describe(string key, ModelError error)
    {
        var message = string.IsNullOrEmpty(error.ErrorMessage)
            ? error.Exception?.Message ?? "Invalid value"
            : error.ErrorMessage;

        return string.IsNullOrEmpty(key) || key == "$" ? message : $"{key}: {message}";
    }
}
