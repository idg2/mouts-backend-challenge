using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.GetDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Validator for GetDiscountPolicyRequest.
/// </summary>
public class GetDiscountPolicyRequestValidator : AbstractValidator<GetDiscountPolicyRequest>
{
    /// <summary>
    /// Initializes validation rules for GetDiscountPolicyRequest: the id is required.
    /// </summary>
    public GetDiscountPolicyRequestValidator()
    {
        RuleFor(request => request.Id).NotEmpty().WithMessage("Discount policy ID is required");
    }
}
