namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

// Work item: TASK-039 (FEAT-006)
/// <summary>
/// The answer to a sale accepted for asynchronous processing.
/// </summary>
public class SaleAcceptedResponse
{
    /// <summary>
    /// Gets or sets the id the sale will be stored under; GET /api/sales/{id} answers 404 until it is stored.
    /// </summary>
    public Guid Id { get; set; }
}
