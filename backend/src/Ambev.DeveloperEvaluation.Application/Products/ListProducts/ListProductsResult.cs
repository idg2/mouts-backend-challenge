namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Response model for the ListProducts operation: one page of products and the total count.
/// </summary>
public class ListProductsResult
{
    /// <summary>
    /// The products on the requested page.
    /// </summary>
    public List<ListProductsItem> Items { get; set; } = [];

    /// <summary>
    /// The total number of products.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    public int Size { get; set; }
}

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// A product entry in the ListProducts result.
/// </summary>
public class ListProductsItem
{
    /// <summary>
    /// The unique identifier of the product.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
