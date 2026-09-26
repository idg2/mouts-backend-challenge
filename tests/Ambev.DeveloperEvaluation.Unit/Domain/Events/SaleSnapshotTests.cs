using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Unit.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Events;

// Work item: TASK-028 (FEAT-004), TASK-062 (FEAT-001)
/// <summary>
/// Contains unit tests for <see cref="SaleSnapshot.From"/>.
/// </summary>
public class SaleSnapshotTests
{
    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Tests that the snapshot copies every sale and item value and lists the items by line number.
    /// </summary>
    [Fact(DisplayName = "Given a sale When taking a snapshot Then copies every value and orders items by line")]
    public void Given_Sale_When_TakingSnapshot_Then_CopiesEveryValueAndOrdersItemsByLine()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var second = Persisted.New<SaleItem>(new
        {
            Id = Guid.NewGuid(), LineNumber = 2, ProductId = Guid.NewGuid(), ProductDescription = "Soda 2L",
            UnitPrice = 5m, Quantity = 2, TotalAmount = 10m, IsCancelled = true, DiscountPolicyId = policyId
        });
        var first = Persisted.New<SaleItem>(new
        {
            Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), ProductDescription = "Beer 350ml",
            UnitPrice = 10.99m, Quantity = 4, DiscountPercentage = 10m, DiscountAmount = 4.40m, TotalAmount = 39.56m,
            RequestedDiscountPercentage = 10m, DiscountPolicyId = policyId, DiscountCeilingPercentage = 10m
        });
        var sale = Persisted.New<Sale>(new
        {
            Id = Guid.NewGuid(), SaleNumber = 42L, SaleDate = new DateTime(2026, 9, 24, 13, 45, 30, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(), CustomerName = "Acme Market", BranchId = Guid.NewGuid(), BranchName = "Downtown",
            TotalAmount = 49.56m, IsCancelled = true, Items = new List<SaleItem> {second, first}
        });

        // Act
        var snapshot = SaleSnapshot.From(sale);

        // Assert
        snapshot.Should().BeEquivalentTo(new
        {
            SaleId = sale.Id, SaleNumber = 42L, sale.SaleDate, sale.CustomerId, CustomerName = "Acme Market",
            sale.BranchId, BranchName = "Downtown", TotalAmount = 49.56m, IsCancelled = true
        });
        snapshot.Items.Should().Equal(
            new SaleSnapshotItem(first.Id, 1, first.ProductId, "Beer 350ml", 10.99m, 4, 10m, 4.40m, 39.56m, false, 10m, policyId, 10m),
            new SaleSnapshotItem(second.Id, 2, second.ProductId, "Soda 2L", 5m, 2, 0m, 0m, 10m, true, null, policyId, 0m));
    }
}
