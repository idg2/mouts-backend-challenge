using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="CreateCustomerHandler"/> class.
/// </summary>
public class CreateCustomerHandlerTests
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly CreateCustomerHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public CreateCustomerHandlerTests()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new CreateCustomerHandler(_customerRepository, _mapper);
    }

    /// <summary>
    /// Tests that a valid command creates the customer and returns its result.
    /// </summary>
    [Fact(DisplayName = "Given valid customer data When creating customer Then returns the created customer")]
    public async Task Given_ValidCommand_When_Handled_Then_CreatesCustomerAndReturnsResult()
    {
        // Arrange
        var command = new CreateCustomerCommand { Name = "Acme Market" };
        var customer = new Customer { Id = Guid.NewGuid(), Name = command.Name };
        var expected = new CreateCustomerResult { Id = customer.Id, Name = customer.Name };
        _mapper.Map<Customer>(command).Returns(customer);
        _customerRepository.CreateAsync(customer, Arg.Any<CancellationToken>()).Returns(customer);
        _mapper.Map<CreateCustomerResult>(customer).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        await _customerRepository.Received(1).CreateAsync(customer, Arg.Any<CancellationToken>());
    }
}
