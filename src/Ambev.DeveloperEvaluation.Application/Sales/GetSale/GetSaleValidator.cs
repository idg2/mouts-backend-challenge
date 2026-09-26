using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Validator for GetSaleCommand.
/// </summary>
public class GetSaleValidator : AbstractValidator<GetSaleCommand>
{
    /// <summary>
    /// Initializes validation rules for GetSaleCommand.
    /// </summary>
    public GetSaleValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Sale ID is required");
    }
}
