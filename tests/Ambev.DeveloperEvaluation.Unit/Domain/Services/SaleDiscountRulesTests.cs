using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

// Work item: TASK-064 (FEAT-001), TD-039
/// <summary>
/// Contains unit tests for <see cref="SaleDiscountRules"/>: the three business codes, the line each one lands on, and
/// their messages (spec section 5).
/// </summary>
public class SaleDiscountRulesTests
{
    private static readonly DateTime SaleDate = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _branch = Guid.NewGuid();
    private readonly Guid _product = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly DiscountPolicy _readme = DiscountPolicyTestData.Create();

    [Fact(DisplayName = "Given lines within the rules When checked Then there is no failure")]
    public void Given_LinesWithinRules_When_Checked_Then_NoFailure()
    {
        // Arrange
        var lines = new[] { Line(_product, 4), Line(_product, 4), Line(_product, 4, 20m) };

        // Act
        var failures = Check(lines, _product);

        // Assert
        failures.Should().BeEmpty();
    }

    [Fact(DisplayName = "Given a product total above the maximum When checked Then QuantityLimitExceeded lands on every line of the product")]
    public void Given_TotalAboveMaximum_When_Checked_Then_QuantityLimitExceededOnEveryLine()
    {
        // Arrange
        var lines = new[] { Line(_product, 12), Line(_other, 1), Line(_product, 9, 50m) };

        // Act
        var failures = Check(lines, _product, _other);

        // Assert
        failures.Select(failure => (failure.PropertyName, failure.ErrorCode)).Should().Equal(
            ("Items[0].Quantity", SaleDiscountRules.QuantityLimitExceeded),
            ("Items[2].Quantity", SaleDiscountRules.QuantityLimitExceeded));
        failures[0].ErrorMessage.Should().Be($"Total of 21 units for product {_product} exceeds maximum of 20");
    }

    // Work item: TASK-066 (FEAT-001)
    [Fact(DisplayName = "Given two lines whose total is above int.MaxValue When checked Then QuantityLimitExceeded lands on both lines without overflow")]
    public void Given_TotalAboveIntMaxValue_When_Checked_Then_QuantityLimitExceededWithoutOverflow()
    {
        // Arrange
        var lines = new[] { Line(_product, 1_500_000_000), Line(_product, 1_500_000_000) };

        // Act
        var act = () => Check(lines, _product);

        // Assert
        var failures = act.Should().NotThrow().Subject;
        failures.Select(failure => (failure.PropertyName, failure.ErrorCode)).Should().Equal(
            ("Items[0].Quantity", SaleDiscountRules.QuantityLimitExceeded),
            ("Items[1].Quantity", SaleDiscountRules.QuantityLimitExceeded));
        failures[0].ErrorMessage.Should().Be($"Total of 3000000000 units for product {_product} exceeds maximum of 20");
    }

    [Fact(DisplayName = "Given one line at the ceiling and one line 0.01 above When checked Then only the line above fails")]
    public void Given_RequestedAtAndAboveCeiling_When_Checked_Then_OnlyAboveFails()
    {
        // Arrange
        var lines = new[] { Line(_product, 5, 10m), Line(_other, 5, 10.01m) };

        // Act
        var failures = Check(lines, _product, _other);

        // Assert
        var failure = failures.Should().ContainSingle().Subject;
        failure.PropertyName.Should().Be("Items[1].DiscountPercentage");
        failure.ErrorCode.Should().Be(SaleDiscountRules.DiscountAboveAllowed);
        failure.ErrorMessage.Should().Be("Requested discount 10.01% exceeds 10% allowed for 5 units");
    }

    [Fact(DisplayName = "Given three units with a discount When checked Then the ceiling is zero and the discount fails")]
    public void Given_ThreeUnitsWithDiscount_When_Checked_Then_CeilingIsZero()
    {
        // Act
        var failures = Check([Line(_product, 3, 1m)], _product);

        // Assert
        failures.Should().ContainSingle().Which.ErrorMessage.Should().Be("Requested discount 1% exceeds 0% allowed for 3 units");
    }

    [Fact(DisplayName = "Given three units with a zero discount When checked Then there is no failure")]
    public void Given_ThreeUnitsWithZeroDiscount_When_Checked_Then_NoFailure()
    {
        // Act
        var failures = Check([Line(_product, 3, 0m)], _product);

        // Assert
        failures.Should().BeEmpty();
    }

    [Fact(DisplayName = "Given a product without policy When checked Then NoDiscountPolicy lands on its lines")]
    public void Given_ProductWithoutPolicy_When_Checked_Then_NoDiscountPolicyOnItsLines()
    {
        // Arrange
        var lines = new[] { Line(_product, 4), Line(_other, 2), Line(_other, 1, isCancelled: true) };

        // Act
        var failures = Check(lines, _product);

        // Assert
        failures.Select(failure => (failure.PropertyName, failure.ErrorCode)).Should().Equal(
            ("Items[1].ProductId", SaleDiscountRules.NoDiscountPolicy),
            ("Items[2].ProductId", SaleDiscountRules.NoDiscountPolicy));
        failures[0].ErrorMessage.Should()
            .Be($"No discount policy in effect for product {_other} at branch {_branch} on 2026-09-25T12:00:00.0000000Z");
    }

    [Fact(DisplayName = "Given cancelled lines When checked Then they count toward no total and their discount is not checked")]
    public void Given_CancelledLines_When_Checked_Then_LeftOutOfTotalsAndCeilingChecks()
    {
        // Arrange
        var lines = new[] { Line(_product, 12, 50m, isCancelled: true), Line(_product, 9) };

        // Act
        var failures = Check(lines, _product);

        // Assert
        failures.Should().BeEmpty();
    }

    private IReadOnlyList<FluentValidation.Results.ValidationFailure> Check(IReadOnlyList<SaleDiscountLine> lines, params Guid[] productsWithPolicy) =>
        SaleDiscountRules.Check(lines, _branch, SaleDate, productsWithPolicy.ToDictionary(product => product, _ => _readme));

    private static SaleDiscountLine Line(Guid productId, int quantity, decimal? requested = null, bool isCancelled = false) =>
        new(productId, quantity, requested, isCancelled);
}
