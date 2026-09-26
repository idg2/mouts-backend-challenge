using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032
/// <summary>
/// Validator for DisableDiscountPoliciesCommand.
/// </summary>
public class DisableDiscountPoliciesValidator : AbstractValidator<DisableDiscountPoliciesCommand>
{
    /// <summary>
    /// Initializes validation rules for DisableDiscountPoliciesCommand: at least one id (IdsRequired), no empty id
    /// (EmptyId), and no id twice (DuplicateId).
    /// </summary>
    public DisableDiscountPoliciesValidator()
    {
        RuleFor(command => command.Ids)
            .NotEmpty().WithErrorCode("IdsRequired").WithMessage("At least one discount policy id is required.");
        RuleForEach(command => command.Ids)
            .NotEqual(Guid.Empty).WithErrorCode("EmptyId").WithMessage("A discount policy id must not be empty.");
        RuleFor(command => command.Ids)
            .Must(ids => Duplicates(ids).Count == 0)
            .WithErrorCode("DuplicateId")
            .WithMessage(command => $"Discount policy ids appear more than once: {string.Join(", ", Duplicates(command.Ids))}.")
            .When(command => command.Ids is not null);
    }

    private static List<Guid> Duplicates(IEnumerable<Guid> ids) =>
        ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
}
