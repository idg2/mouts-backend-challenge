using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Command for updating a sale and replacing its item list; items are matched by id.
/// </summary>
public class UpdateSaleCommand : IRequest<SaleResult>
{
    /// <summary>
    /// Gets or sets the unique identifier of the sale to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer id.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the branch id.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the sale total, stored as received.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the complete list of items the sale must have after the update.
    /// </summary>
    public List<UpdateSaleItemInput> Items { get; set; } = [];
}
