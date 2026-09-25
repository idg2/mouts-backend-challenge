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

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Handles the DeleteSaleCommand request.
    /// </summary>
    /// <param name="request">The DeleteSale command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the delete operation</returns>
    public async Task<DeleteSaleResult> Handle(DeleteSaleCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteSaleValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var success = await _saleRepository.DeleteAsync(request.Id, cancellationToken);
        if (!success)
            throw new KeyNotFoundException($"Sale with ID {request.Id} not found");

        await _outbox.EnqueueAsync(new SaleDeleted(request.Id), cancellationToken);

        return new DeleteSaleResult { Success = true };
    }
}
