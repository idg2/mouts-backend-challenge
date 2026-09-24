using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="UpdateCustomerHandler"/> class.
/// </summary>
public class UpdateCustomerHandlerTests
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly UpdateCustomerHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public UpdateCustomerHandlerTests()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new UpdateCustomerHandler(_customerRepository, _mapper);
    }

    /// <summary>
    /// Tests that the new name is applied to the stored customer and saved.
    /// </summary>
    [Fact(DisplayName = "Given new customer data When updating customer Then saves the changes")]
    public async Task Given_ValidCommand_When_Handled_Then_SavesChanges()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme Market" };
        var command = new UpdateCustomerCommand { Id = customer.Id, Name = "Acme Supermarket" };
        var expected = new UpdateCustomerResult { Id = customer.Id, Name = command.Name };
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.UpdateAsync(customer, Arg.Any<CancellationToken>()).Returns(customer);
        _mapper.Map<UpdateCustomerResult>(customer).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        customer.Name.Should().Be("Acme Supermarket");
        await _customerRepository.Received(1).UpdateAsync(customer, Arg.Any<CancellationToken>());
    }
}
