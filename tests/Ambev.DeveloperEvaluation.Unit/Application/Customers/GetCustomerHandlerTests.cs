using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="GetCustomerHandler"/> class.
/// </summary>
public class GetCustomerHandlerTests
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly GetCustomerHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public GetCustomerHandlerTests()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetCustomerHandler(_customerRepository, _mapper);
    }

    /// <summary>
    /// Tests that an existing customer is returned.
    /// </summary>
    [Fact(DisplayName = "Given an existing customer id When getting customer Then returns the customer")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsCustomer()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme Market" };
        var expected = new GetCustomerResult { Id = customer.Id, Name = customer.Name };
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _mapper.Map<GetCustomerResult>(customer).Returns(expected);

        // Act
        var result = await _handler.Handle(new GetCustomerCommand(customer.Id), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }
}
