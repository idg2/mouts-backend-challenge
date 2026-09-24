using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for CreateProductRequest.
/// </summary>
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    // Work item: BUG-009 (FEAT-010), FEAT-013
    /// <summary>
    /// Initializes validation rules for CreateProductRequest: the code is required and has at most
    /// 50 characters; the description is required and has at most
    /// 200 characters; the unit price is greater than zero and fits numeric(18,2).
    /// </summary>
    public CreateProductRequestValidator()
    {
        RuleFor(product => product.Code).NotEmpty().MaximumLength(50);
        RuleFor(product => product.Description).NotEmpty().MaximumLength(200);
        RuleFor(product => product.UnitPrice).GreaterThan(0m).PrecisionScale(18, 2, true);
    }
}
