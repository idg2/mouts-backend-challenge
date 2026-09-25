using System.Diagnostics;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Common;

// Work item: TASK-034 (FEAT-016)
/// <summary>
/// Logs the outcome and duration of every request by its type name, never its content.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of LoggingBehavior
    /// </summary>
    /// <param name="logger">The logger that receives one event per request</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    // Work item: TASK-046 (FEAT-017)
    /// <summary>
    /// Handles the request, logging Information on success, Warning on a rejection the API answers with 4xx,
    /// and Error on any other exception. Exceptions are always rethrown.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        StepTrace.Step("CMN-PIP-07", "LoggingBehavior times the request", [("request", requestName)]);
        try
        {
            var response = await next();
            _logger.LogInformation("{RequestName} handled in {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception) when (IsRejection(exception))
        {
            // Type only: DuplicateEntryException messages carry e-mails and customer documents.
            _logger.LogWarning("{RequestName} rejected after {ElapsedMilliseconds} ms with {ExceptionType}",
                requestName, stopwatch.ElapsedMilliseconds, exception.GetType().Name);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "{RequestName} failed after {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    // Same exceptions ValidationExceptionMiddleware maps to 4xx.
    private static bool IsRejection(Exception exception) =>
        exception is ValidationException or KeyNotFoundException or DuplicateEntryException or UnauthorizedAccessException;
}
