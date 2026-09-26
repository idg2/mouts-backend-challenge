using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
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

    // Work item: TASK-018 (FEAT-010), FEAT-012
    /// <summary>
    /// Tests that a valid command creates the customer with its document normalized and returns its result.
    /// </summary>
    [Fact(DisplayName = "Given valid customer data When creating customer Then returns the created customer")]
    public async Task Given_ValidCommand_When_Handled_Then_CreatesCustomerAndReturnsResult()
    {
        // Arrange
        var command = new CreateCustomerCommand { Name = "Acme Market", Document = "12.abc.345/01de-35" };
        var customer = new Customer { Id = Guid.NewGuid(), Name = command.Name, Document = command.Document };
        var expected = new CreateCustomerResult { Id = customer.Id, Name = customer.Name };
        _mapper.Map<Customer>(command).Returns(customer);
        _customerRepository.CreateAsync(customer, Arg.Any<CancellationToken>()).Returns(customer);
        _mapper.Map<CreateCustomerResult>(customer).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        customer.Document.Should().Be("12ABC34501DE35");
        await _customerRepository.Received(1).CreateAsync(customer, Arg.Any<CancellationToken>());
    }

    // Work item: FEAT-012
    /// <summary>
    /// Tests that a document already in use is rejected, whatever its mask, before anything is stored.
    /// </summary>
    [Fact(DisplayName = "Given a document already in use When creating customer Then throws duplicate entry exception")]
    public async Task Given_DocumentAlreadyInUse_When_Handled_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var command = new CreateCustomerCommand { Name = "Acme Market", Document = "529.982.247-25" };
        _customerRepository.GetByDocumentAsync("52998224725", Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = Guid.NewGuid(), Name = "Other", Document = "52998224725" });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateEntryException>().WithMessage("Customer with document 52998224725 already exists");
        await _customerRepository.DidNotReceive().CreateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }
}
