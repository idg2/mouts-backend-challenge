using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for ListBranchesCommand.
/// </summary>
public class ListBranchesValidator : AbstractValidator<ListBranchesCommand>
{
    /// <summary>
    /// Initializes validation rules for ListBranchesCommand: page at least 1, size from 1 to 100.
    /// </summary>
    public ListBranchesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
