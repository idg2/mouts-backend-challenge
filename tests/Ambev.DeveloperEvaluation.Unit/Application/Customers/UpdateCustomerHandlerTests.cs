using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
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

    // Work item: TASK-018 (FEAT-010), FEAT-012
    /// <summary>
    /// Tests that the new name and the normalized new document are applied to the stored customer and saved.
    /// </summary>
    [Fact(DisplayName = "Given new customer data When updating customer Then saves the changes")]
    public async Task Given_ValidCommand_When_Handled_Then_SavesChanges()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme Market", Document = "52998224725" };
        var command = new UpdateCustomerCommand { Id = customer.Id, Name = "Acme Supermarket", Document = "11.222.333/0001-81" };
        var expected = new UpdateCustomerResult { Id = customer.Id, Name = command.Name };
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.UpdateAsync(customer, Arg.Any<CancellationToken>()).Returns(customer);
        _mapper.Map<UpdateCustomerResult>(customer).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        customer.Name.Should().Be("Acme Supermarket");
        customer.Document.Should().Be("11222333000181");
        await _customerRepository.Received(1).UpdateAsync(customer, Arg.Any<CancellationToken>());
    }

    // Work item: FEAT-012
    /// <summary>
    /// Tests that another customer's document is rejected and nothing is saved.
    /// </summary>
    [Fact(DisplayName = "Given another customer's document When updating customer Then throws duplicate entry exception")]
    public async Task Given_AnotherCustomersDocument_When_Handled_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme Market", Document = "52998224725" };
        var command = new UpdateCustomerCommand { Id = customer.Id, Name = "Acme Market", Document = "11222333000181" };
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.GetByDocumentAsync("11222333000181", Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = Guid.NewGuid(), Name = "Other", Document = "11222333000181" });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateEntryException>().WithMessage("Customer with document 11222333000181 already exists");
        await _customerRepository.DidNotReceive().UpdateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    // Work item: FEAT-012
    /// <summary>
    /// Tests that keeping the customer's own document, even with a mask, is allowed.
    /// </summary>
    [Fact(DisplayName = "Given the customer's own document When updating customer Then saves the changes")]
    public async Task Given_OwnDocument_When_Handled_Then_SavesChanges()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme Market", Document = "52998224725" };
        var command = new UpdateCustomerCommand { Id = customer.Id, Name = "Acme Supermarket", Document = "529.982.247-25" };
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.GetByDocumentAsync("52998224725", Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.UpdateAsync(customer, Arg.Any<CancellationToken>()).Returns(customer);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _customerRepository.Received(1).UpdateAsync(customer, Arg.Any<CancellationToken>());
    }
}
