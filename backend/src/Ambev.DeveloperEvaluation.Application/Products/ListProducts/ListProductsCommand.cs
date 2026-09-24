using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Command for retrieving one page of products ordered by description.
/// </summary>
public class ListProductsCommand : IRequest<ListProductsResult>
{
    /// <summary>
    /// Gets or sets the page number, starting at 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size, from 1 to 100.
    /// </summary>
    public int Size { get; set; } = 10;
}
