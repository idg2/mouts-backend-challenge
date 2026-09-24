using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.GetBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for GetBranchRequest.
/// </summary>
public class GetBranchRequestValidator : AbstractValidator<GetBranchRequest>
{
    /// <summary>
    /// Initializes validation rules for GetBranchRequest.
    /// </summary>
    public GetBranchRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Branch ID is required");
    }
}
