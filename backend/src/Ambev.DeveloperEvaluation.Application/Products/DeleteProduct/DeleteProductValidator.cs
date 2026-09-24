using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for DeleteProductCommand.
/// </summary>
public class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    /// <summary>
    /// Initializes validation rules for DeleteProductCommand.
    /// </summary>
    public DeleteProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required");
    }
}
