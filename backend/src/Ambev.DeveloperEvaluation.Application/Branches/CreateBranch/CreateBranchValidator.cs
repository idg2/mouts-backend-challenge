using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for CreateBranchCommand.
/// </summary>
public class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    /// <summary>
    /// Initializes validation rules for CreateBranchCommand: the name is required and has at most 100 characters.
    /// </summary>
    public CreateBranchValidator()
    {
        RuleFor(branch => branch.Name).NotEmpty().MaximumLength(100);
    }
}
