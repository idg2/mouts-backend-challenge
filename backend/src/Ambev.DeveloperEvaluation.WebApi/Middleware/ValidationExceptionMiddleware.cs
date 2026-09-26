using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware
{
    // Work item: TASK-068 (FEAT-018)
    /// <summary>
    /// Turns every exception that leaves the pipeline into the general-api error body: 400 for validation, 404 for
    /// a missing key, 401 for a failed login, 409 for a duplicate, a Kestrel request error's own status (413, 400)
    /// with the catalog text, and 500 for anything else (logged here, since it is the only place that sees
    /// exceptions thrown outside MediatR). Bodiless statuses are handled by StatusCodeErrorResponse (Common) instead.
    /// </summary>
    public class ValidationExceptionMiddleware
    {
        // Work item: TASK-068 (FEAT-018)
        private const string ServerErrorDetail = "An unexpected error occurred";

        private readonly RequestDelegate _next;

        public ValidationExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Work item: TASK-017 (FEAT-010), BUG-002, BUG-003, TASK-046 (FEAT-017), TASK-068 (FEAT-018)
        public async Task InvokeAsync(HttpContext context, ILogger<ValidationExceptionMiddleware> logger)
        {
            StepTrace.Step("CMN-PIP-02", "Exception middleware wraps the rest", [("path", context.Request.Path.Value)]);
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "exception")]);
                StepTrace.Step("CMN-RSP-05", "Exception type?", [("exception", ex.GetType().Name), ("handled", true)]);
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (KeyNotFoundException ex)
            {
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "exception")]);
                StepTrace.Step("CMN-RSP-05", "Exception type?", [("exception", ex.GetType().Name), ("handled", true)]);
                await HandleNotFoundExceptionAsync(context, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "exception")]);
                StepTrace.Step("CMN-RSP-05", "Exception type?", [("exception", ex.GetType().Name), ("handled", true)]);
                await HandleUnauthorizedExceptionAsync(context, ex);
            }
            catch (DuplicateEntryException ex)
            {
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "exception")]);
                StepTrace.Step("CMN-RSP-05", "Exception type?", [("exception", ex.GetType().Name), ("handled", true)]);
                await HandleDuplicateEntryExceptionAsync(context, ex);
            }
            catch (BadHttpRequestException ex) when (!context.Response.HasStarted)
            {
                // Kestrel already chose the status (413 for a body over the limit, 400 for a bad chunked body);
                // keep it and give it the catalog body instead of a 500.
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "exception")]);
                StepTrace.Step("CMN-RSP-05", "Exception type?", [("exception", ex.GetType().Name), ("handled", true)]);
                await StatusCodeErrorResponse.Create(ex.StatusCode).WriteAsync(context, ex.StatusCode);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "exception")]);
                StepTrace.Step("CMN-RSP-05", "Exception type?", [("exception", ex.GetType().Name), ("handled", true)]);
                await HandleUnexpectedExceptionAsync(context, ex, logger);
            }
        }

        // Work item: TASK-046 (FEAT-017), TASK-068 (FEAT-018)
        private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            var failures = exception.Errors.ToList();
            StepTrace.Step("CMN-RSP-06", "400 ValidationError, one detail per failure", [("status", 400), ("errors", failures.Count)]);

            var response = failures.Count > 0
                ? ErrorResponse.Validation(failures)
                : ErrorResponse.Of(ErrorResponse.ValidationError, [exception.Message]);

            return response.WriteAsync(context, StatusCodes.Status400BadRequest);
        }

        // Work item: TASK-017 (FEAT-010), TASK-046 (FEAT-017), TASK-068 (FEAT-018)
        private static Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException exception)
        {
            StepTrace.Step("CMN-RSP-07", "404 ResourceNotFound", [("status", 404), ("message", exception.Message)]);
            return ErrorResponse.Of(ErrorResponse.ResourceNotFound, [exception.Message])
                .WriteAsync(context, StatusCodes.Status404NotFound);
        }

        // Work item: BUG-002, TASK-046 (FEAT-017), TASK-068 (FEAT-018)
        private static Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
        {
            StepTrace.Step("CMN-RSP-08", "401 AuthenticationError", [("status", 401), ("message", exception.Message)]);
            return ErrorResponse.Of(ErrorResponse.AuthenticationError, [exception.Message])
                .WriteAsync(context, StatusCodes.Status401Unauthorized);
        }

        // Work item: BUG-003, TASK-046 (FEAT-017), TASK-068 (FEAT-018)
        private static Task HandleDuplicateEntryExceptionAsync(HttpContext context, DuplicateEntryException exception)
        {
            // Type only: the message embeds the e-mail or the customer document.
            StepTrace.Step("CMN-RSP-09", "409 DuplicateEntry", [("status", 409), ("exception", exception.GetType().Name)]);
            return ErrorResponse.Of(ErrorResponse.DuplicateEntry, [exception.Message])
                .WriteAsync(context, StatusCodes.Status409Conflict);
        }

        // Work item: TASK-068 (FEAT-018)
        /// <summary>
        /// Logs the exception with the request method and path and answers 500 with a fixed detail: the body never
        /// carries the exception message, type, or stack.
        /// </summary>
        private static Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception, ILogger logger)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path.Value);
            StepTrace.Step("CMN-RSP-10", "500 ServerError, exception logged", [("status", 500), ("exception", exception.GetType().Name)]);
            return ErrorResponse.Of(ErrorResponse.ServerError, [ServerErrorDetail])
                .WriteAsync(context, StatusCodes.Status500InternalServerError);
        }
    }
}
