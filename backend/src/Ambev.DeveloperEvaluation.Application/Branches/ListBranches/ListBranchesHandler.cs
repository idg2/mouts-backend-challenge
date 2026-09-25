using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Handler for processing ListBranchesCommand requests.
/// </summary>
public class ListBranchesHandler : IRequestHandler<ListBranchesCommand, ListBranchesResult>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of ListBranchesHandler.
    /// </summary>
    /// <param name="branchRepository">The branch repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public ListBranchesHandler(IBranchRepository branchRepository, IMapper mapper)
    {
        _branchRepository = branchRepository;
        _mapper = mapper;
    }

    // Work item: TASK-025 (FEAT-011), TASK-050 (FEAT-017)
    /// <summary>
    /// Handles the ListBranchesCommand request.
    /// </summary>
    /// <param name="command">The ListBranches command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of branches and the total count</returns>
    public async Task<ListBranchesResult> Handle(ListBranchesCommand command, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(ListBranchesCommand)), ("page", command.Page), ("size", command.Size)]);
        var validator = new ListBranchesValidator();
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
        var (branches, totalCount) = await _branchRepository.ListAsync(query, cancellationToken);
        StepTrace.Step("BRN-LST-03", "Query one page", [("page", query.Page), ("size", query.Size), ("filters", query.Filters.Count), ("order", query.Order.Count), ("count", branches.Count), ("total", totalCount)]);

        return new ListBranchesResult
        {
            Items = _mapper.Map<List<ListBranchesItem>>(branches),
            TotalCount = totalCount,
            Page = command.Page,
            Size = command.Size
        };
    }
}
