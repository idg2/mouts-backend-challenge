using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ambev.DeveloperEvaluation.ORM.ReadModel;

// Work item: TASK-074 (FEAT-003)
/// <summary>
/// One item of a <see cref="SaleDocument"/>, under the names of the item response so the filter translation and
/// the profiles need no renaming.
/// </summary>
public sealed class SaleItemDocument
{
    /// <summary>The item id.</summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    /// <summary>The line number within the sale.</summary>
    public int LineNumber { get; set; }

    /// <summary>The product id.</summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid ProductId { get; set; }

    /// <summary>The product description copied into the sale.</summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>The unit price copied into the sale.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>The applied discount percentage.</summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>The discount amount.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>The item total.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Whether the item is cancelled.</summary>
    public bool IsCancelled { get; set; }

    /// <summary>The discount percentage the client asked for, or null.</summary>
    public decimal? RequestedDiscountPercentage { get; set; }

    /// <summary>The discount policy that priced the item.</summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid DiscountPolicyId { get; set; }

    /// <summary>The highest percentage the policy allowed.</summary>
    public decimal DiscountCeilingPercentage { get; set; }
}
