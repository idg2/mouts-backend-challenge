using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-015 (FEAT-010)
/// <summary>
/// Represents a sale record. Customer and branch are referenced by id with copies of their names
/// (External Identities); discount and total values are stored as received.
/// </summary>
public class Sale : BaseEntity
{
    /// <summary>
    /// Gets or sets the sequential sale number assigned by the database.
    /// </summary>
    public long SaleNumber { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the sale was made.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the customer id (external identity, no foreign key).
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer name copied from the catalog.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch id (external identity, no foreign key).
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch name copied from the catalog.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sale total, stored as received.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the sale items.
    /// </summary>
    public List<SaleItem> Items { get; set; } = [];

    /// <summary>
    /// Validates the sale against the <see cref="SaleValidator"/> rules.
    /// </summary>
    /// <returns>The validation result with any errors found.</returns>
    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    /// <summary>
    /// Replaces the item list with the incoming items, matching them by id.
    /// </summary>
    /// <remarks>
    /// An incoming item with <see cref="Guid.Empty"/> as id is added. An incoming item with an id copies its
    /// values onto the existing item with that id, which must exist. An existing item whose id is not among
    /// the incoming items is removed.
    /// </remarks>
    /// <param name="incoming">The complete list of items the sale must have.</param>
    public void SyncItems(IReadOnlyCollection<SaleItem> incoming)
    {
        var incomingIds = incoming
            .Where(item => item.Id != Guid.Empty)
            .Select(item => item.Id)
            .ToHashSet();

        Items.RemoveAll(existing => !incomingIds.Contains(existing.Id));

        foreach (var item in incoming)
        {
            if (item.Id == Guid.Empty)
            {
                Items.Add(item);
                continue;
            }

            CopyValues(item, Items.Single(existing => existing.Id == item.Id));
        }
    }

    private static void CopyValues(SaleItem source, SaleItem target)
    {
        target.ProductId = source.ProductId;
        target.ProductDescription = source.ProductDescription;
        target.UnitPrice = source.UnitPrice;
        target.Quantity = source.Quantity;
        target.DiscountPercentage = source.DiscountPercentage;
        target.DiscountAmount = source.DiscountAmount;
        target.TotalAmount = source.TotalAmount;
        target.IsCancelled = source.IsCancelled;
    }
}
