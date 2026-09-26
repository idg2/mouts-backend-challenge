using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-077 (FEAT-003)
/// <summary>
/// Contains unit tests for the <see cref="ListSalesProfile"/> class.
/// </summary>
public class ListSalesProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<ListSalesProfile>()).CreateMapper();

    /// <summary>
    /// Tests that a snapshot maps to the list item header with SaleId as Id.
    /// </summary>
    [Fact(DisplayName = "Given a snapshot When mapping to the list item Then the header fields and the id are mapped")]
    public void Given_Snapshot_When_MappingToListItem_Then_HeaderAndIdMapped()
    {
        // Arrange
        var snapshot = new SaleSnapshot(Guid.NewGuid(), 5, new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), "Acme", Guid.NewGuid(), "Downtown", 12.5m, true, []);

        // Act
        var item = _mapper.Map<ListSalesItem>(snapshot);

        // Assert
        item.Id.Should().Be(snapshot.SaleId);
        item.SaleNumber.Should().Be(5);
        item.SaleDate.Should().Be(snapshot.SaleDate);
        item.CustomerId.Should().Be(snapshot.CustomerId);
        item.CustomerName.Should().Be("Acme");
        item.BranchId.Should().Be(snapshot.BranchId);
        item.BranchName.Should().Be("Downtown");
        item.TotalAmount.Should().Be(12.5m);
        item.IsCancelled.Should().BeTrue();
    }
}
