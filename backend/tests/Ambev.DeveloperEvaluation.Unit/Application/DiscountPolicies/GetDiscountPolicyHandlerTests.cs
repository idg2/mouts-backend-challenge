using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.GetDiscountPolicy;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.DiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="GetDiscountPolicyHandler"/> class.
/// </summary>
public class GetDiscountPolicyHandlerTests
{
    private readonly IDiscountPolicyRepository _repository = Substitute.For<IDiscountPolicyRepository>();
    private readonly GetDiscountPolicyHandler _handler;

    /// <summary>
    /// Initializes the handler with a real mapper.
    /// </summary>
    public GetDiscountPolicyHandlerTests()
    {
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<DiscountPolicyProfile>()).CreateMapper();
        _handler = new GetDiscountPolicyHandler(_repository, mapper);
    }

    [Fact(DisplayName = "Given a stored policy When getting it Then returns it with the tiers ordered by minimum")]
    public async Task Given_StoredPolicy_When_Handled_Then_ReturnsItWithOrderedTiers()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(tiers: [new DiscountTier(4, 9, 10m), new DiscountTier(10, 20, 20m)]);
        _repository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        // Act
        var result = await _handler.Handle(new GetDiscountPolicyCommand(policy.Id), CancellationToken.None);

        // Assert
        result.Id.Should().Be(policy.Id);
        result.ValidFrom.Should().Be(DiscountPolicyTestData.Start);
        result.CreatedAt.Should().Be(DiscountPolicyTestData.Start);
        result.Tiers.Select(tier => tier.MinQuantity).Should().Equal(4, 10);
    }

    [Fact(DisplayName = "Given an unknown id When getting Then throws KeyNotFoundException")]
    public async Task Given_UnknownId_When_Handled_Then_KeyNotFound()
    {
        // Act
        var act = () => _handler.Handle(new GetDiscountPolicyCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Given the empty id When getting Then throws ValidationException")]
    public async Task Given_EmptyId_When_Handled_Then_ValidationException()
    {
        // Act
        var act = () => _handler.Handle(new GetDiscountPolicyCommand(Guid.Empty), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
