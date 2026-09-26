using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032
/// <summary>
/// Validator for DisableDiscountPoliciesRequest.
/// </summary>
public class DisableDiscountPoliciesRequestValidator : AbstractValidator<DisableDiscountPoliciesRequest>
{
    /// <summary>
    /// Initializes validation rules for DisableDiscountPoliciesRequest; the same rules as the command validator.
    /// </summary>
    public DisableDiscountPoliciesRequestValidator()
    {
        RuleFor(request => request.Ids)
            .NotEmpty().WithErrorCode("IdsRequired").WithMessage("At least one discount policy id is required.");
        RuleForEach(request => request.Ids)
            .NotEqual(Guid.Empty).WithErrorCode("EmptyId").WithMessage("A discount policy id must not be empty.");
        RuleFor(request => request.Ids)
            .Must(ids => Duplicates(ids).Count == 0)
            .WithErrorCode("DuplicateId")
            .WithMessage(request => $"Discount policy ids appear more than once: {string.Join(", ", Duplicates(request.Ids))}.")
            .When(request => request.Ids is not null);
    }

    private static List<Guid> Duplicates(IEnumerable<Guid> ids) =>
        ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
}
