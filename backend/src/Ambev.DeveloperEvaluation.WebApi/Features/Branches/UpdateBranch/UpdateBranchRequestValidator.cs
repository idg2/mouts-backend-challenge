using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for UpdateBranchRequest.
/// </summary>
public class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    /// <summary>
    /// Initializes validation rules for UpdateBranchRequest: the id is required; the name is required
    /// and has at most 100 characters.
    /// </summary>
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Branch ID is required");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
