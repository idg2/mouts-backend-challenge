using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for ListProductsCommand.
/// </summary>
public class ListProductsValidator : AbstractValidator<ListProductsCommand>
{
    /// <summary>
    /// Initializes validation rules for ListProductsCommand: page at least 1, size from 1 to 100.
    /// </summary>
    public ListProductsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
