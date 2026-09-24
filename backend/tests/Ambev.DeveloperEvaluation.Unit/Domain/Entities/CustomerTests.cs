using Ambev.DeveloperEvaluation.Domain.Entities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Contains unit tests for the Customer entity class.
/// </summary>
public class CustomerTests
{
    /// <summary>
    /// Tests that validation passes when the customer data is valid.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for valid customer data")]
    public void Given_ValidCustomer_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme Market" };

        // Act
        var result = customer.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
