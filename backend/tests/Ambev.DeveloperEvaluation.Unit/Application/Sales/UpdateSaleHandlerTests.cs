using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
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

    // Work item: TASK-029 (FEAT-004)
    private readonly IOutbox _outbox;

    // Work item: TASK-029 (FEAT-004)
    private readonly List<IIntegrationEvent> _enqueued = [];

    // Work item: TASK-029 (FEAT-004)
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
        _outbox = Substitute.For<IOutbox>();
        _outbox.When(outbox => outbox.EnqueueAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => _enqueued.Add(call.Arg<IIntegrationEvent>()));
        _handler = new UpdateSaleHandler(_saleRepository, _customerRepository, _branchRepository, _productRepository, _mapper, _outbox);
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

    // Work item: TD-007 (FEAT-010)
    /// <summary>
    /// Tests that updating a sale that does not exist raises KeyNotFoundException.
    /// </summary>
    [Fact(DisplayName = "Given an unknown sale id When updating sale Then throws KeyNotFoundException")]
    public async Task Given_UnknownSaleId_When_Handled_Then_ThrowsKeyNotFound()
    {
        // Arrange
        var command = ValidCommand(NewSale());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // Work item: TD-007 (FEAT-010)
    /// <summary>
    /// Tests that an item id from another sale is rejected and nothing is saved.
    /// </summary>
    [Fact(DisplayName = "Given an item id from another sale When updating sale Then reports it and saves nothing")]
    public async Task Given_ItemIdFromAnotherSale_When_Handled_Then_ReportsItAndSavesNothing()
    {
        // Arrange
        var sale = NewSale();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = ValidCommand(sale);
        command.Items[0].Id = Guid.NewGuid();

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Items[0].Id" && e.ErrorCode == "ItemNotInSale");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    // Work item: TD-007 (FEAT-010)
    /// <summary>
    /// Tests that a changed customer, a changed branch, and a new item's product that do not exist are all
    /// reported, and nothing is saved.
    /// </summary>
    [Fact(DisplayName = "Given unknown references When updating sale Then reports each one and saves nothing")]
    public async Task Given_UnknownReferences_When_Handled_Then_ReportsEachAndSavesNothing()
    {
        // Arrange
        var sale = NewSale();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Product>());
        var command = ValidCommand(sale);
        command.CustomerId = Guid.NewGuid();
        command.BranchId = Guid.NewGuid();

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Select(e => e.PropertyName).Should()
            .BeEquivalentTo(["CustomerId", "BranchId", "Items[0].ProductId"]);
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    private static UpdateSaleCommand ValidCommand(Sale sale) => new()
    {
        Id = sale.Id,
        CustomerId = sale.CustomerId,
        BranchId = sale.BranchId,
        TotalAmount = 10m,
        Items = [new UpdateSaleItemInput { ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m }]
    };

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Tests that an update that does not cancel the sale or any item, including one that un-cancels the sale,
    /// enqueues only SaleModified with the sale after the update.
    /// </summary>
    [Theory(DisplayName = "Given no cancellation When updating sale Then enqueues only SaleModified")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Given_NoCancellation_When_Handled_Then_EnqueuesOnlySaleModified(bool saleWasCancelled)
    {
        // Arrange
        var item = new SaleItem { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m };
        var sale = NewSale(item);
        sale.IsCancelled = saleWasCancelled;
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: false, new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 3, TotalAmount = 30m });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var modified = _enqueued.Should().ContainSingle().Which.Should().BeOfType<SaleModified>().Subject;
        modified.Sale.SaleId.Should().Be(sale.Id);
        modified.Sale.IsCancelled.Should().BeFalse();
        modified.Sale.Items.Should().ContainSingle().Which.Quantity.Should().Be(3);
    }

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Tests that cancelling an active sale enqueues SaleModified and then SaleCancelled.
    /// </summary>
    [Fact(DisplayName = "Given an active sale cancelled When updating sale Then enqueues SaleModified then SaleCancelled")]
    public async Task Given_ActiveSaleCancelled_When_Handled_Then_EnqueuesModifiedThenCancelled()
    {
        // Arrange
        var item = new SaleItem { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m };
        var sale = NewSale(item);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: true, new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 1, TotalAmount = 10m });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _enqueued.Select(e => e.GetType()).Should().Equal(typeof(SaleModified), typeof(SaleCancelled));
        _enqueued[1].Should().Be(new SaleCancelled(sale.Id));
    }

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Tests that ItemCancelled is enqueued only for existing items that were active and are now cancelled, in the
    /// order of the incoming item list; an item already cancelled and a new item sent cancelled enqueue nothing.
    /// </summary>
    [Fact(DisplayName = "Given item flag changes When updating sale Then enqueues ItemCancelled only for active items in incoming order")]
    public async Task Given_ItemFlagChanges_When_Handled_Then_EnqueuesItemCancelledOnlyForActiveItemsInIncomingOrder()
    {
        // Arrange
        var first = new SaleItem { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m };
        var second = new SaleItem { Id = Guid.NewGuid(), LineNumber = 2, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m };
        var alreadyCancelled = new SaleItem
        {
            Id = Guid.NewGuid(), LineNumber = 3, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m, IsCancelled = true
        };
        var sale = NewSale(first, second, alreadyCancelled);
        var water = new Product { Id = Guid.NewGuid(), Description = "Water 500ml", UnitPrice = 3m };
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { water });
        var command = UpdateCommand(sale, isCancelled: false,
            new UpdateSaleItemInput { Id = second.Id, ProductId = second.ProductId, Quantity = 1, TotalAmount = 10m, IsCancelled = true },
            new UpdateSaleItemInput { Id = first.Id, ProductId = first.ProductId, Quantity = 1, TotalAmount = 10m, IsCancelled = true },
            new UpdateSaleItemInput { Id = alreadyCancelled.Id, ProductId = alreadyCancelled.ProductId, Quantity = 1, TotalAmount = 10m, IsCancelled = true },
            new UpdateSaleItemInput { ProductId = water.Id, Quantity = 1, TotalAmount = 3m, IsCancelled = true });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _enqueued.Should().HaveCount(3);
        _enqueued[0].Should().BeOfType<SaleModified>();
        _enqueued.Skip(1).Should().Equal(
            new ItemCancelled(sale.Id, second.Id, second.ProductId),
            new ItemCancelled(sale.Id, first.Id, first.ProductId));
    }

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Tests that an update cancelling an item and the sale together enqueues SaleModified, then ItemCancelled,
    /// then SaleCancelled.
    /// </summary>
    [Fact(DisplayName = "Given sale and item cancelled together When updating sale Then enqueues modified, item, then sale")]
    public async Task Given_SaleAndItemCancelledTogether_When_Handled_Then_EnqueuesModifiedThenItemThenSale()
    {
        // Arrange
        var item = new SaleItem { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m };
        var sale = NewSale(item);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: true,
            new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 1, TotalAmount = 10m, IsCancelled = true });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _enqueued.Select(e => e.GetType()).Should().Equal(typeof(SaleModified), typeof(ItemCancelled), typeof(SaleCancelled));
    }

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Tests that an update of an unknown sale enqueues nothing.
    /// </summary>
    [Fact(DisplayName = "Given an unknown sale When updating sale Then enqueues nothing")]
    public async Task Given_UnknownSale_When_Handled_Then_EnqueuesNothing()
    {
        // Arrange
        var sale = NewSale(new SaleItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m });
        var command = UpdateCommand(sale, isCancelled: false,
            new UpdateSaleItemInput { ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _enqueued.Should().BeEmpty();
    }

    // Work item: TASK-029 (FEAT-004)
    private static UpdateSaleCommand UpdateCommand(Sale sale, bool isCancelled, params UpdateSaleItemInput[] items) => new()
    {
        Id = sale.Id,
        CustomerId = sale.CustomerId,
        BranchId = sale.BranchId,
        TotalAmount = items.Sum(item => item.TotalAmount),
        IsCancelled = isCancelled,
        Items = items.ToList()
    };

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
