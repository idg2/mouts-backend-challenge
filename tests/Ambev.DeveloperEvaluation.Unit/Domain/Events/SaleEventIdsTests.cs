using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Events;

// Work item: TASK-054 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="SaleEventIds"/> helper.
/// </summary>
public class SaleEventIdsTests
{
    // Work item: TASK-054 (FEAT-017)
    /// <summary>
    /// Tests that the sale id is read from each of the five sale events.
    /// </summary>
    [Fact(DisplayName = "Given each sale event When reading the sale id Then the id is returned")]
    public void Given_EachEvent_When_ReadingSaleId_Then_IdReturned()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var snapshot = new SaleSnapshot(saleId, 1, DateTime.UtcNow, Guid.NewGuid(), "c", Guid.NewGuid(), "b", 0m, false, []);

        // Act / Assert
        SaleEventIds.SaleIdOf(new SaleCreated(snapshot)).Should().Be(saleId);
        SaleEventIds.SaleIdOf(new SaleModified(snapshot)).Should().Be(saleId);
        SaleEventIds.SaleIdOf(new ItemCancelled(saleId, Guid.NewGuid(), Guid.NewGuid())).Should().Be(saleId);
        SaleEventIds.SaleIdOf(new SaleCancelled(saleId)).Should().Be(saleId);
        SaleEventIds.SaleIdOf(new SaleDeleted(saleId)).Should().Be(saleId);
    }

    // Work item: TASK-059 (FEAT-017)
    /// <summary>
    /// Tests that a created or modified event without a snapshot yields no sale id instead of throwing.
    /// </summary>
    [Fact(DisplayName = "Given a created or modified event with a null sale When reading the sale id Then null")]
    public void Given_NullSale_When_ReadingSaleId_Then_Null()
    {
        // Act / Assert
        SaleEventIds.SaleIdOf(new SaleCreated(null!)).Should().BeNull();
        SaleEventIds.SaleIdOf(new SaleModified(null!)).Should().BeNull();
    }

    // Work item: TASK-054 (FEAT-017)
    /// <summary>
    /// Tests that anything other than a sale event yields no sale id.
    /// </summary>
    [Fact(DisplayName = "Given another object or null When reading the sale id Then null")]
    public void Given_Other_When_ReadingSaleId_Then_Null()
    {
        // Act / Assert
        SaleEventIds.SaleIdOf(null).Should().BeNull();
        SaleEventIds.SaleIdOf("text").Should().BeNull();
    }
}
