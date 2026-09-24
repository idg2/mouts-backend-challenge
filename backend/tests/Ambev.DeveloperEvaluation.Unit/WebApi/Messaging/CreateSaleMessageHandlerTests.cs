using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using MediatR;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging;

// Work item: TASK-038 (FEAT-006)
/// <summary>
/// Contains unit tests for the <see cref="CreateSaleMessageHandler"/> class.
/// </summary>
public class CreateSaleMessageHandlerTests
{
    /// <summary>
    /// Tests that a consumed message runs through the mediator unchanged.
    /// </summary>
    [Fact(DisplayName = "Given a create sale message When handled Then sends it to the mediator")]
    public async Task Given_CreateSaleMessage_When_Handled_Then_SendsItToTheMediator()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var command = new CreateSaleCommand { Id = Guid.NewGuid() };

        // Act
        await new CreateSaleMessageHandler(mediator).Handle(command);

        // Assert
        await mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }
}
