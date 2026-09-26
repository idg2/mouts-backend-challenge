using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Common;

// Work item: TASK-034 (FEAT-016)
/// <summary>
/// Contains unit tests for the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
/// </summary>
public class LoggingBehaviorTests
{
    private const string Secret = "s3cr3t-payload-value";

    private readonly ILogger<LoggingBehavior<ProbeRequest, string>> _logger =
        Substitute.For<ILogger<LoggingBehavior<ProbeRequest, string>>>();

    private readonly LoggingBehavior<ProbeRequest, string> _behavior;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehaviorTests"/> class.
    /// </summary>
    public LoggingBehaviorTests()
    {
        _behavior = new LoggingBehavior<ProbeRequest, string>(_logger);
    }

    /// <summary>
    /// The exceptions the middleware turns into 4xx responses.
    /// </summary>
    public static TheoryData<Exception> ExpectedExceptions => new()
    {
        new ValidationException("Validation failed"),
        new KeyNotFoundException("Sale not found"),
        new DuplicateEntryException("User with email someone@example.com already exists"),
        new UnauthorizedAccessException("Invalid credentials")
    };

    /// <summary>
    /// Tests that a successful request logs Information with the request name.
    /// </summary>
    [Fact(DisplayName = "Given a handler that succeeds When handling Then logs Information with the request name")]
    public async Task Given_HandlerSucceeds_When_Handling_Then_LogsInformation()
    {
        // Arrange
        var request = new ProbeRequest(Secret);

        // Act
        var result = await _behavior.Handle(request, () => Task.FromResult("done"), CancellationToken.None);

        // Assert
        result.Should().Be("done");
        var entry = _logger.Entries().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().StartWith("ProbeRequest handled in ").And.EndWith(" ms");
        entry.Exception.Should().BeNull();
    }

    /// <summary>
    /// Tests that an expected exception logs a Warning with its type only and is rethrown.
    /// </summary>
    [Theory(DisplayName = "Given an expected exception When handling Then logs a Warning with the type only and rethrows")]
    [MemberData(nameof(ExpectedExceptions))]
    public async Task Given_ExpectedException_When_Handling_Then_LogsWarningWithTypeOnlyAndRethrows(Exception failure)
    {
        // Arrange
        var request = new ProbeRequest(Secret);

        // Act
        var act = () => _behavior.Handle(request, () => throw failure, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(failure);
        var entry = _logger.Entries().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().StartWith("ProbeRequest rejected after ").And.EndWith($" ms with {failure.GetType().Name}");
        entry.Message.Should().NotContain(failure.Message);
        entry.Exception.Should().BeNull();
    }

    /// <summary>
    /// Tests that an unexpected exception logs an Error with the exception attached and is rethrown.
    /// </summary>
    [Fact(DisplayName = "Given an unexpected exception When handling Then logs an Error with the exception and rethrows")]
    public async Task Given_UnexpectedException_When_Handling_Then_LogsErrorWithExceptionAndRethrows()
    {
        // Arrange
        var failure = new InvalidOperationException("boom");

        // Act
        var act = () => _behavior.Handle(new ProbeRequest(Secret), () => throw failure, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        var entry = _logger.Entries().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Message.Should().StartWith("ProbeRequest failed after ").And.EndWith(" ms");
        entry.Exception.Should().BeSameAs(failure);
    }

    /// <summary>
    /// Tests that no log entry contains a value carried by the request.
    /// </summary>
    [Theory(DisplayName = "Given a request carrying a secret When handling Then no log contains it")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Given_RequestCarryingSecret_When_Handling_Then_NoLogContainsIt(bool handlerFails)
    {
        // Arrange
        RequestHandlerDelegate<string> next;
        if (handlerFails)
            next = () => throw new InvalidOperationException("boom");
        else
            next = () => Task.FromResult("done");

        // Act
        try
        {
            await _behavior.Handle(new ProbeRequest(Secret), next, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // The failing case rethrows by design; only the logs matter here.
        }

        // Assert
        var entries = _logger.Entries();
        entries.Should().NotBeEmpty();
        entries.Where(entry =>
            entry.Message.Contains(Secret) ||
            entry.Properties.Any(property => (property.Value?.ToString() ?? string.Empty).Contains(Secret)))
            .Should().BeEmpty();
    }

    // Work item: TASK-034 (FEAT-016)
    /// <summary>
    /// A request that carries a value no log may contain. Public so NSubstitute can proxy the generic logger.
    /// </summary>
    public record ProbeRequest(string Secret) : IRequest<string>;
}
