using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Rebus.Messages;
using Rebus.Pipeline;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging;

// Work item: TASK-076 (FEAT-003), TD-018
/// <summary>
/// Contains unit tests for the <see cref="SaleEventLogHandler"/> class, possible since the message context is
/// injected (TD-018).
/// </summary>
public class SaleEventLogHandlerTests
{
    /// <summary>
    /// Tests that an event produces one Information log line carrying the message id of the context.
    /// </summary>
    [Fact(DisplayName = "Given an event with a message id When handled Then logs one line with that id")]
    public async Task Given_EventWithMessageId_When_Handled_Then_LogsOneLineWithId()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SaleEventLogHandler>>();
        var messageContext = Substitute.For<IMessageContext>();
        messageContext.Headers.Returns(new Dictionary<string, string> { [Headers.MessageId] = "msg-1" });

        // Act
        await new SaleEventLogHandler(logger, messageContext).Handle(new SaleDeleted(Guid.NewGuid()));

        // Assert
        var log = logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Which;
        log.GetArguments()[0].Should().Be(LogLevel.Information);
        log.GetArguments()[2]!.ToString().Should().Contain("msg-1");
    }
}
