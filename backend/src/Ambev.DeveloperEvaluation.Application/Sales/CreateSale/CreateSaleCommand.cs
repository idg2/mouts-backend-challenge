using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Command for creating a new sale with its items.
/// </summary>
public class CreateSaleCommand : IRequest<SaleResult>
{
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
    /// Gets or sets the sale items.
    /// </summary>
    public List<CreateSaleItemInput> Items { get; set; } = [];
}
