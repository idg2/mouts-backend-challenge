using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="CreateSaleHandler"/> class.
/// Tests cover the copies taken from the catalogs and the amounts stored as received.
/// </summary>
public class CreateSaleHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;
    private readonly CreateSaleHandler _handler;

    private readonly Customer _customer = new() { Id = Guid.NewGuid(), Name = "Acme Market" };
    private readonly Branch _branch = new() { Id = Guid.NewGuid(), Name = "Downtown" };
    private readonly Product _beer = new() { Id = Guid.NewGuid(), Description = "Beer 350ml", UnitPrice = 10m };

    private Sale? _savedSale;

    /// <summary>
    /// Initializes the test dependencies with one customer, one branch, and one product in the catalogs.
    /// </summary>
    public CreateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _customerRepository = Substitute.For<ICustomerRepository>();
        _branchRepository = Substitute.For<IBranchRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        _mapper = Substitute.For<IMapper>();
        _timeProvider = Substitute.For<TimeProvider>();

        _timeProvider.GetUtcNow().Returns(Now);
        _customerRepository.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
        _branchRepository.GetByIdAsync(_branch.Id, Arg.Any<CancellationToken>()).Returns(_branch);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { _beer });
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var sale = call.Arg<Sale>();
                _savedSale = sale;
                return sale;
            });

        _handler = new CreateSaleHandler(
            _saleRepository, _customerRepository, _branchRepository, _productRepository, _mapper, _timeProvider);
    }

    /// <summary>
    /// Tests that the sale copies the customer name, branch name, product description, and unit price,
    /// takes its date from the time provider, and stores discount and total values exactly as received.
    /// </summary>
    [Fact(DisplayName = "Given a valid sale When creating sale Then copies catalog values and keeps received amounts")]
    public async Task Given_ValidCommand_When_Handled_Then_CopiesCatalogValuesAndKeepsReceivedAmounts()
    {
        // Arrange
        var expected = new SaleResult { Id = Guid.NewGuid() };
        _mapper.Map<SaleResult>(Arg.Any<Sale>()).Returns(expected);
        var command = new CreateSaleCommand
        {
            CustomerId = _customer.Id,
            BranchId = _branch.Id,
            TotalAmount = 99m,
            Items =
            [
                new CreateSaleItemInput
                {
                    ProductId = _beer.Id,
                    Quantity = 5,
                    DiscountPercentage = 10m,
                    DiscountAmount = 3m,
                    TotalAmount = 40m
                }
            ]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        var sale = _savedSale!;
        sale.SaleDate.Should().Be(Now.UtcDateTime);
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
        sale.CustomerId.Should().Be(_customer.Id);
        sale.CustomerName.Should().Be("Acme Market");
        sale.BranchId.Should().Be(_branch.Id);
        sale.BranchName.Should().Be("Downtown");
        sale.TotalAmount.Should().Be(99m);
        sale.IsCancelled.Should().BeFalse();
        var item = sale.Items.Should().ContainSingle().Which;
        item.ProductId.Should().Be(_beer.Id);
        item.ProductDescription.Should().Be("Beer 350ml");
        item.UnitPrice.Should().Be(10m);
        item.Quantity.Should().Be(5);
        item.DiscountPercentage.Should().Be(10m);
        item.DiscountAmount.Should().Be(3m);
        item.TotalAmount.Should().Be(40m);
        item.IsCancelled.Should().BeFalse();
    }

    // Work item: TD-010 (FEAT-010)
    /// <summary>
    /// Tests that the lines are numbered from 1 in the order the command lists them.
    /// </summary>
    [Fact(DisplayName = "Given several items When creating sale Then lines are numbered in the command order")]
    public async Task Given_SeveralItems_When_Handled_Then_LinesAreNumberedInCommandOrder()
    {
        // Arrange
        var command = new CreateSaleCommand
        {
            CustomerId = _customer.Id,
            BranchId = _branch.Id,
            TotalAmount = 60m,
            Items =
            [
                new CreateSaleItemInput { ProductId = _beer.Id, Quantity = 3, TotalAmount = 30m },
                new CreateSaleItemInput { ProductId = _beer.Id, Quantity = 1, TotalAmount = 10m },
                new CreateSaleItemInput { ProductId = _beer.Id, Quantity = 2, TotalAmount = 20m }
            ]
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _savedSale!.Items.Select(item => (item.LineNumber, item.Quantity)).Should()
            .Equal((1, 3), (2, 1), (3, 2));
    }

    // Work item: TD-007 (FEAT-010)
    /// <summary>
    /// Tests that an unknown customer, branch, and product are all reported, and nothing is saved.
    /// </summary>
    [Fact(DisplayName = "Given unknown references When creating sale Then reports each one and saves nothing")]
    public async Task Given_UnknownReferences_When_Handled_Then_ReportsEachAndSavesNothing()
    {
        // Arrange
        var command = new CreateSaleCommand
        {
            CustomerId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            TotalAmount = 10m,
            Items = [new CreateSaleItemInput { ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m }]
        };

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Select(e => e.PropertyName).Should()
            .BeEquivalentTo(["CustomerId", "BranchId", "Items[0].ProductId"]);
        await _saleRepository.DidNotReceive().CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that two lines of the same product both copy it and the product is loaded once.
    /// </summary>
    [Fact(DisplayName = "Given the same product on two lines When creating sale Then both lines copy the product")]
    public async Task Given_SameProductOnTwoLines_When_Handled_Then_BothLinesCopyTheProduct()
    {
        // Arrange
        var command = new CreateSaleCommand
        {
            CustomerId = _customer.Id,
            BranchId = _branch.Id,
            TotalAmount = 90m,
            Items =
            [
                new CreateSaleItemInput { ProductId = _beer.Id, Quantity = 5, TotalAmount = 45m },
                new CreateSaleItemInput { ProductId = _beer.Id, Quantity = 5, TotalAmount = 45m }
            ]
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _savedSale!.Items.Should().HaveCount(2)
            .And.OnlyContain(item => item.ProductDescription == "Beer 350ml" && item.UnitPrice == 10m);
        await _productRepository.Received(1).GetByIdsAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 1 && ids.Contains(_beer.Id)),
            Arg.Any<CancellationToken>());
    }
}
