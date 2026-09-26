using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Handler for processing CreateBranchCommand requests.
/// </summary>
public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, CreateBranchResult>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of CreateBranchHandler.
    /// </summary>
    /// <param name="branchRepository">The branch repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public CreateBranchHandler(IBranchRepository branchRepository, IMapper mapper)
    {
        _branchRepository = branchRepository;
        _mapper = mapper;
    }

    // Work item: TASK-050 (FEAT-017)
    /// <summary>
    /// Handles the CreateBranchCommand request.
    /// </summary>
    /// <param name="command">The CreateBranch command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created branch</returns>
    public async Task<CreateBranchResult> Handle(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateBranchValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        StepTrace.Step("BRN-CRT-02", "CMN-PIP-10", "Validate the command", [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var branch = _mapper.Map<Branch>(command);
        var createdBranch = await _branchRepository.CreateAsync(branch, cancellationToken);
        StepTrace.Step("BRN-CRT-03", "Insert the branch", [("id", createdBranch.Id)]);
        return _mapper.Map<CreateBranchResult>(createdBranch);
    }
}
