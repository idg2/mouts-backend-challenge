using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.Unit.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-062 (FEAT-001)
/// <summary>
/// Contains unit tests for <see cref="Sale.ApplyDiscounts"/>: tiers by product total, requested discounts, the clamp on
/// recalculation, rounding, limits, and cancelled items.
/// </summary>
public class SaleDiscountTests
{
    private static readonly DateTime SaleDate = DiscountPolicyTestData.Start.AddDays(30);
    private readonly Guid _product = Guid.NewGuid();
    private readonly DiscountPolicy _readme = DiscountPolicyTestData.Create();

    /// <summary>
    /// Tests that lines of one product are priced by the tier of their summed quantity.
    /// </summary>
    [Fact(DisplayName = "Given three lines of four units of one product When applying Then every line gets the 20 percent of the total")]
    public void Given_ThreeLinesOfFour_When_Applied_Then_AllGetTwentyPercent()
    {
        // Arrange
        var sale = NewSale();
        for (var line = 0; line < 3; line++)
            AddItem(sale, _product, 4, 10m, null);

        // Act
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        sale.Items.Should().AllSatisfy(item =>
        {
            item.DiscountPolicyId.Should().Be(_readme.Id);
            item.DiscountCeilingPercentage.Should().Be(20m);
            item.DiscountPercentage.Should().Be(20m);
            item.DiscountAmount.Should().Be(8m);
            item.TotalAmount.Should().Be(32m);
        });
        sale.TotalAmount.Should().Be(96m);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that cancelling a line reprices the remaining lines of the product at the lower tier.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled line When applying again Then the remaining lines drop to the lower tier")]
    public void Given_CancelledLine_When_Reapplied_Then_RemainingDropToTenPercent()
    {
        // Arrange
        var sale = NewSale();
        for (var line = 0; line < 3; line++)
            AddItem(sale, _product, 4, 10m, null);
        sale.ApplyDiscounts(Policies(_product));

        // Act
        Persisted.Set(sale.Items[0], nameof(SaleItem.IsCancelled), true);
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        sale.Items.Skip(1).Should().AllSatisfy(item => item.DiscountPercentage.Should().Be(10m));
        sale.TotalAmount.Should().Be(72m);
    }

    /// <summary>
    /// Tests that a requested discount below the ceiling is applied as requested.
    /// </summary>
    [Fact(DisplayName = "Given a requested discount below the ceiling When applying Then the requested one is applied")]
    public void Given_RequestedBelowCeiling_When_Applied_Then_RequestedUsed()
    {
        // Arrange
        var sale = NewSale();
        var item = AddItem(sale, _product, 5, 10m, 5m);

        // Act
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        item.DiscountCeilingPercentage.Should().Be(10m);
        item.DiscountPercentage.Should().Be(5m);
        item.DiscountAmount.Should().Be(2.5m);
        item.TotalAmount.Should().Be(47.5m);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that a requested discount above a ceiling that dropped is clamped to the new ceiling.
    /// </summary>
    [Fact(DisplayName = "Given a requested discount above a ceiling that dropped When applying again Then it is clamped to the ceiling")]
    public void Given_RequestedAboveNewCeiling_When_Reapplied_Then_Clamped()
    {
        // Arrange
        var sale = NewSale();
        var kept = AddItem(sale, _product, 6, 10m, 20m);
        var removed = AddItem(sale, _product, 6, 10m, null);
        sale.ApplyDiscounts(Policies(_product));
        kept.DiscountPercentage.Should().Be(20m);

        // Act
        Persisted.Set(removed, nameof(SaleItem.IsCancelled), true);
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        kept.RequestedDiscountPercentage.Should().Be(20m);
        kept.DiscountCeilingPercentage.Should().Be(10m);
        kept.DiscountPercentage.Should().Be(10m);
    }

    /// <summary>
    /// Tests that each line applies the product percentage to its own unit price.
    /// </summary>
    [Fact(DisplayName = "Given one product at two prices When applying Then each line applies the group percentage to its own price")]
    public void Given_DifferentPrices_When_Applied_Then_DiscountPerLine()
    {
        // Arrange
        var sale = NewSale();
        var cheap = AddItem(sale, _product, 5, 2m, null);
        var expensive = AddItem(sale, _product, 5, 8m, null);

        // Act
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        cheap.DiscountAmount.Should().Be(2m);
        expensive.DiscountAmount.Should().Be(8m);
    }

    /// <summary>
    /// Tests that the discount amount is rounded to cents and the totals add up.
    /// </summary>
    [Fact(DisplayName = "Given a fractional discount When applying Then the amount is rounded to cents")]
    public void Given_FractionalDiscount_When_Applied_Then_RoundedToCents()
    {
        // Arrange
        var sale = NewSale();
        var item = AddItem(sale, _product, 4, 3.33m, null);

        // Act
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        item.DiscountAmount.Should().Be(1.33m);
        item.TotalAmount.Should().Be(11.99m);
        sale.TotalAmount.Should().Be(11.99m);
    }

    /// <summary>
    /// Tests that a discount at the midpoint of a cent rounds away from zero.
    /// </summary>
    [Fact(DisplayName = "Given a discount at the midpoint of a cent When applying Then it rounds away from zero")]
    public void Given_MidpointDiscount_When_Applied_Then_AwayFromZero()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(tiers: [new DiscountTier(1, null, 12.5m)]);
        var sale = NewSale();
        var item = AddItem(sale, _product, 1, 0.20m, null);

        // Act
        sale.ApplyDiscounts(new Dictionary<Guid, DiscountPolicy> { [_product] = policy });

        // Assert
        item.DiscountAmount.Should().Be(0.03m);
    }

    /// <summary>
    /// Tests that a product total at the policy maximum, split across lines, is allowed and priced.
    /// </summary>
    [Fact(DisplayName = "Given twenty units split across lines When applying Then it is allowed")]
    public void Given_TwentySplit_When_Applied_Then_Allowed()
    {
        // Arrange
        var sale = NewSale();
        AddItem(sale, _product, 12, 1m, null);
        AddItem(sale, _product, 8, 1m, null);

        // Act
        var act = () => sale.ApplyDiscounts(Policies(_product));

        // Assert
        act.Should().NotThrow();
        sale.Items.Should().AllSatisfy(item => item.DiscountPercentage.Should().Be(20m));
    }

    /// <summary>
    /// Tests that a product total above the policy maximum, split across lines, is rejected.
    /// </summary>
    [Fact(DisplayName = "Given twenty-one units split across lines When applying Then throws DomainException")]
    public void Given_TwentyOneSplit_When_Applied_Then_Throws()
    {
        // Arrange
        var sale = NewSale();
        AddItem(sale, _product, 12, 1m, null);
        AddItem(sale, _product, 9, 1m, null);

        // Act
        var act = () => sale.ApplyDiscounts(Policies(_product));

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Total of 21 units for product * exceeds maximum of 20");
    }

    // Work item: TASK-066 (FEAT-001)
    /// <summary>
    /// Tests that a product total above <see cref="int.MaxValue"/> is rejected with the limit message, not an overflow.
    /// </summary>
    [Fact(DisplayName = "Given a product total above int.MaxValue When applying Then throws DomainException instead of overflowing")]
    public void Given_TotalAboveIntMaxValue_When_Applied_Then_ThrowsDomainException()
    {
        // Arrange
        var sale = NewSale();
        AddItem(sale, _product, 1_500_000_000, 1m, null);
        AddItem(sale, _product, 1_500_000_000, 1m, null);

        // Act
        var act = () => sale.ApplyDiscounts(Policies(_product));

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Total of 3000000000 units for product * exceeds maximum of 20");
    }

    /// <summary>
    /// Tests that a product without a policy is rejected.
    /// </summary>
    [Fact(DisplayName = "Given a product without policy When applying Then throws DomainException")]
    public void Given_NoPolicy_When_Applied_Then_Throws()
    {
        // Arrange
        var sale = NewSale();
        AddItem(sale, _product, 1, 1m, null);

        // Act
        var act = () => sale.ApplyDiscounts(new Dictionary<Guid, DiscountPolicy>());

        // Assert
        act.Should().Throw<DomainException>().WithMessage("No discount policy in effect for product*");
    }

    /// <summary>
    /// Tests that each product is evaluated on its own total.
    /// </summary>
    [Fact(DisplayName = "Given two products When applying Then each is evaluated on its own total")]
    public void Given_TwoProducts_When_Applied_Then_IndependentTiers()
    {
        // Arrange
        var other = Guid.NewGuid();
        var sale = NewSale();
        var first = AddItem(sale, _product, 10, 1m, null);
        var second = AddItem(sale, other, 3, 1m, null);

        // Act
        sale.ApplyDiscounts(Policies(_product, other));

        // Assert
        first.DiscountPercentage.Should().Be(20m);
        second.DiscountPercentage.Should().Be(0m);
        second.DiscountCeilingPercentage.Should().Be(0m);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that a priced item that is then cancelled keeps its snapshot and is left out of the sale total.
    /// </summary>
    [Fact(DisplayName = "Given a priced item then cancelled When applying again Then it keeps its snapshot and leaves the total")]
    public void Given_CancelledPricedItem_When_Applied_Then_KeepsSnapshotAndIsLeftOutOfTotal()
    {
        // Arrange
        var sale = NewSale();
        var kept = AddItem(sale, _product, 4, 10m, null);
        var cancelled = AddItem(sale, _product, 8, 10m, null);
        sale.ApplyDiscounts(Policies(_product));

        // Act
        Persisted.Set(cancelled, nameof(SaleItem.IsCancelled), true);
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        cancelled.DiscountCeilingPercentage.Should().Be(20m);
        cancelled.DiscountPercentage.Should().Be(20m);
        cancelled.DiscountAmount.Should().Be(16m);
        cancelled.TotalAmount.Should().Be(64m);
        kept.DiscountPercentage.Should().Be(10m);
        sale.TotalAmount.Should().Be(36m);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that an item that was never priced and arrives cancelled gets its policy with no discount.
    /// </summary>
    [Fact(DisplayName = "Given an item sent already cancelled When applying Then it is stored without discount and leaves the total")]
    public void Given_CancelledUnpricedItem_When_Applied_Then_StoredWithoutDiscount()
    {
        // Arrange
        var sale = NewSale();
        AddItem(sale, _product, 4, 10m, null);
        var cancelled = AddItem(sale, _product, 3, 10m, 5m);
        Persisted.Set(cancelled, nameof(SaleItem.IsCancelled), true);

        // Act
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        cancelled.DiscountPolicyId.Should().Be(_readme.Id);
        cancelled.DiscountCeilingPercentage.Should().Be(0m);
        cancelled.DiscountPercentage.Should().Be(0m);
        cancelled.DiscountAmount.Should().Be(0m);
        cancelled.TotalAmount.Should().Be(30m);
        sale.TotalAmount.Should().Be(36m);
    }

    // Work item: TD-039
    /// <summary>
    /// Tests that a sale with every item cancelled totals zero.
    /// </summary>
    [Fact(DisplayName = "Given every item cancelled When applying Then the sale total is zero")]
    public void Given_EveryItemCancelled_When_Applied_Then_TotalIsZero()
    {
        // Arrange
        var sale = NewSale();
        var item = AddItem(sale, _product, 4, 10m, null);
        sale.ApplyDiscounts(Policies(_product));

        // Act
        Persisted.Set(item, nameof(SaleItem.IsCancelled), true);
        sale.ApplyDiscounts(Policies(_product));

        // Assert
        sale.TotalAmount.Should().Be(0m);
        item.TotalAmount.Should().Be(36m);
    }

    // Work item: TD-039
    private static Sale NewSale() => Persisted.New<Sale>(new
    {
        Id = Guid.NewGuid(),
        SaleDate = SaleDate,
        CustomerId = Guid.NewGuid(),
        CustomerName = "Acme Market",
        BranchId = Guid.NewGuid(),
        BranchName = "Downtown"
    });

    // Work item: TD-039
    private static SaleItem AddItem(Sale sale, Guid productId, int quantity, decimal unitPrice, decimal? requested)
    {
        var item = Persisted.New<SaleItem>(new
        {
            Id = Guid.NewGuid(),
            LineNumber = sale.Items.Count + 1,
            ProductId = productId,
            ProductDescription = "Beer 350ml",
            UnitPrice = unitPrice,
            Quantity = quantity,
            RequestedDiscountPercentage = requested
        });
        Persisted.Set(sale, nameof(Sale.Items), sale.Items.Append(item).ToList());
        return item;
    }

    private Dictionary<Guid, DiscountPolicy> Policies(params Guid[] products) =>
        products.ToDictionary(product => product, _ => _readme);
}
