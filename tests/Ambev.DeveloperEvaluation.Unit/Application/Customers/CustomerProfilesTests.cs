using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

// Work item: FEAT-012
/// <summary>
/// Contains unit tests for the Application customer profiles: every result carries the customer's document.
/// </summary>
public class CustomerProfilesTests
{
    private static readonly Customer Customer = new() { Id = Guid.NewGuid(), Name = "Acme Market", Document = "12ABC34501DE35" };

    private readonly IMapper _mapper = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<CreateCustomerProfile>();
        cfg.AddProfile<GetCustomerProfile>();
        cfg.AddProfile<ListCustomersProfile>();
        cfg.AddProfile<UpdateCustomerProfile>();
    }).CreateMapper();

    /// <summary>
    /// Tests that the create command's document reaches the entity.
    /// </summary>
    [Fact(DisplayName = "Given a create command When mapping to the entity Then the document is copied")]
    public void Given_CreateCommand_When_MappingToEntity_Then_DocumentIsCopied()
    {
        // Act
        var customer = _mapper.Map<Customer>(new CreateCustomerCommand { Name = "Acme Market", Document = "12ABC34501DE35" });

        // Assert
        customer.Document.Should().Be("12ABC34501DE35");
    }

    /// <summary>
    /// Tests that the create, get, list, and update results carry the document.
    /// </summary>
    [Fact(DisplayName = "Given a customer When mapping to each result Then the document is copied")]
    public void Given_Customer_When_MappingToEachResult_Then_DocumentIsCopied()
    {
        // Act
        string[] documents =
        [
            _mapper.Map<CreateCustomerResult>(Customer).Document,
            _mapper.Map<GetCustomerResult>(Customer).Document,
            _mapper.Map<ListCustomersItem>(Customer).Document,
            _mapper.Map<UpdateCustomerResult>(Customer).Document
        ];

        // Assert
        documents.Should().AllBe("12ABC34501DE35");
    }
}
