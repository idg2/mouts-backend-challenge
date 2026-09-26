using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for ListProductsRequest.
/// </summary>
public class ListProductsRequestValidator : AbstractValidator<ListProductsRequest>
{
    /// <summary>
    /// Initializes validation rules for ListProductsRequest: page at least 1, size from 1 to 100.
    /// </summary>
    public ListProductsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
