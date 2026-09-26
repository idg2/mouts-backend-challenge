using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Handler for processing ListDiscountPoliciesCommand requests.
/// </summary>
public class ListDiscountPoliciesHandler : IRequestHandler<ListDiscountPoliciesCommand, ListDiscountPoliciesResult>
{
    private readonly IDiscountPolicyRepository _repository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of ListDiscountPoliciesHandler.
    /// </summary>
    /// <param name="repository">The discount policy repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public ListDiscountPoliciesHandler(IDiscountPolicyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    // Work item: TASK-063 (FEAT-001), TD-032
    /// <summary>
    /// Handles the ListDiscountPoliciesCommand request.
    /// </summary>
    /// <param name="command">The ListDiscountPolicies command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of policies and the total count</returns>
    public async Task<ListDiscountPoliciesResult> Handle(ListDiscountPoliciesCommand command, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(ListDiscountPoliciesCommand)), ("page", command.Page), ("size", command.Size)]);
        var validator = new ListDiscountPoliciesValidator();
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
        var (policies, totalCount) = await _repository.ListAsync(query, command.IncludeDisabled, cancellationToken);
        StepTrace.Step("DSC-LST-03", "Query one page",
            [("page", query.Page), ("size", query.Size), ("filters", query.Filters.Count), ("order", query.Order.Count), ("includeDisabled", command.IncludeDisabled),
             ("count", policies.Count), ("total", totalCount)]);

        return new ListDiscountPoliciesResult
        {
            Items = _mapper.Map<List<DiscountPolicyResult>>(policies),
            TotalCount = totalCount,
            Page = command.Page,
            Size = command.Size
        };
    }
}
