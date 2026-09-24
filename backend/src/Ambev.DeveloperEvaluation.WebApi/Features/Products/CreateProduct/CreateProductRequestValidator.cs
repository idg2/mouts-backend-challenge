using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for CreateProductRequest.
/// </summary>
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    /// <summary>
    /// Initializes validation rules for CreateProductRequest: the description is required and has at most
    /// 200 characters; the unit price is greater than zero.
    /// </summary>
    public CreateProductRequestValidator()
    {
        RuleFor(product => product.Description).NotEmpty().MaximumLength(200);
        RuleFor(product => product.UnitPrice).GreaterThan(0m);
    }
}
