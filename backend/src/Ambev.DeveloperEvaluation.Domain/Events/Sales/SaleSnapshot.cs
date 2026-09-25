using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// The full state of a sale after a write, carried by <see cref="SaleCreated"/> and <see cref="SaleModified"/>
/// so a consumer can apply it any number of times with the same result.
/// </summary>
/// <param name="SaleId">The sale id</param>
/// <param name="SaleNumber">The sequential sale number</param>
/// <param name="SaleDate">The UTC sale date</param>
/// <param name="CustomerId">The customer id</param>
/// <param name="CustomerName">The customer name copied into the sale</param>
/// <param name="BranchId">The branch id</param>
/// <param name="BranchName">The branch name copied into the sale</param>
/// <param name="TotalAmount">The sale total</param>
/// <param name="IsCancelled">Whether the sale is cancelled</param>
/// <param name="Items">The items, ordered by line number</param>
public sealed record SaleSnapshot(
    Guid SaleId,
    long SaleNumber,
    DateTime SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    decimal TotalAmount,
    bool IsCancelled,
    IReadOnlyList<SaleSnapshotItem> Items)
{
    /// <summary>
    /// Takes a snapshot of a saved sale.
    /// </summary>
    /// <param name="sale">The sale, with its database-assigned number and item ids</param>
    /// <returns>The snapshot</returns>
    public static SaleSnapshot From(Sale sale) => new(
        sale.Id,
        sale.SaleNumber,
        sale.SaleDate,
        sale.CustomerId,
        sale.CustomerName,
        sale.BranchId,
        sale.BranchName,
        sale.TotalAmount,
        sale.IsCancelled,
        sale.Items
            .OrderBy(item => item.LineNumber)
            .Select(item => new SaleSnapshotItem(
                item.Id,
                item.LineNumber,
                item.ProductId,
                item.ProductDescription,
                item.UnitPrice,
                item.Quantity,
                item.DiscountPercentage,
                item.DiscountAmount,
                item.TotalAmount,
                item.IsCancelled))
            .ToList());
}
