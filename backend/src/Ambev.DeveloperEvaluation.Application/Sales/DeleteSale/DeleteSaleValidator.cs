using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Validator for DeleteSaleCommand.
/// </summary>
public class DeleteSaleValidator : AbstractValidator<DeleteSaleCommand>
{
    /// <summary>
    /// Initializes validation rules for DeleteSaleCommand.
    /// </summary>
    public DeleteSaleValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Sale ID is required");
    }
}
