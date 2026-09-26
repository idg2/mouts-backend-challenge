using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.Unit.TestData;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-022 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="UpdateSaleHandler"/> class.
/// Tests cover which copied values are kept and which are refreshed from the catalogs, and the repricing from the
/// discount policies at the stored sale date.
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

    // Work item: TASK-064 (FEAT-001)
    private readonly IDiscountPolicyRepository _discountPolicies = Substitute.For<IDiscountPolicyRepository>();

    // Work item: TASK-064 (FEAT-001)
    private readonly DiscountPolicy _readme = DiscountPolicyTestData.Create();

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001)
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
        _discountPolicies.GetApplicableAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DiscountPolicy> { _readme });
        _handler = new UpdateSaleHandler(
            _saleRepository, _customerRepository, _branchRepository, _productRepository, _mapper, _outbox,
            new DiscountPolicyResolver(_discountPolicies));
    }

    // Work item: TASK-062 (FEAT-001), TASK-064 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an item with the same product keeps its copy (and can be cancelled), an item whose product
    /// changed copies the new product, and a new item copies its product; only changed or new products are loaded.
    /// </summary>
    [Fact(DisplayName = "Given item changes When updating sale Then copies are kept or refreshed per item")]
    public async Task Given_ItemChanges_When_Handled_Then_CopiesAreKeptOrRefreshedPerItem()
    {
        // Arrange
        var beerId = Guid.NewGuid();
        var kept = Persisted.New<SaleItem>(new
        {
            Id = Guid.NewGuid(), ProductId = beerId, ProductDescription = "Beer 350ml", UnitPrice = 10m,
            Quantity = 5, DiscountPolicyId = _readme.Id, DiscountPercentage = 10m, DiscountAmount = 5m, TotalAmount = 45m
        });
        var replaced = Persisted.New<SaleItem>(new
        {
            Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductDescription = "Soda 2L", UnitPrice = 5m,
            Quantity = 2, DiscountPolicyId = _readme.Id, TotalAmount = 10m
        });
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
            Items =
            [
                new UpdateSaleItemInput { Id = kept.Id, ProductId = beerId, Quantity = 6, DiscountPercentage = 10m, IsCancelled = true },
                new UpdateSaleItemInput { Id = replaced.Id, ProductId = water.Id, Quantity = 1 },
                new UpdateSaleItemInput { ProductId = juice.Id, Quantity = 1 }
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
        keptItem.Quantity.Should().Be(5);
        keptItem.RequestedDiscountPercentage.Should().BeNull();
        keptItem.DiscountAmount.Should().Be(5m);
        keptItem.TotalAmount.Should().Be(45m);
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

    // Work item: TASK-064 (FEAT-001), TASK-066 (FEAT-001), TD-039
    /// <summary>
    /// Tests that the sale number and date never change, the customer name is kept when the customer id is
    /// unchanged, and the branch name is refreshed when the branch id changes; the policies are resolved for the new
    /// branch at the stored sale date.
    /// </summary>
    [Fact(DisplayName = "Given header changes When updating sale Then keeps number and date and refreshes changed references")]
    public async Task Given_HeaderChanges_When_Handled_Then_KeepsNumberAndDateAndRefreshesChangedReferences()
    {
        // Arrange
        var item = Persisted.New<SaleItem>(new
        {
            Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductDescription = "Beer 350ml", UnitPrice = 10m,
            Quantity = 5, TotalAmount = 50m
        });
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
            IsCancelled = true,
            Items = [new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 5 }]
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        sale.SaleNumber.Should().Be(7);
        sale.SaleDate.Should().Be(saleDate);
        sale.CustomerName.Should().Be("Acme Market");
        sale.BranchId.Should().Be(uptown.Id);
        sale.BranchName.Should().Be("Uptown");
        sale.TotalAmount.Should().Be(45m);
        sale.IsCancelled.Should().BeTrue();
        await _customerRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _productRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await _discountPolicies.Received(1).GetApplicableAsync(
            uptown.Id, Arg.Any<IReadOnlyCollection<Guid>>(), saleDate, Arg.Any<CancellationToken>());
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

    // Work item: TASK-064 (FEAT-001)
    private static UpdateSaleCommand ValidCommand(Sale sale) => new()
    {
        Id = sale.Id,
        CustomerId = sale.CustomerId,
        BranchId = sale.BranchId,
        Items = [new UpdateSaleItemInput { ProductId = Guid.NewGuid(), Quantity = 1 }]
    };

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001), TD-039
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
        var item = Persisted.New<SaleItem>(new { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m });
        var sale = NewSale(item);
        Persisted.Set(sale, nameof(Sale.IsCancelled), saleWasCancelled);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: false, new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 3 });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var modified = _enqueued.Should().ContainSingle().Which.Should().BeOfType<SaleModified>().Subject;
        modified.Sale.SaleId.Should().Be(sale.Id);
        modified.Sale.IsCancelled.Should().BeFalse();
        modified.Sale.Items.Should().ContainSingle().Which.Quantity.Should().Be(3);
    }

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001), TD-039
    /// <summary>
    /// Tests that cancelling an active sale enqueues SaleModified and then SaleCancelled.
    /// </summary>
    [Fact(DisplayName = "Given an active sale cancelled When updating sale Then enqueues SaleModified then SaleCancelled")]
    public async Task Given_ActiveSaleCancelled_When_Handled_Then_EnqueuesModifiedThenCancelled()
    {
        // Arrange
        var item = Persisted.New<SaleItem>(new { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m });
        var sale = NewSale(item);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: true, new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 1 });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _enqueued.Select(e => e.GetType()).Should().Equal(typeof(SaleModified), typeof(SaleCancelled));
        _enqueued[1].Should().Be(new SaleCancelled(sale.Id));
    }

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001), TD-039
    /// <summary>
    /// Tests that ItemCancelled is enqueued only for existing items that were active and are now cancelled, in the
    /// order of the incoming item list; an item already cancelled and a new item sent cancelled enqueue nothing.
    /// </summary>
    [Fact(DisplayName = "Given item flag changes When updating sale Then enqueues ItemCancelled only for active items in incoming order")]
    public async Task Given_ItemFlagChanges_When_Handled_Then_EnqueuesItemCancelledOnlyForActiveItemsInIncomingOrder()
    {
        // Arrange
        var first = Persisted.New<SaleItem>(new { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m });
        var second = Persisted.New<SaleItem>(new { Id = Guid.NewGuid(), LineNumber = 2, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m });
        var alreadyCancelled = Persisted.New<SaleItem>(new
        {
            Id = Guid.NewGuid(), LineNumber = 3, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m, IsCancelled = true
        });
        var sale = NewSale(first, second, alreadyCancelled);
        var water = new Product { Id = Guid.NewGuid(), Description = "Water 500ml", UnitPrice = 3m };
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { water });
        var command = UpdateCommand(sale, isCancelled: false,
            new UpdateSaleItemInput { Id = second.Id, ProductId = second.ProductId, Quantity = 1, IsCancelled = true },
            new UpdateSaleItemInput { Id = first.Id, ProductId = first.ProductId, Quantity = 1, IsCancelled = true },
            new UpdateSaleItemInput { Id = alreadyCancelled.Id, ProductId = alreadyCancelled.ProductId, Quantity = 1, IsCancelled = true },
            new UpdateSaleItemInput { ProductId = water.Id, Quantity = 1, IsCancelled = true });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _enqueued.Should().HaveCount(3);
        _enqueued[0].Should().BeOfType<SaleModified>();
        _enqueued.Skip(1).Should().Equal(
            new ItemCancelled(sale.Id, second.Id, second.ProductId),
            new ItemCancelled(sale.Id, first.Id, first.ProductId));
    }

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an update cancelling an item and the sale together enqueues SaleModified, then ItemCancelled,
    /// then SaleCancelled.
    /// </summary>
    [Fact(DisplayName = "Given sale and item cancelled together When updating sale Then enqueues modified, item, then sale")]
    public async Task Given_SaleAndItemCancelledTogether_When_Handled_Then_EnqueuesModifiedThenItemThenSale()
    {
        // Arrange
        var item = Persisted.New<SaleItem>(new { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m });
        var sale = NewSale(item);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: true,
            new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 1, IsCancelled = true });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _enqueued.Select(e => e.GetType()).Should().Equal(typeof(SaleModified), typeof(ItemCancelled), typeof(SaleCancelled));
    }

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an update of an unknown sale enqueues nothing.
    /// </summary>
    [Fact(DisplayName = "Given an unknown sale When updating sale Then enqueues nothing")]
    public async Task Given_UnknownSale_When_Handled_Then_EnqueuesNothing()
    {
        // Arrange
        var sale = NewSale(Persisted.New<SaleItem>(new { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 1, TotalAmount = 10m }));
        var command = UpdateCommand(sale, isCancelled: false,
            new UpdateSaleItemInput { ProductId = Guid.NewGuid(), Quantity = 1 });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _enqueued.Should().BeEmpty();
    }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Tests that cancelling one of three lines of four units drops the other two to the 10% tier when they ask for no
    /// discount, and that the cancelled line keeps its 20% snapshot out of the total.
    /// </summary>
    [Fact(DisplayName = "Given one of three lines cancelled When updating sale Then the remaining lines drop to ten percent")]
    public async Task Given_LineCancelled_When_Handled_Then_RemainingLinesDropToTenPercent()
    {
        // Arrange
        var (sale, items) = PricedSaleOfThreeLines();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: false,
            new UpdateSaleItemInput { Id = items[0].Id, ProductId = items[0].ProductId, Quantity = 4, IsCancelled = true },
            new UpdateSaleItemInput { Id = items[1].Id, ProductId = items[1].ProductId, Quantity = 4 },
            new UpdateSaleItemInput { Id = items[2].Id, ProductId = items[2].ProductId, Quantity = 4 });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        items[0].DiscountPercentage.Should().Be(20m);
        items[0].TotalAmount.Should().Be(32m);
        items.Skip(1).Should().AllSatisfy(item =>
        {
            item.DiscountCeilingPercentage.Should().Be(10m);
            item.DiscountPercentage.Should().Be(10m);
            item.DiscountAmount.Should().Be(4m);
            item.TotalAmount.Should().Be(36m);
        });
        sale.TotalAmount.Should().Be(72m);
    }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Tests that a remaining line that still asks for the old 20% after the cancellation is rejected, and nothing is saved.
    /// </summary>
    [Fact(DisplayName = "Given a line cancelled and the old ceiling still requested When updating sale Then DiscountAboveAllowed")]
    public async Task Given_LineCancelledAndOldCeilingRequested_When_Handled_Then_DiscountAboveAllowed()
    {
        // Arrange
        var (sale, items) = PricedSaleOfThreeLines();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: false,
            new UpdateSaleItemInput { Id = items[0].Id, ProductId = items[0].ProductId, Quantity = 4, IsCancelled = true },
            new UpdateSaleItemInput { Id = items[1].Id, ProductId = items[1].ProductId, Quantity = 4, DiscountPercentage = 20m },
            new UpdateSaleItemInput { Id = items[2].Id, ProductId = items[2].ProductId, Quantity = 4 });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "Items[1].DiscountPercentage" && error.ErrorCode == SaleDiscountRules.DiscountAboveAllowed);
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _enqueued.Should().BeEmpty();
    }

    // Work item: TASK-064 (FEAT-001), TD-039
    /// <summary>
    /// Tests that an update resolves the policies at the stored sale date and for the branch the command sends.
    /// </summary>
    [Fact(DisplayName = "Given a stored sale When updating sale Then resolves the policies at the stored sale date")]
    public async Task Given_StoredSale_When_Handled_Then_ResolvesAtTheStoredSaleDate()
    {
        // Arrange
        var item = Persisted.New<SaleItem>(new { Id = Guid.NewGuid(), LineNumber = 1, ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10m, DiscountPolicyId = _readme.Id });
        var sale = NewSale(item);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: false, new UpdateSaleItemInput { Id = item.Id, ProductId = item.ProductId, Quantity = 2 });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _discountPolicies.Received(1).GetApplicableAsync(
            sale.BranchId,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(item.ProductId)),
            new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Tests that an existing item sent as cancelled with another product id keeps its stored product: the other
    /// product is neither loaded nor reported, the policies and the rules use the stored product, and ItemCancelled
    /// carries it.
    /// </summary>
    [Fact(DisplayName = "Given an item cancelled with another product id When updating sale Then the stored product is used")]
    public async Task Given_ItemCancelledWithAnotherProductId_When_Handled_Then_StoredProductIsUsed()
    {
        // Arrange
        var (sale, items) = PricedSaleOfThreeLines();
        var storedProductId = items[0].ProductId;
        var unknownProductId = Guid.NewGuid();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var command = UpdateCommand(sale, isCancelled: false,
            new UpdateSaleItemInput { Id = items[0].Id, ProductId = unknownProductId, Quantity = 4, IsCancelled = true },
            new UpdateSaleItemInput { Id = items[1].Id, ProductId = storedProductId, Quantity = 4 },
            new UpdateSaleItemInput { Id = items[2].Id, ProductId = storedProductId, Quantity = 4 });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        items[0].ProductId.Should().Be(storedProductId);
        items[0].IsCancelled.Should().BeTrue();
        await _productRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await _discountPolicies.Received(1).GetApplicableAsync(
            sale.BranchId,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(storedProductId)),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        _enqueued.Should().ContainSingle(e => e is ItemCancelled)
            .Which.Should().Be(new ItemCancelled(sale.Id, items[0].Id, storedProductId));
    }

    // Work item: TASK-064 (FEAT-001), TD-039
    private (Sale Sale, IReadOnlyList<SaleItem> Items) PricedSaleOfThreeLines()
    {
        var productId = Guid.NewGuid();
        var items = Enumerable.Range(1, 3)
            .Select(line => Persisted.New<SaleItem>(new
            {
                Id = Guid.NewGuid(), LineNumber = line, ProductId = productId, ProductDescription = "Beer 350ml", UnitPrice = 10m,
                Quantity = 4, DiscountPolicyId = _readme.Id, DiscountCeilingPercentage = 20m, DiscountPercentage = 20m,
                DiscountAmount = 8m, TotalAmount = 32m
            }))
            .ToList();
        var sale = NewSale(items.ToArray());
        Persisted.Set(sale, nameof(Sale.TotalAmount), 96m);
        return (sale, items);
    }

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001)
    private static UpdateSaleCommand UpdateCommand(Sale sale, bool isCancelled, params UpdateSaleItemInput[] items) => new()
    {
        Id = sale.Id,
        CustomerId = sale.CustomerId,
        BranchId = sale.BranchId,
        IsCancelled = isCancelled,
        Items = items.ToList()
    };

    // Work item: TD-039
    private static Sale NewSale(params SaleItem[] items) => Persisted.New<Sale>(new
    {
        Id = Guid.NewGuid(),
        SaleNumber = 7L,
        SaleDate = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
        CustomerId = Guid.NewGuid(),
        CustomerName = "Acme Market",
        BranchId = Guid.NewGuid(),
        BranchName = "Downtown",
        TotalAmount = 45m,
        Items = items.ToList()
    });
}
