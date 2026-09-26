using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.ValueObjects;

// Work item: TASK-060 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="DiscountTier"/> value object and its tier-set check.
/// </summary>
public class DiscountTierTests
{
    [Theory(DisplayName = "Given invalid tier values When constructing Then throws DomainException")]
    [InlineData(0, 9, 10)]
    [InlineData(4, 3, 10)]
    [InlineData(4, 9, 0)]
    [InlineData(4, 9, 100.01)]
    public void Given_InvalidValues_When_Constructed_Then_Throws(int min, int max, double percentage)
    {
        // Act
        var act = () => new DiscountTier(min, max, (decimal)percentage);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given 100 percent and an open upper bound When constructing Then succeeds")]
    public void Given_BoundaryValues_When_Constructed_Then_Succeeds()
    {
        // Act
        var tier = new DiscountTier(1, null, 100m);

        // Assert
        tier.MaxQuantity.Should().BeNull();
        tier.Percentage.Should().Be(100m);
    }

    [Theory(DisplayName = "Given a closed tier When checking a quantity Then only quantities within its bounds match")]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    public void Given_ClosedTier_When_Contains_Then_RespectsBounds(int quantity, bool expected)
    {
        // Act
        var contains = new DiscountTier(4, 9, 10m).Contains(quantity);

        // Assert
        contains.Should().Be(expected);
    }

    [Fact(DisplayName = "Given an open tier When checking quantities Then any quantity from its minimum matches")]
    public void Given_OpenTier_When_Contains_Then_HasNoUpperBound()
    {
        // Arrange
        var tier = new DiscountTier(10, null, 20m);

        // Act & Assert
        tier.Contains(9).Should().BeFalse();
        tier.Contains(10).Should().BeTrue();
        tier.Contains(1000).Should().BeTrue();
    }

    [Fact(DisplayName = "Given the README tiers When checking the set Then there is no violation")]
    public void Given_ReadmeTiers_When_Checked_Then_NoViolation()
    {
        // Act
        var violation = DiscountTier.FindSetViolation([(4, 9), (10, 20)], 20);

        // Assert
        violation.Should().BeNull();
    }

    [Fact(DisplayName = "Given no tiers When checking the set Then there is no violation")]
    public void Given_NoTiers_When_Checked_Then_NoViolation()
    {
        // Act
        var violation = DiscountTier.FindSetViolation([], 20);

        // Assert
        violation.Should().BeNull();
    }

    [Fact(DisplayName = "Given tiers with a gap When checking the set Then there is no violation")]
    public void Given_TiersWithGap_When_Checked_Then_NoViolation()
    {
        // Act
        var violation = DiscountTier.FindSetViolation([(4, 5), (10, 20)], 20);

        // Assert
        violation.Should().BeNull();
    }

    [Theory(DisplayName = "Given an overlapping, unordered, or oversized tier set When checking Then a violation is reported")]
    [InlineData("4-9,9-20")]
    [InlineData("10-20,4-9")]
    [InlineData("4+,10-20")]
    [InlineData("10-25")]
    [InlineData("21+")]
    public void Given_InvalidSet_When_Checked_Then_ReturnsViolation(string ranges)
    {
        // Act
        var violation = DiscountTier.FindSetViolation(Parse(ranges), 20);

        // Assert
        violation.Should().NotBeNull();
    }

    private static IEnumerable<(int Min, int? Max)> Parse(string ranges) =>
        ranges.Split(',').Select(range => range.EndsWith('+')
            ? (int.Parse(range.TrimEnd('+')), (int?)null)
            : (int.Parse(range.Split('-')[0]), (int?)int.Parse(range.Split('-')[1])));
}
