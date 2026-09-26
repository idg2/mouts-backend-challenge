using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TD-010 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="SaleProfile"/> class.
/// </summary>
public class SaleProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<SaleProfile>()).CreateMapper();

    /// <summary>
    /// Tests that the result lists the items by line number, the order the client sent them in, so the create,
    /// update, and get responses agree.
    /// </summary>
    [Fact(DisplayName = "Given items out of line order When mapping a sale Then result items are ordered by line number")]
    public void Given_ItemsOutOfLineOrder_When_MappingSale_Then_ResultItemsAreOrderedByLineNumber()
    {
        // Arrange
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            Items =
            [
                new SaleItem { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), LineNumber = 3, Quantity = 30 },
                new SaleItem { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), LineNumber = 1, Quantity = 10 },
                new SaleItem { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), LineNumber = 2, Quantity = 20 }
            ]
        };

        // Act
        var result = _mapper.Map<SaleResult>(sale);

        // Assert
        result.Items.Select(i => i.Quantity).Should().Equal(10, 20, 30);
    }

    // Work item: TASK-064 (FEAT-001), TASK-066 (FEAT-001)
    /// <summary>
    /// Tests that the item result carries the requested discount and the discount snapshot: policy, ceiling, and
    /// applied percentage.
    /// </summary>
    [Fact(DisplayName = "Given a priced item When mapping a sale Then the result carries the discount snapshot")]
    public void Given_PricedItem_When_MappingSale_Then_ResultCarriesDiscountSnapshot()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            Items =
            [
                new SaleItem
                {
                    Id = Guid.NewGuid(), LineNumber = 1, Quantity = 5, UnitPrice = 10m, DiscountPolicyId = policyId,
                    DiscountCeilingPercentage = 10m, DiscountPercentage = 5m, DiscountAmount = 2.5m, TotalAmount = 47.5m,
                    RequestedDiscountPercentage = 7.5m
                }
            ]
        };

        // Act
        var item = _mapper.Map<SaleResult>(sale).Items.Single();

        // Assert
        item.RequestedDiscountPercentage.Should().Be(7.5m);
        item.DiscountPolicyId.Should().Be(policyId);
        item.DiscountCeilingPercentage.Should().Be(10m);
        item.DiscountPercentage.Should().Be(5m);
        item.DiscountAmount.Should().Be(2.5m);
        item.TotalAmount.Should().Be(47.5m);
    }
}
