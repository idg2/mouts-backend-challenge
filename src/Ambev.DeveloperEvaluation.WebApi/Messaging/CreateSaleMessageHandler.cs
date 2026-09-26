using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Common.Tracing;
using MediatR;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

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

    // Work item: TASK-054 (FEAT-017), TASK-059 (FEAT-017)
    /// <summary>
    /// Creates the sale carried by the message; the command's preset id makes a redelivery a no-op.
    /// </summary>
    /// <param name="message">The queued command</param>
    public async Task Handle(CreateSaleCommand message)
    {
        StepTrace.Step("SAL-ASY-05", "CreateSaleMessageHandler sends it through MediatR",
            [("saleId", message.Id), ("customerId", message.CustomerId), ("branchId", message.BranchId), ("items", message.Items?.Count),
             ("messageId", MessageContext.Current?.Headers.GetValueOrDefault(Headers.MessageId))]);
        try
        {
            var result = await _mediator.Send(message);
            StepTrace.Step("SAL-ASY-06", "Handled?", [("saleId", result?.Id), ("saleNumber", result?.SaleNumber), ("handled", true)]);
        }
        catch (Exception exception)
        {
            StepTrace.Step("SAL-ASY-06", "Handled?", [("saleId", message.Id), ("handled", false), ("error", exception)]);
            throw;
        }
    }
}
