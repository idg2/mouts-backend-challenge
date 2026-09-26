using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Events;

// Work item: TASK-066 (FEAT-001)
/// <summary>
/// Contains unit tests for reading sale event payloads stored in the outbox before the discount policies. The outbox
/// (<c>OutboxWriter</c> and <c>OutboxRelay</c>) serializes with the default <see cref="JsonSerializer"/> options, so
/// the payload uses the property names as declared and is read case-sensitively.
/// </summary>
public class SaleEventPayloadTests
{
    // Work item: TASK-066 (FEAT-001)
    /// <summary>
    /// Tests that a SaleCreated payload whose items lack the discount snapshot reads back with null, an empty policy
    /// id, and a zero ceiling, while the fields it does carry still bind.
    /// </summary>
    [Fact(DisplayName = "Given a payload without the discount fields When deserialized Then the defaults apply")]
    public void Given_PayloadWithoutDiscountFields_When_Deserialized_Then_DefaultsApply()
    {
        // Arrange
        const string payload = """
            {
              "Sale": {
                "SaleId": "7d2f3c1e-0b6a-4f0e-9d8c-1a2b3c4d5e6f",
                "SaleNumber": 42,
                "SaleDate": "2026-09-24T13:45:30Z",
                "CustomerId": "11111111-1111-1111-1111-111111111111",
                "CustomerName": "Acme Market",
                "BranchId": "22222222-2222-2222-2222-222222222222",
                "BranchName": "Downtown",
                "TotalAmount": 39.56,
                "IsCancelled": false,
                "Items": [
                  {
                    "ItemId": "33333333-3333-3333-3333-333333333333",
                    "LineNumber": 1,
                    "ProductId": "44444444-4444-4444-4444-444444444444",
                    "ProductDescription": "Beer 350ml",
                    "UnitPrice": 10.99,
                    "Quantity": 4,
                    "DiscountPercentage": 10,
                    "DiscountAmount": 4.40,
                    "TotalAmount": 39.56,
                    "IsCancelled": false
                  }
                ]
              }
            }
            """;

        // Act
        var created = JsonSerializer.Deserialize<SaleCreated>(payload)!;

        // Assert
        var item = created.Sale.Items.Should().ContainSingle().Subject;
        item.ItemId.Should().Be(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        item.Quantity.Should().Be(4);
        item.DiscountPercentage.Should().Be(10m);
        item.TotalAmount.Should().Be(39.56m);
        item.RequestedDiscountPercentage.Should().BeNull();
        item.DiscountPolicyId.Should().Be(Guid.Empty);
        item.DiscountCeilingPercentage.Should().Be(0m);
    }
}
