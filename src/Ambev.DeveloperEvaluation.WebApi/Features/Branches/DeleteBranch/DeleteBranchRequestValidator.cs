using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.DeleteBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for DeleteBranchRequest.
/// </summary>
public class DeleteBranchRequestValidator : AbstractValidator<DeleteBranchRequest>
{
    /// <summary>
    /// Initializes validation rules for DeleteBranchRequest.
    /// </summary>
    public DeleteBranchRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Branch ID is required");
    }
}
