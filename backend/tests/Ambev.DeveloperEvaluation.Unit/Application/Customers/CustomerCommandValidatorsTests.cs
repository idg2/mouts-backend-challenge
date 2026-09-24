using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

// Work item: FEAT-012
/// <summary>
/// Contains unit tests for the document rules of <see cref="CreateCustomerValidator"/> and <see cref="UpdateCustomerValidator"/>.
/// </summary>
public class CustomerCommandValidatorsTests
{
    /// <summary>
    /// Tests that CPFs and CNPJs pass with or without a mask, including the alphanumeric CNPJ in lower case.
    /// </summary>
    [Theory(DisplayName = "Given a valid document with or without mask When validating create and update Then both are valid")]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    [InlineData("11.222.333/0001-81")]
    [InlineData("12.abc.345/01de-35")]
    public void Given_ValidDocument_When_ValidatingCreateAndUpdate_Then_BothAreValid(string document)
    {
        // Act
        var create = new CreateCustomerValidator().Validate(new CreateCustomerCommand { Name = "Acme Market", Document = document });
        var update = new UpdateCustomerValidator().Validate(new UpdateCustomerCommand { Id = Guid.NewGuid(), Name = "Acme Market", Document = document });

        // Assert
        create.IsValid.Should().BeTrue();
        update.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a missing document, wrong check digits, a wrong length, and characters outside the mask fail on Document.
    /// </summary>
    [Theory(DisplayName = "Given an invalid document When validating create and update Then both fail on the document")]
    [InlineData("")]
    [InlineData("529.982.247-24")]
    [InlineData("1234567890")]
    [InlineData("529_982_247_25")]
    public void Given_InvalidDocument_When_ValidatingCreateAndUpdate_Then_BothFailOnDocument(string document)
    {
        // Act
        var create = new CreateCustomerValidator().Validate(new CreateCustomerCommand { Name = "Acme Market", Document = document });
        var update = new UpdateCustomerValidator().Validate(new UpdateCustomerCommand { Id = Guid.NewGuid(), Name = "Acme Market", Document = document });

        // Assert
        create.Errors.Select(error => error.PropertyName).Should().Contain("Document");
        update.Errors.Select(error => error.PropertyName).Should().Contain("Document");
    }
}
