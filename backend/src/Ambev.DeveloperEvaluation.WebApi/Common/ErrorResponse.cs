using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation.Results;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

// Work item: TASK-067 (FEAT-018)
/// <summary>
/// The error body of every 4xx and 5xx response: the contract of .doc/general-api.md. <see cref="Detail"/> is a string
/// holding a JSON array with one message per failure, never empty. Only the middleware, the controller base, the
/// model-state factory, and the status-code handler build instances; handlers and controllers never do.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>A request or command failed validation, or broke a business rule (400).</summary>
    public const string ValidationError = "ValidationError";

    /// <summary>The resource or the route does not exist (404).</summary>
    public const string ResourceNotFound = "ResourceNotFound";

    /// <summary>Login failed or the bearer token is missing, invalid, or expired (401).</summary>
    public const string AuthenticationError = "AuthenticationError";

    /// <summary>The authenticated user's role is not allowed (403).</summary>
    public const string AuthorizationError = "AuthorizationError";

    /// <summary>The HTTP method is not allowed on the path (405).</summary>
    public const string MethodNotAllowed = "MethodNotAllowed";

    /// <summary>A unique value already exists (409).</summary>
    public const string DuplicateEntry = "DuplicateEntry";

    /// <summary>The request Content-Type is not supported (415).</summary>
    public const string UnsupportedMediaType = "UnsupportedMediaType";

    /// <summary>An unexpected exception (500).</summary>
    public const string ServerError = "ServerError";

    /// <summary>Any other status the framework produced without a body.</summary>
    public const string HttpError = "HttpError";

    /// <summary>
    /// The serializer options every producer uses: camelCase names and the relaxed encoder MVC uses, so the inner
    /// quotes of <see cref="Detail"/> come out as backslash-quote whichever path wrote the body.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Initializes the body. An empty message list is replaced by one element equal to <paramref name="error"/>.
    /// </summary>
    private ErrorResponse(string type, string error, IReadOnlyList<string> messages)
    {
        Type = type;
        Error = error;
        Detail = JsonSerializer.Serialize(messages.Count == 0 ? [error] : messages, JsonOptions);
    }

    /// <summary>
    /// Gets the error category (one of the constants of this class).
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the code of the first failure, or the type when the category has no codes.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets the JSON array of messages, serialized as a string.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Builds a <see cref="ValidationError"/> body: the error is the first failure's code (or the type when the
    /// code is null or empty) and each failure is one element of the detail, as "property: message" when the
    /// failure names a property (so a sale line failure reads "Items[1].Quantity: ...") or the message alone.
    /// </summary>
    /// <param name="failures">The FluentValidation failures, in the order they were produced</param>
    /// <returns>The body</returns>
    public static ErrorResponse Validation(IEnumerable<ValidationFailure> failures)
    {
        var list = failures.ToList();
        var firstCode = list.Count > 0 ? list[0].ErrorCode : null;
        return new ErrorResponse(
            ValidationError,
            string.IsNullOrEmpty(firstCode) ? ValidationError : firstCode,
            list.Select(failure => string.IsNullOrEmpty(failure.PropertyName)
                ? failure.ErrorMessage
                : $"{failure.PropertyName}: {failure.ErrorMessage}").ToList());
    }

    /// <summary>
    /// Builds a body of any category from plain messages.
    /// </summary>
    /// <param name="type">The category</param>
    /// <param name="messages">The detail elements, in order</param>
    /// <param name="error">The error code; defaults to <paramref name="type"/></param>
    /// <returns>The body</returns>
    public static ErrorResponse Of(string type, IEnumerable<string> messages, string? error = null) =>
        new(type, error ?? type, messages.ToList());

    /// <summary>
    /// Sets the status code and the JSON content type and writes this body.
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="statusCode">The HTTP status</param>
    /// <returns>The write task</returns>
    public Task WriteAsync(HttpContext context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(this, JsonOptions));
    }
}
