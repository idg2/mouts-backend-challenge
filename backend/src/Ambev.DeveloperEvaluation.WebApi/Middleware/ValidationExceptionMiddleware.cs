using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware
{
    public class ValidationExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidationExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Work item: TASK-017 (FEAT-010), BUG-002, BUG-003, TASK-046 (FEAT-017)
        public async Task InvokeAsync(HttpContext context)
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
            catch (Exception ex)
            {
                StepTrace.Step("CMN-RSP-01", "How did the rest of the pipeline return?", [("outcome", "exception")]);
                StepTrace.Step("CMN-RSP-05", "Exception type?", [("exception", ex.GetType().Name), ("handled", false)]);
                throw;
            }
        }

        // Work item: TASK-046 (FEAT-017)
        private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            StepTrace.Step("CMN-RSP-06", "400 envelope with error and detail per failure", [("status", 400), ("errors", exception.Errors.Count())]);

            var response = new ApiResponse
            {
                Success = false,
                Message = "Validation Failed",
                Errors = exception.Errors
                    .Select(error => (ValidationErrorDetail)error)
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }

        // Work item: TASK-017 (FEAT-010), TASK-046 (FEAT-017)
        private static Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            StepTrace.Step("CMN-RSP-07", "404 envelope with the message", [("status", 404), ("message", exception.Message)]);

            var response = new ApiResponse
            {
                Success = false,
                Message = exception.Message
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }

        // Work item: BUG-002, TASK-046 (FEAT-017)
        private static Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            StepTrace.Step("CMN-RSP-08", "401 envelope with the message", [("status", 401), ("message", exception.Message)]);

            var response = new ApiResponse
            {
                Success = false,
                Message = exception.Message
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }

        // Work item: BUG-003, TASK-046 (FEAT-017)
        private static Task HandleDuplicateEntryExceptionAsync(HttpContext context, DuplicateEntryException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            // Type only: the message embeds the e-mail or the customer document.
            StepTrace.Step("CMN-RSP-09", "409 envelope with the message", [("status", 409), ("exception", exception.GetType().Name)]);

            var response = new ApiResponse
            {
                Success = false,
                Message = exception.Message
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }
    }
}
