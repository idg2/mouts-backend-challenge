using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Events;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// Contains unit tests for <see cref="SaleSnapshot.From"/>.
/// </summary>
public class SaleSnapshotTests
{
    /// <summary>
    /// Tests that the snapshot copies every sale and item value and lists the items by line number.
    /// </summary>
    [Fact(DisplayName = "Given a sale When taking a snapshot Then copies every value and orders items by line")]
    public void Given_Sale_When_TakingSnapshot_Then_CopiesEveryValueAndOrdersItemsByLine()
    {
        // Arrange
        var second = new SaleItem
        {
            Id = Guid.NewGuid(), LineNumber = 2, ProductId = Guid.NewGuid(), ProductDescription = "Soda 2L",
            UnitPrice = 5m, Quantity = 2, TotalAmount = 10m, IsCancelled = true
        };
        var first = new SaleItem
        {
            Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), ProductDescription = "Beer 350ml",
            UnitPrice = 10.99m, Quantity = 4, DiscountPercentage = 10m, DiscountAmount = 4.40m, TotalAmount = 39.56m
        };
        var sale = new Sale
        {
            Id = Guid.NewGuid(), SaleNumber = 42, SaleDate = new DateTime(2026, 9, 24, 13, 45, 30, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(), CustomerName = "Acme Market", BranchId = Guid.NewGuid(), BranchName = "Downtown",
            TotalAmount = 49.56m, IsCancelled = true, Items = [second, first]
        };

        // Act
        var snapshot = SaleSnapshot.From(sale);

        // Assert
        snapshot.Should().BeEquivalentTo(new
        {
            SaleId = sale.Id, SaleNumber = 42L, sale.SaleDate, sale.CustomerId, CustomerName = "Acme Market",
            sale.BranchId, BranchName = "Downtown", TotalAmount = 49.56m, IsCancelled = true
        });
        snapshot.Items.Should().Equal(
            new SaleSnapshotItem(first.Id, 1, first.ProductId, "Beer 350ml", 10.99m, 4, 10m, 4.40m, 39.56m, false),
            new SaleSnapshotItem(second.Id, 2, second.ProductId, "Soda 2L", 5m, 2, 0m, 0m, 10m, true));
    }
}
