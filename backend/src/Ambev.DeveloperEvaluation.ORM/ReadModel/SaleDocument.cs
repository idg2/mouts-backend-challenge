using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ambev.DeveloperEvaluation.ORM.ReadModel;

// Work item: TASK-074 (FEAT-003)
/// <summary>
/// The sale as stored in the MongoDB read model: the <see cref="SaleSnapshot"/> under the names of the sale
/// response, plus the outbox sequence of the last event applied (<see cref="Version"/>) and the tombstone flag
/// (<see cref="IsDeleted"/>). Guids use the standard representation; decimals are Decimal128 (driver default).
/// </summary>
public sealed class SaleDocument
{
    /// <summary>The sale id, the document key.</summary>
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    /// <summary>The sequential sale number.</summary>
    public long SaleNumber { get; set; }

    /// <summary>The UTC sale date.</summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SaleDate { get; set; }

    /// <summary>The customer id.</summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid CustomerId { get; set; }

    /// <summary>The customer name copied into the sale.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The branch id.</summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid BranchId { get; set; }

    /// <summary>The branch name copied into the sale.</summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>The sale total.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Whether the sale is cancelled.</summary>
    public bool IsCancelled { get; set; }

    /// <summary>The items, in line order.</summary>
    public List<SaleItemDocument> Items { get; set; } = [];

    /// <summary>The outbox sequence of the last event applied; an event with a lower or equal sequence is ignored.</summary>
    public long Version { get; set; }

    /// <summary>True once SaleDeleted was applied; reads skip the document.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Copies a snapshot into a document with the given version.
    /// </summary>
    /// <param name="snapshot">The sale state carried by the event</param>
    /// <param name="version">The outbox sequence of that event</param>
    /// <returns>The document</returns>
    public static SaleDocument From(SaleSnapshot snapshot, long version) => new()
    {
        Id = snapshot.SaleId,
        SaleNumber = snapshot.SaleNumber,
        SaleDate = snapshot.SaleDate,
        CustomerId = snapshot.CustomerId,
        CustomerName = snapshot.CustomerName,
        BranchId = snapshot.BranchId,
        BranchName = snapshot.BranchName,
        TotalAmount = snapshot.TotalAmount,
        IsCancelled = snapshot.IsCancelled,
        Items = snapshot.Items.Select(item => new SaleItemDocument
        {
            Id = item.ItemId,
            LineNumber = item.LineNumber,
            ProductId = item.ProductId,
            ProductDescription = item.ProductDescription,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            DiscountPercentage = item.DiscountPercentage,
            DiscountAmount = item.DiscountAmount,
            TotalAmount = item.TotalAmount,
            IsCancelled = item.IsCancelled,
            RequestedDiscountPercentage = item.RequestedDiscountPercentage,
            DiscountPolicyId = item.DiscountPolicyId,
            DiscountCeilingPercentage = item.DiscountCeilingPercentage
        }).ToList(),
        Version = version,
        IsDeleted = false
    };

    /// <summary>
    /// Copies the document back into a snapshot for the read handlers.
    /// </summary>
    /// <returns>The snapshot</returns>
    public SaleSnapshot ToSnapshot() => new(
        Id, SaleNumber, SaleDate, CustomerId, CustomerName, BranchId, BranchName, TotalAmount, IsCancelled,
        Items.Select(item => new SaleSnapshotItem(
            item.Id, item.LineNumber, item.ProductId, item.ProductDescription, item.UnitPrice, item.Quantity,
            item.DiscountPercentage, item.DiscountAmount, item.TotalAmount, item.IsCancelled,
            item.RequestedDiscountPercentage, item.DiscountPolicyId, item.DiscountCeilingPercentage)).ToList());
}
