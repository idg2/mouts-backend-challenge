using Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="DeleteCustomerHandler"/> class.
/// </summary>
public class DeleteCustomerHandlerTests
{
    private readonly ICustomerRepository _customerRepository;
    private readonly DeleteCustomerHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public DeleteCustomerHandlerTests()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _handler = new DeleteCustomerHandler(_customerRepository);
    }

    /// <summary>
    /// Tests that deleting an existing customer reports success.
    /// </summary>
    [Fact(DisplayName = "Given an existing customer id When deleting customer Then returns success")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        _customerRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(new DeleteCustomerCommand(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _customerRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
