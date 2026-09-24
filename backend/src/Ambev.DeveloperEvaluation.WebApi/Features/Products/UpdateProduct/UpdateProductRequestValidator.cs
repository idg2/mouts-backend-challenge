using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for UpdateProductRequest.
/// </summary>
public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    /// <summary>
    /// Initializes validation rules for UpdateProductRequest: the id is required; the description is required
    /// and has at most 200 characters; the unit price is greater than zero.
    /// </summary>
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Product ID is required");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThan(0m);
    }
}
