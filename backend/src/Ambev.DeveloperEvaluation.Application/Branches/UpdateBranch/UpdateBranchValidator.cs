using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for UpdateBranchCommand.
/// </summary>
public class UpdateBranchValidator : AbstractValidator<UpdateBranchCommand>
{
    /// <summary>
    /// Initializes validation rules for UpdateBranchCommand: the id is required; the name is required
    /// and has at most 100 characters.
    /// </summary>
    public UpdateBranchValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Branch ID is required");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
