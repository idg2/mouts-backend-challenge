using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Validator for the Branch entity.
/// </summary>
public class BranchValidator : AbstractValidator<Branch>
{
    /// <summary>
    /// Initializes the validation rules for Branch: the name is required and has at most 100 characters.
    /// </summary>
    public BranchValidator()
    {
        RuleFor(branch => branch.Name)
            .NotEmpty()
            .MaximumLength(100).WithMessage("Branch name cannot be longer than 100 characters.");
    }
}
