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
}
