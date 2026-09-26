using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-060 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="DiscountPolicy"/> aggregate: evaluation, invariants, validity, and specificity.
/// </summary>
public class DiscountPolicyTests
{
    private static readonly DateTime Start = DiscountPolicyTestData.Start;

    [Theory(DisplayName = "Given the README policy When evaluating a total Then returns the tier ceiling")]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 20)]
    [InlineData(20, 20)]
    public void Given_ReadmePolicy_When_Evaluated_Then_ReturnsCeiling(int quantity, int expected)
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create();

        // Act
        var decision = policy.Evaluate(quantity);

        // Assert
        decision.Should().Be(new DiscountDecision(true, expected, policy.Id, 20));
    }

    [Fact(DisplayName = "Given a total above the maximum When evaluating Then it is not allowed and the ceiling is zero")]
    public void Given_QuantityAboveMax_When_Evaluated_Then_NotAllowed()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create();

        // Act
        var decision = policy.Evaluate(21);

        // Assert
        decision.Should().Be(new DiscountDecision(false, 0m, policy.Id, 20));
    }

    [Fact(DisplayName = "Given a total in a gap between tiers When evaluating Then the ceiling is zero")]
    public void Given_Gap_When_Evaluated_Then_ZeroCeiling()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(tiers: [new DiscountTier(4, 5, 10m), new DiscountTier(10, 20, 20m)]);

        // Act
        var decision = policy.Evaluate(7);

        // Assert
        decision.IsAllowed.Should().BeTrue();
        decision.CeilingPercentage.Should().Be(0m);
    }

    [Fact(DisplayName = "Given a policy without tiers When evaluating Then it only limits the quantity")]
    public void Given_NoTiers_When_Evaluated_Then_ZeroCeiling()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(tiers: []);

        // Act
        var decision = policy.Evaluate(5);

        // Assert
        decision.Should().Be(new DiscountDecision(true, 0m, policy.Id, 20));
    }

    [Fact(DisplayName = "Given a zero total When evaluating Then throws DomainException")]
    public void Given_ZeroQuantity_When_Evaluated_Then_Throws()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create();

        // Act
        var act = () => policy.Evaluate(0);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given valid input When creating Then sets every field, a new id, and the creation instant")]
    public void Given_ValidInput_When_Created_Then_FieldsSet()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var now = Start.AddHours(-3);

        // Act
        var policy = DiscountPolicy.Create(productId, branchId, Start.AddDays(1), Start.AddDays(10), 15,
            [new DiscountTier(4, null, 5m)], now);

        // Assert
        policy.Id.Should().NotBeEmpty();
        policy.ProductId.Should().Be(productId);
        policy.BranchId.Should().Be(branchId);
        policy.ValidFrom.Should().Be(Start.AddDays(1));
        policy.ValidTo.Should().Be(Start.AddDays(10));
        policy.MaxQuantityPerProduct.Should().Be(15);
        policy.CreatedAt.Should().Be(now);
        policy.Tiers.Should().ContainSingle().Which.Percentage.Should().Be(5m);
    }

    [Fact(DisplayName = "Given a start equal to now When creating Then succeeds")]
    public void Given_StartEqualToNow_When_Created_Then_Succeeds()
    {
        // Act
        var act = () => DiscountPolicy.Create(null, null, Start, null, 20, [], now: Start);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Given a start in the past When creating Then throws DomainException")]
    public void Given_PastStart_When_Created_Then_Throws()
    {
        // Act
        var act = () => DiscountPolicy.Create(null, null, Start, null, 20, [], now: Start.AddTicks(10));

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*past*");
    }

    [Fact(DisplayName = "Given a local start When creating Then throws DomainException")]
    public void Given_LocalTime_When_Created_Then_Throws()
    {
        // Arrange
        var local = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

        // Act
        var act = () => DiscountPolicy.Create(null, null, local, null, 20, [], now: Start);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*UTC*");
    }

    [Fact(DisplayName = "Given ValidTo equal to ValidFrom When creating Then throws DomainException")]
    public void Given_ValidToEqualValidFrom_When_Created_Then_Throws()
    {
        // Act
        var act = () => DiscountPolicy.Create(null, null, Start, Start, 20, [], now: Start);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given a zero maximum When creating Then throws DomainException")]
    public void Given_ZeroMax_When_Created_Then_Throws()
    {
        // Act
        var act = () => DiscountPolicy.Create(null, null, Start, null, 0, [], now: Start);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given overlapping tiers When creating Then throws DomainException")]
    public void Given_OverlappingTiers_When_Created_Then_Throws()
    {
        // Act
        var act = () => DiscountPolicy.Create(null, null, Start, null, 20,
            [new DiscountTier(4, 10, 10m), new DiscountTier(10, 20, 20m)], now: Start);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*overlaps*");
    }

    [Fact(DisplayName = "Given a tier above the policy maximum When creating Then throws DomainException")]
    public void Given_TierAboveMax_When_Created_Then_Throws()
    {
        // Act
        var act = () => DiscountPolicy.Create(null, null, Start, null, 15, [new DiscountTier(10, 20, 20m)], now: Start);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*maximum*");
    }

    [Fact(DisplayName = "Given an empty product id When creating Then throws DomainException")]
    public void Given_EmptyProductId_When_Created_Then_Throws()
    {
        // Act
        var act = () => DiscountPolicy.Create(Guid.Empty, null, Start, null, 20, [], now: Start);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given each scope When ranking Then product and branch > product > branch > default")]
    public void Given_Scopes_When_Ranked_Then_FollowPrecedence()
    {
        // Arrange
        var product = Guid.NewGuid();
        var branch = Guid.NewGuid();

        // Act & Assert
        DiscountPolicyTestData.Create(product, branch).SpecificityRank.Should().Be(3);
        DiscountPolicyTestData.Create(product).SpecificityRank.Should().Be(2);
        DiscountPolicyTestData.Create(branchId: branch).SpecificityRank.Should().Be(1);
        DiscountPolicyTestData.Create().SpecificityRank.Should().Be(0);
    }

    [Fact(DisplayName = "Given a bounded policy When checking instants Then the start is inclusive and the end exclusive")]
    public void Given_BoundedPolicy_When_CheckingEffect_Then_EndIsExclusive()
    {
        // Arrange
        var end = Start.AddDays(10);
        var policy = DiscountPolicyTestData.Create(validTo: end);

        // Act & Assert
        policy.IsInEffectAt(Start.AddTicks(-1)).Should().BeFalse();
        policy.IsInEffectAt(Start).Should().BeTrue();
        policy.IsInEffectAt(end.AddTicks(-1)).Should().BeTrue();
        policy.IsInEffectAt(end).Should().BeFalse();
    }

    [Fact(DisplayName = "Given an open-ended policy When checking a far future instant Then it is in effect")]
    public void Given_OpenPolicy_When_CheckingFarFuture_Then_InEffect()
    {
        // Act
        var inEffect = DiscountPolicyTestData.Create().IsInEffectAt(Start.AddYears(50));

        // Assert
        inEffect.Should().BeTrue();
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given an active policy When disabling Then DisabledAt is the given instant")]
    public void Given_ActivePolicy_When_Disabled_Then_DisabledAtSet()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create();
        var now = Start.AddDays(3);

        // Act
        policy.Disable(now);

        // Assert
        policy.DisabledAt.Should().Be(now);
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given a disabled policy When disabling again Then the first instant is kept")]
    public void Given_DisabledPolicy_When_DisabledAgain_Then_FirstInstantKept()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create();
        var first = Start.AddDays(3);
        policy.Disable(first);

        // Act
        policy.Disable(first.AddDays(1));

        // Assert
        policy.DisabledAt.Should().Be(first);
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given a local instant When disabling Then throws DomainException and the policy stays active")]
    public void Given_LocalInstant_When_Disabled_Then_Throws()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create();
        var local = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

        // Act
        var act = () => policy.Disable(local);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*UTC*");
        policy.DisabledAt.Should().BeNull();
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given a disabled policy When checking an instant inside its validity Then it is not in effect")]
    public void Given_DisabledPolicy_When_CheckingEffect_Then_NotInEffect()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(validTo: Start.AddDays(10));
        policy.Disable(Start.AddDays(1));

        // Act
        var inEffect = policy.IsInEffectAt(Start.AddDays(5));

        // Assert
        inEffect.Should().BeFalse();
    }
}
