using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010), TD-006, TASK-064 (FEAT-001)
/// <summary>
/// Command for creating a new sale with its items.
/// </summary>
public class CreateSaleCommand : IRequest<SaleResult>, ITransactionalCommand
{
    // Work item: TASK-037 (FEAT-006)
    /// <summary>
    /// Gets or sets the sale id chosen by the caller, or null to let the database assign one. When a sale with this
    /// id is already stored, the handler returns it without writing, so a redelivered message is harmless.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the customer id.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the branch id.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the sale items.
    /// </summary>
    public List<CreateSaleItemInput> Items { get; set; } = [];
}
