using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using MediatR;
using Rebus.Handlers;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-038 (FEAT-006)
/// <summary>
/// Consumes the sale intake queue: each <see cref="CreateSaleCommand"/> runs through the same MediatR pipeline as the
/// synchronous endpoint. Rebus opens one DI scope per message, so each message gets its own database context.
/// </summary>
public class CreateSaleMessageHandler : IHandleMessages<CreateSaleCommand>
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of CreateSaleMessageHandler
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    public CreateSaleMessageHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates the sale carried by the message; the command's preset id makes a redelivery a no-op.
    /// </summary>
    /// <param name="message">The queued command</param>
    public async Task Handle(CreateSaleCommand message)
    {
        await _mediator.Send(message);
    }
}
