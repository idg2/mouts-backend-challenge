using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Validator for CreateBranchRequest.
/// </summary>
public class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    /// <summary>
    /// Initializes validation rules for CreateBranchRequest: the name is required and has at most 100 characters.
    /// </summary>
    public CreateBranchRequestValidator()
    {
        RuleFor(branch => branch.Name).NotEmpty().MaximumLength(100);
    }
}
