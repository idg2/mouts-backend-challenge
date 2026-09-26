using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.DiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="CreateDiscountPolicyHandler"/> class. Every rejection must be a
/// ValidationException (400), never a DomainException (500).
/// </summary>
public class CreateDiscountPolicyHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
    private readonly IDiscountPolicyRepository _repository = Substitute.For<IDiscountPolicyRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly CreateDiscountPolicyHandler _handler;

    /// <summary>
    /// Initializes the handler with a real mapper, a fixed clock, and a repository that returns what it stores.
    /// </summary>
    public CreateDiscountPolicyHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(Now);
        _repository.CreateAsync(Arg.Any<DiscountPolicy>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<DiscountPolicy>());
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<DiscountPolicyProfile>()).CreateMapper();
        _handler = new CreateDiscountPolicyHandler(_repository, mapper, _timeProvider);
    }

    [Fact(DisplayName = "Given a valid command When handled Then stores the policy and returns it with its tiers in order")]
    public async Task Given_ValidCommand_When_Handled_Then_StoredAndMapped()
    {
        // Arrange
        var command = Command(Now.UtcDateTime.AddDays(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).CreateAsync(Arg.Is<DiscountPolicy>(policy => policy.Id == result.Id), Arg.Any<CancellationToken>());
        result.BranchId.Should().Be(command.BranchId);
        result.ProductId.Should().BeNull();
        result.MaxQuantityPerProduct.Should().Be(20);
        result.CreatedAt.Should().Be(Now.UtcDateTime);
        result.Tiers.Select(tier => (tier.MinQuantity, tier.MaxQuantity, tier.Percentage)).Should()
            .Equal((4, (int?)9, 10m), (10, (int?)20, 20m));
    }

    [Fact(DisplayName = "Given ValidFrom equal to now When handled Then the policy is stored")]
    public async Task Given_ValidFromEqualToNow_When_Handled_Then_Stored()
    {
        // Act
        await _handler.Handle(Command(Now.UtcDateTime), CancellationToken.None);

        // Assert
        await _repository.Received(1).CreateAsync(Arg.Any<DiscountPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given ValidFrom one microsecond before now When handled Then ValidFromInPast and nothing is stored")]
    public async Task Given_ValidFromInPast_When_Handled_Then_ValidFromInPast()
    {
        // Arrange
        var command = Command(Now.UtcDateTime.AddTicks(-10));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "ValidFrom" && error.ErrorCode == CreateDiscountPolicyHandler.ValidFromInPast);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<DiscountPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given a clock with sub-microsecond ticks When handled Then CreatedAt is truncated to microseconds")]
    public async Task Given_SubMicrosecondClock_When_Handled_Then_CreatedAtTruncated()
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(Now.AddTicks(1_234_567));

        // Act
        var result = await _handler.Handle(Command(Now.UtcDateTime.AddDays(1)), CancellationToken.None);

        // Assert
        result.CreatedAt.Should().Be(Now.UtcDateTime.AddTicks(1_234_560));
    }

    [Fact(DisplayName = "Given overlapping tiers When handled Then ValidationException and nothing is stored")]
    public async Task Given_OverlappingTiers_When_Handled_Then_ValidationException()
    {
        // Arrange
        var command = Command(Now.UtcDateTime.AddDays(1));
        command.Tiers = [Tier(4, 10, 10m), Tier(10, 20, 20m)];

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        await _repository.DidNotReceive().CreateAsync(Arg.Any<DiscountPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given a ValidFrom without time zone When handled Then ValidationException, not DomainException")]
    public async Task Given_UnspecifiedValidFrom_When_Handled_Then_ValidationException()
    {
        // Arrange
        var command = Command(DateTime.SpecifyKind(Now.UtcDateTime.AddDays(1), DateTimeKind.Unspecified));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    private static CreateDiscountPolicyCommand Command(DateTime validFrom) => new()
    {
        BranchId = Guid.NewGuid(),
        ValidFrom = validFrom,
        MaxQuantityPerProduct = 20,
        Tiers = [Tier(4, 9, 10m), Tier(10, 20, 20m)]
    };

    private static DiscountTierInput Tier(int min, int? max, decimal percentage) =>
        new() { MinQuantity = min, MaxQuantity = max, Percentage = percentage };
}
