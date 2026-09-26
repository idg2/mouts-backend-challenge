using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Validator for CreateDiscountPolicyCommand.
/// </summary>
public class CreateDiscountPolicyValidator : AbstractValidator<CreateDiscountPolicyCommand>
{
    // Work item: TASK-066 (FEAT-001)
    /// <summary>
    /// Initializes the validation rules for CreateDiscountPolicyCommand.
    /// </summary>
    /// <remarks>
    /// Rules: product and branch null or a non-empty id; ValidFrom required and UTC; ValidTo, when present, UTC and later
    /// than ValidFrom; maximum per product above zero; tiers not null and without null entries; per tier, minimum at
    /// least 1, maximum null or not below the minimum, percentage above 0 and at most 100 with two decimals; the tier set
    /// sorted by minimum, not overlapping, and within the maximum (<see cref="DiscountTier.FindSetViolation"/>). That
    /// ValidFrom is not in the past is checked by the handler, which owns the clock.
    /// </remarks>
    public CreateDiscountPolicyValidator()
    {
        RuleFor(policy => policy.ProductId).NotEqual(Guid.Empty).When(policy => policy.ProductId.HasValue);
        RuleFor(policy => policy.BranchId).NotEqual(Guid.Empty).When(policy => policy.BranchId.HasValue);
        RuleFor(policy => policy.ValidFrom)
            .NotEmpty()
            .Must(BeUtc).WithErrorCode("NotUtc").WithMessage("ValidFrom must be a UTC date and time, ending in Z.");
        RuleFor(policy => policy.ValidTo)
            .Must(validTo => BeUtc(validTo!.Value)).WithErrorCode("NotUtc").WithMessage("ValidTo must be a UTC date and time, ending in Z.")
            .Must((policy, validTo) => validTo > policy.ValidFrom).WithErrorCode("ValidToNotAfterValidFrom").WithMessage("ValidTo must be later than ValidFrom.")
            .When(policy => policy.ValidTo.HasValue);
        RuleFor(policy => policy.MaxQuantityPerProduct).GreaterThan(0);
        RuleFor(policy => policy.Tiers).NotNull();
        RuleForEach(policy => policy.Tiers).NotNull();
        RuleForEach(policy => policy.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(input => input.MinQuantity).GreaterThanOrEqualTo(1);
            tier.RuleFor(input => input.MaxQuantity).GreaterThanOrEqualTo(input => input.MinQuantity).When(input => input.MaxQuantity.HasValue);
            tier.RuleFor(input => input.Percentage).GreaterThan(0m).LessThanOrEqualTo(100m).PrecisionScale(5, 2, true);
        });
        RuleFor(policy => policy.Tiers)
            .Must((policy, _) => TierSetViolation(policy) is null)
            .WithErrorCode("InvalidTierSet")
            .WithMessage(policy => TierSetViolation(policy)!)
            .When(policy => policy.Tiers is not null && policy.Tiers.TrueForAll(tier => tier is not null) && policy.MaxQuantityPerProduct > 0);
    }

    private static string? TierSetViolation(CreateDiscountPolicyCommand policy) =>
        DiscountTier.FindSetViolation(policy.Tiers.Select(tier => (tier.MinQuantity, tier.MaxQuantity)), policy.MaxQuantityPerProduct);

    private static bool BeUtc(DateTime value) => value.Kind == DateTimeKind.Utc;
}
