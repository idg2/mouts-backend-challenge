using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Contains unit tests for the Customer entity class.
/// </summary>
public class CustomerTests
{
    // Work item: TASK-014 (FEAT-010), FEAT-012, TD-042
    /// <summary>
    /// Tests that validation passes when the customer data is valid.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for valid customer data")]
    public void Given_ValidCustomer_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var customer = CustomerTestData.GenerateValidCustomer();

        // Act
        var result = customer.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // Work item: FEAT-012, TD-042
    /// <summary>
    /// Tests that a customer without a valid CPF or CNPJ fails validation on its document.
    /// </summary>
    [Theory(DisplayName = "Validation should fail for a missing or invalid document")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("52998224724")]
    [InlineData("11222333000180")]
    public void Given_CustomerWithInvalidDocument_When_Validated_Then_ShouldReturnInvalid(string? document)
    {
        // Arrange
        var customer = CustomerTestData.GenerateValidCustomer();
        customer.Document = document!;

        // Act
        var result = customer.Validate();

        // Assert
        Assert.False(result.IsValid);
    }
}
