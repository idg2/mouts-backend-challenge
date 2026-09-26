using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="ListCustomersHandler"/> class.
/// </summary>
public class ListCustomersHandlerTests
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly ListCustomersHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public ListCustomersHandlerTests()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new ListCustomersHandler(_customerRepository, _mapper);
    }

    // Work item: TASK-025 (FEAT-011)
    /// <summary>
    /// Tests that the page, size, filters, and order reach the repository and the page and total count are returned.
    /// </summary>
    [Fact(DisplayName = "Given a page request with filters and order When listing customers Then passes them and returns the page")]
    public async Task Given_PageRequestWithFiltersAndOrder_When_Handled_Then_PassesThemAndReturnsPage()
    {
        // Arrange
        var filters = new List<FieldFilter> { new("Name", FilterOperator.Like, "acme%") };
        var order = new List<SortField> { new("Name", true) };
        var command = new ListCustomersCommand { Page = 2, Size = 5, Filters = filters, Order = order };
        IReadOnlyList<Customer> customers = new List<Customer> { new() { Id = Guid.NewGuid(), Name = "Acme Market" } };
        var items = new List<ListCustomersItem> { new() { Id = customers[0].Id, Name = customers[0].Name } };
        _customerRepository.ListAsync(
                Arg.Is<ListQuery>(query => query.Page == 2 && query.Size == 5 && query.Filters == filters && query.Order == order),
                Arg.Any<CancellationToken>())
            .Returns((customers, 12));
        _mapper.Map<List<ListCustomersItem>>(customers).Returns(items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Items.Should().BeSameAs(items);
        result.TotalCount.Should().Be(12);
        result.Page.Should().Be(2);
        result.Size.Should().Be(5);
    }
}
