using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.GetBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Handler for processing GetBranchCommand requests.
/// </summary>
public class GetBranchHandler : IRequestHandler<GetBranchCommand, GetBranchResult>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of GetBranchHandler.
    /// </summary>
    /// <param name="branchRepository">The branch repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public GetBranchHandler(IBranchRepository branchRepository, IMapper mapper)
    {
        _branchRepository = branchRepository;
        _mapper = mapper;
    }

    // Work item: TASK-050 (FEAT-017)
    /// <summary>
    /// Handles the GetBranchCommand request.
    /// </summary>
    /// <param name="request">The GetBranch command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The branch details if found</returns>
    public async Task<GetBranchResult> Handle(GetBranchCommand request, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(GetBranchCommand)), ("id", request.Id)]);
        var validator = new GetBranchValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("BRN-GET-02", "Load the branch", [("id", request.Id)]);
        var branch = await _branchRepository.GetByIdAsync(request.Id, cancellationToken);
        StepTrace.Step("BRN-GET-03", "Branch found?", [("id", request.Id), ("found", branch != null)]);
        if (branch == null)
            throw new KeyNotFoundException($"Branch with ID {request.Id} not found");

        return _mapper.Map<GetBranchResult>(branch);
    }
}
