using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Handler for processing ListSalesCommand requests.
/// </summary>
public class ListSalesHandler : IRequestHandler<ListSalesCommand, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of ListSalesHandler.
    /// </summary>
    /// <param name="saleRepository">The sale repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    // Work item: TASK-025 (FEAT-011), TASK-052 (FEAT-017)
    /// <summary>
    /// Handles the ListSalesCommand request.
    /// </summary>
    /// <param name="command">The ListSales command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of sale headers and the total count</returns>
    public async Task<ListSalesResult> Handle(ListSalesCommand command, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(ListSalesCommand)), ("page", command.Page), ("size", command.Size)]);
        var validator = new ListSalesValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var query = new ListQuery
        {
            Page = command.Page,
            Size = command.Size,
            Filters = command.Filters,
            Order = command.Order
        };
        var (sales, totalCount) = await _saleRepository.ListAsync(query, cancellationToken);
        StepTrace.Step("SAL-LST-03", "Query one page of headers", [("page", command.Page), ("size", command.Size), ("filters", command.Filters.Count), ("returned", sales.Count), ("totalCount", totalCount)]);

        return new ListSalesResult
        {
            Items = _mapper.Map<List<ListSalesItem>>(sales),
            TotalCount = totalCount,
            Page = command.Page,
            Size = command.Size
        };
    }
}
