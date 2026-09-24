using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Handler for processing DeleteBranchCommand requests.
/// </summary>
public class DeleteBranchHandler : IRequestHandler<DeleteBranchCommand, DeleteBranchResult>
{
    private readonly IBranchRepository _branchRepository;

    /// <summary>
    /// Initializes a new instance of DeleteBranchHandler.
    /// </summary>
    /// <param name="branchRepository">The branch repository</param>
    public DeleteBranchHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    /// <summary>
    /// Handles the DeleteBranchCommand request.
    /// </summary>
    /// <param name="request">The DeleteBranch command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the delete operation</returns>
    public async Task<DeleteBranchResult> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteBranchValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var success = await _branchRepository.DeleteAsync(request.Id, cancellationToken);
        if (!success)
            throw new KeyNotFoundException($"Branch with ID {request.Id} not found");

        return new DeleteBranchResult { Success = true };
    }
}
