using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for ListBranchesRequest.
/// </summary>
public class ListBranchesRequestValidator : AbstractValidator<ListBranchesRequest>
{
    /// <summary>
    /// Initializes validation rules for ListBranchesRequest: page at least 1, size from 1 to 100.
    /// </summary>
    public ListBranchesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
