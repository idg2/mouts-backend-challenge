using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer;
using AutoMapper;
using FluentAssertions;
using Xunit;
using CreateCustomerProfile = Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer.CreateCustomerProfile;
using GetCustomerProfile = Ambev.DeveloperEvaluation.WebApi.Features.Customers.GetCustomer.GetCustomerProfile;
using ListCustomersProfile = Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers.ListCustomersProfile;
using UpdateCustomerProfile = Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer.UpdateCustomerProfile;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Customers;

// Work item: FEAT-012
/// <summary>
/// Contains unit tests for the WebApi customer profiles: requests carry the document to the commands and results
/// carry it to the responses.
/// </summary>
public class CustomerProfilesTests
{
    private const string Document = "12ABC34501DE35";

    private readonly IMapper _mapper = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<CreateCustomerProfile>();
        cfg.AddProfile<GetCustomerProfile>();
        cfg.AddProfile<ListCustomersProfile>();
        cfg.AddProfile<UpdateCustomerProfile>();
    }).CreateMapper();

    /// <summary>
    /// Tests that the create and update requests carry the document to their commands.
    /// </summary>
    [Fact(DisplayName = "Given create and update requests When mapping to commands Then the document is copied")]
    public void Given_CreateAndUpdateRequests_When_MappingToCommands_Then_DocumentIsCopied()
    {
        // Act
        var create = _mapper.Map<CreateCustomerCommand>(new CreateCustomerRequest { Name = "Acme Market", Document = Document });
        var update = _mapper.Map<UpdateCustomerCommand>(new UpdateCustomerRequest { Id = Guid.NewGuid(), Name = "Acme Market", Document = Document });

        // Assert
        create.Document.Should().Be(Document);
        update.Document.Should().Be(Document);
    }

    /// <summary>
    /// Tests that the create, get, list, and update responses carry the document.
    /// </summary>
    [Fact(DisplayName = "Given each result When mapping to its response Then the document is copied")]
    public void Given_EachResult_When_MappingToResponse_Then_DocumentIsCopied()
    {
        // Act
        string[] documents =
        [
            _mapper.Map<CreateCustomerResponse>(new CreateCustomerResult { Document = Document }).Document,
            _mapper.Map<GetCustomerResponse>(new GetCustomerResult { Document = Document }).Document,
            _mapper.Map<ListCustomersResponse>(new ListCustomersItem { Document = Document }).Document,
            _mapper.Map<UpdateCustomerResponse>(new UpdateCustomerResult { Document = Document }).Document
        ];

        // Assert
        documents.Should().AllBe(Document);
    }
}
