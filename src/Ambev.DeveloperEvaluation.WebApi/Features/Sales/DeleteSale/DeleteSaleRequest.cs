namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.DeleteSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Request model for deleting a sale.
/// </summary>
public class DeleteSaleRequest
{
    /// <summary>
    /// The unique identifier of the sale to delete.
    /// </summary>
    public Guid Id { get; set; }
}
