using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for UpdateProductCommand.
/// </summary>
public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    // Work item: BUG-009 (FEAT-010), FEAT-013
    /// <summary>
    /// Initializes validation rules for UpdateProductCommand: the id is required; the code is required and has at most
    /// 50 characters; the description is required
    /// and has at most 200 characters; the unit price is greater than zero and fits numeric(18,2).
    /// </summary>
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Product ID is required");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThan(0m).PrecisionScale(18, 2, true);
    }
}
