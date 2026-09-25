using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Handler for processing DeleteSaleCommand requests.
/// </summary>
public class DeleteSaleHandler : IRequestHandler<DeleteSaleCommand, DeleteSaleResult>
{
    private readonly ISaleRepository _saleRepository;

    // Work item: TASK-029 (FEAT-004)
    private readonly IOutbox _outbox;

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Initializes a new instance of DeleteSaleHandler.
    /// </summary>
    /// <param name="saleRepository">The sale repository</param>
    /// <param name="outbox">The outbox the sale events are recorded in</param>
    public DeleteSaleHandler(ISaleRepository saleRepository, IOutbox outbox)
    {
        _saleRepository = saleRepository;
        _outbox = outbox;
    }

    // Work item: TASK-029 (FEAT-004), TASK-052 (FEAT-017)
    /// <summary>
    /// Handles the DeleteSaleCommand request.
    /// </summary>
    /// <param name="request">The DeleteSale command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the delete operation</returns>
    public async Task<DeleteSaleResult> Handle(DeleteSaleCommand request, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(DeleteSaleCommand)), ("saleId", request.Id)]);
        var validator = new DeleteSaleValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("SAL-DEL-02", "Delete the sale, its items cascade", [("saleId", request.Id)]);
        var success = await _saleRepository.DeleteAsync(request.Id, cancellationToken);
        StepTrace.Step("SAL-DEL-03", "Sale existed?", [("saleId", request.Id), ("existed", success)]);
        if (!success)
            throw new KeyNotFoundException($"Sale with ID {request.Id} not found");

        await _outbox.EnqueueAsync(new SaleDeleted(request.Id), cancellationToken);
        StepTrace.Step("SAL-DEL-04", "Enqueue SaleDeleted", [("saleId", request.Id), ("eventType", nameof(SaleDeleted))]);

        return new DeleteSaleResult { Success = true };
    }
}
