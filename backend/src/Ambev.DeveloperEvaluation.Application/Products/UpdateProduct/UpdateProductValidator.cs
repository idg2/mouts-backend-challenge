using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for UpdateProductCommand.
/// </summary>
public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>
    /// Initializes validation rules for UpdateProductCommand: the id is required; the description is required
    /// and has at most 200 characters; the unit price is greater than zero.
    /// </summary>
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Product ID is required");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThan(0m);
    }
}
