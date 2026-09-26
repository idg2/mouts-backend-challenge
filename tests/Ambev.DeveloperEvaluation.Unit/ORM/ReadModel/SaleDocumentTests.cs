using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.ORM.ReadModel;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM.ReadModel;

// Work item: TASK-074 (FEAT-003)
/// <summary>
/// Contains unit tests for the <see cref="SaleDocument"/> class.
/// </summary>
public class SaleDocumentTests
{
    /// <summary>
    /// Tests that a snapshot survives the trip through the document unchanged, items in the same order, and that
    /// the version is stored. Equivalence, not equality: the snapshot record compares its item list by reference
    /// (TD-017).
    /// </summary>
    [Fact(DisplayName = "Given a snapshot When converting to a document and back Then the snapshot is equivalent and the version is kept")]
    public void Given_Snapshot_When_RoundTripping_Then_EquivalentAndVersionKept()
    {
        // Arrange
        var snapshot = Snapshot();

        // Act
        var document = SaleDocument.From(snapshot, version: 42);
        var back = document.ToSnapshot();

        // Assert
        document.Id.Should().Be(snapshot.SaleId);
        document.Version.Should().Be(42);
        document.IsDeleted.Should().BeFalse();
        document.Items.Select(item => item.Id).Should().Equal(snapshot.Items.Select(item => item.ItemId));
        back.Should().BeEquivalentTo(snapshot);
    }

    /// <summary>
    /// Tests that the document uses the response names, which the list parser and the filter translation rely on.
    /// </summary>
    [Fact(DisplayName = "Given the document type When reading its property names Then they are the response names")]
    public void Given_DocumentType_When_ReadingPropertyNames_Then_ResponseNames()
    {
        // Act
        var names = typeof(SaleDocument).GetProperties().Select(property => property.Name);
        var itemNames = typeof(SaleItemDocument).GetProperties().Select(property => property.Name);

        // Assert
        names.Should().Contain(new[] { "Id", "SaleNumber", "SaleDate", "CustomerId", "CustomerName", "BranchId", "BranchName", "TotalAmount", "IsCancelled", "Items", "Version", "IsDeleted" });
        itemNames.Should().Contain(new[] { "Id", "LineNumber", "ProductId", "ProductDescription", "UnitPrice", "Quantity", "DiscountPercentage", "DiscountAmount", "TotalAmount", "IsCancelled", "RequestedDiscountPercentage", "DiscountPolicyId", "DiscountCeilingPercentage" });
    }

    private static SaleSnapshot Snapshot() => new(
        Guid.NewGuid(), 7, new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc),
        Guid.NewGuid(), "Acme Market", Guid.NewGuid(), "Downtown", 95.5m, false,
        [
            new SaleSnapshotItem(Guid.NewGuid(), 1, Guid.NewGuid(), "Beer", 10m, 5, 10m, 5m, 45m, false, null, Guid.NewGuid(), 10m),
            new SaleSnapshotItem(Guid.NewGuid(), 2, Guid.NewGuid(), "Soda", 5.05m, 10, 0m, 0m, 50.5m, true, 5m, Guid.NewGuid(), 20m)
        ]);
}
