using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Handler for processing ListCustomersCommand requests.
/// </summary>
public class ListCustomersHandler : IRequestHandler<ListCustomersCommand, ListCustomersResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of ListCustomersHandler.
    /// </summary>
    /// <param name="customerRepository">The customer repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public ListCustomersHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    // Work item: TASK-025 (FEAT-011), TASK-049 (FEAT-017)
    /// <summary>
    /// Handles the ListCustomersCommand request.
    /// </summary>
    /// <param name="command">The ListCustomers command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of customers and the total count</returns>
    public async Task<ListCustomersResult> Handle(ListCustomersCommand command, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(ListCustomersCommand)), ("page", command.Page), ("size", command.Size)]);
        var validator = new ListCustomersValidator();
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
        var (customers, totalCount) = await _customerRepository.ListAsync(query, cancellationToken);
        StepTrace.Step("CUS-LST-03", "Query one page", [("page", query.Page), ("size", query.Size), ("filters", query.Filters.Count), ("order", query.Order.Count), ("count", customers.Count), ("total", totalCount)]);

        return new ListCustomersResult
        {
            Items = _mapper.Map<List<ListCustomersItem>>(customers),
            TotalCount = totalCount,
            Page = command.Page,
            Size = command.Size
        };
    }
}
