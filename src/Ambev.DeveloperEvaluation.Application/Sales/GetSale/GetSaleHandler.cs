using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Handler for processing GetSaleCommand requests.
/// </summary>
public class GetSaleHandler : IRequestHandler<GetSaleCommand, SaleResult>
{
    private readonly ISaleReadStore _saleReadStore;
    private readonly IMapper _mapper;

    // Work item: TASK-077 (FEAT-003)
    /// <summary>
    /// Initializes a new instance of GetSaleHandler.
    /// </summary>
    /// <param name="saleReadStore">The sale read model</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public GetSaleHandler(ISaleReadStore saleReadStore, IMapper mapper)
    {
        _saleReadStore = saleReadStore;
        _mapper = mapper;
    }

    // Work item: TASK-052 (FEAT-017), TASK-077 (FEAT-003)
    /// <summary>
    /// Handles the GetSaleCommand request.
    /// </summary>
    /// <param name="request">The GetSale command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sale with its items if found</returns>
    public async Task<SaleResult> Handle(GetSaleCommand request, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(GetSaleCommand)), ("saleId", request.Id)]);
        var validator = new GetSaleValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("SAL-GET-02", "Load the sale from the read model", [("saleId", request.Id)]);
        var sale = await _saleReadStore.GetAsync(request.Id, cancellationToken);
        StepTrace.Step("SAL-GET-03", "Sale found?", [("saleId", request.Id), ("found", sale != null), ("items", sale?.Items.Count)]);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {request.Id} not found");

        return _mapper.Map<SaleResult>(sale);
    }
}
