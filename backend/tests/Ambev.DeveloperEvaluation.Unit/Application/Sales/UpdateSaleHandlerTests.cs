using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="UpdateSaleHandler"/> class.
/// Tests cover which copied values are kept and which are refreshed from the catalogs.
/// </summary>
public class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly UpdateSaleHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public UpdateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _customerRepository = Substitute.For<ICustomerRepository>();
        _branchRepository = Substitute.For<IBranchRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        _mapper = Substitute.For<IMapper>();
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<Sale>());
        _handler = new UpdateSaleHandler(_saleRepository, _customerRepository, _branchRepository, _productRepository, _mapper);
    }

    /// <summary>
    /// Tests that an item with the same product keeps its copy (and can be cancelled), an item whose product
    /// changed copies the new product, and a new item copies its product; only changed or new products are loaded.
    /// </summary>
    [Fact(DisplayName = "Given item changes When updating sale Then copies are kept or refreshed per item")]
    public async Task Given_ItemChanges_When_Handled_Then_CopiesAreKeptOrRefreshedPerItem()
    {
        // Arrange
        var beerId = Guid.NewGuid();
        var kept = new SaleItem
        {
            Id = Guid.NewGuid(), ProductId = beerId, ProductDescription = "Beer 350ml", UnitPrice = 10m,
            Quantity = 5, DiscountPercentage = 10m, DiscountAmount = 5m, TotalAmount = 45m
        };
        var replaced = new SaleItem
        {
            Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductDescription = "Soda 2L", UnitPrice = 5m,
            Quantity = 2, TotalAmount = 10m
        };
        var sale = NewSale(kept, replaced);
        var water = new Product { Id = Guid.NewGuid(), Description = "Water 500ml", UnitPrice = 3m };
        var juice = new Product { Id = Guid.NewGuid(), Description = "Juice 1L", UnitPrice = 4m };
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { water, juice });
        var command = new UpdateSaleCommand
        {
            Id = sale.Id,
            CustomerId = sale.CustomerId,
            BranchId = sale.BranchId,
            TotalAmount = 61m,
            Items =
            [
                new UpdateSaleItemInput
                {
                    Id = kept.Id, ProductId = beerId, Quantity = 6, DiscountPercentage = 10m,
                    DiscountAmount = 6m, TotalAmount = 54m, IsCancelled = true
                },
                new UpdateSaleItemInput { Id = replaced.Id, ProductId = water.Id, Quantity = 1, TotalAmount = 3m },
                new UpdateSaleItemInput { ProductId = juice.Id, Quantity = 1, TotalAmount = 4m }
            ]
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        sale.Items.Should().HaveCount(3);
        var keptItem = sale.Items.Single(item => item.Id == kept.Id);
        keptItem.Should().BeSameAs(kept);
        keptItem.ProductDescription.Should().Be("Beer 350ml");
        keptItem.UnitPrice.Should().Be(10m);
        keptItem.Quantity.Should().Be(6);
        keptItem.DiscountAmount.Should().Be(6m);
        keptItem.TotalAmount.Should().Be(54m);
        keptItem.IsCancelled.Should().BeTrue();
        var replacedItem = sale.Items.Single(item => item.Id == replaced.Id);
        replacedItem.ProductId.Should().Be(water.Id);
        replacedItem.ProductDescription.Should().Be("Water 500ml");
        replacedItem.UnitPrice.Should().Be(3m);
        var newItem = sale.Items.Single(item => item.Id == Guid.Empty);
        newItem.ProductId.Should().Be(juice.Id);
        newItem.ProductDescription.Should().Be("Juice 1L");
        newItem.UnitPrice.Should().Be(4m);
        await _productRepository.Received(1).GetByIdsAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 2 && ids.Contains(water.Id) && ids.Contains(juice.Id)),
            Arg.Any<CancellationToken>());
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that the sale number and date never change, the customer name is kept when the customer id is
    /// unchanged, and the branch name is refreshed when the branch id changes.
    /// </summary>
    [Fact(DisplayName = "Given header changes When updating sale Then keeps number and date and refreshes changed references")]
    public async Task Given_HeaderChanges_When_Handled_Then_KeepsNumberAndDateAndRefreshesChangedReferences()
    {
        // Arrange
        var item = new SaleItem
        {
            Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductDescription = "Beer 350ml", UnitPrice = 10m,
            Quantity = 5, TotalAmount = 50m
        };
        var sale = NewSale(item);
        var saleDate = sale.SaleDate;
        var uptown = new Branch { Id = Guid.NewGuid(), Name = "Uptown" };
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _branchRepository.GetByIdAsync(uptown.Id, Arg.Any<CancellationToken>()).Returns(uptown);
        var command = new UpdateSaleCommand
        {
            Id = sale.Id,
            CustomerId = sale.CustomerId,
            BranchId = uptown.Id,
            TotalAmount = 99m,
            IsCancelled = true,
            Items = [new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 5, TotalAmount = 50m }]
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        sale.SaleNumber.Should().Be(7);
        sale.SaleDate.Should().Be(saleDate);
        sale.CustomerName.Should().Be("Acme Market");
        sale.BranchId.Should().Be(uptown.Id);
        sale.BranchName.Should().Be("Uptown");
        sale.TotalAmount.Should().Be(99m);
        sale.IsCancelled.Should().BeTrue();
        await _customerRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _productRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    private static Sale NewSale(params SaleItem[] items) => new()
    {
        Id = Guid.NewGuid(),
        SaleNumber = 7,
        SaleDate = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
        CustomerId = Guid.NewGuid(),
        CustomerName = "Acme Market",
        BranchId = Guid.NewGuid(),
        BranchName = "Downtown",
        TotalAmount = 45m,
        Items = items.ToList()
    };
}
