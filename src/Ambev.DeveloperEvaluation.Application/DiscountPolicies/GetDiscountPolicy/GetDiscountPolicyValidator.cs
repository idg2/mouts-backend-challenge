using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.GetDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Validator for GetDiscountPolicyCommand.
/// </summary>
public class GetDiscountPolicyValidator : AbstractValidator<GetDiscountPolicyCommand>
{
    /// <summary>
    /// Initializes validation rules for GetDiscountPolicyCommand: the id is required.
    /// </summary>
    public GetDiscountPolicyValidator()
    {
        RuleFor(policy => policy.Id).NotEmpty().WithMessage("Discount policy ID is required");
    }
}
