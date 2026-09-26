using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.DiscountPolicies;

// Work item: TD-032
/// <summary>
/// Contains unit tests for the <see cref="DisableDiscountPoliciesHandler"/> class: all or nothing, one instant for the
/// whole request, and the result in request order.
/// </summary>
public class DisableDiscountPoliciesHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    private readonly IDiscountPolicyRepository _repository = Substitute.For<IDiscountPolicyRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly DisableDiscountPoliciesHandler _handler;

    /// <summary>
    /// Initializes the handler with a real mapper and a fixed clock.
    /// </summary>
    public DisableDiscountPoliciesHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(Now);
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<DiscountPolicyProfile>()).CreateMapper();
        _handler = new DisableDiscountPoliciesHandler(_repository, mapper, _timeProvider);
    }

    [Fact(DisplayName = "Given one unknown id When handled Then KeyNotFoundException naming it and nothing is saved")]
    public async Task Given_UnknownId_When_Handled_Then_NotFoundAndNothingSaved()
    {
        // Arrange
        var known = DiscountPolicyTestData.Create(Guid.NewGuid());
        var unknown = Guid.NewGuid();
        Returns(known);

        // Act
        var act = () => _handler.Handle(new DisableDiscountPoliciesCommand { Ids = [known.Id, unknown] }, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
        exception.Which.Message.Should().Contain(unknown.ToString()).And.NotContain(known.Id.ToString());
        known.DisabledAt.Should().BeNull();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<IReadOnlyCollection<DiscountPolicy>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given known ids When handled Then every policy gets the same instant, one save, and the request order")]
    public async Task Given_KnownIds_When_Handled_Then_DisabledSavedOnceInRequestOrder()
    {
        // Arrange
        var first = DiscountPolicyTestData.Create(Guid.NewGuid());
        var second = DiscountPolicyTestData.Create(Guid.NewGuid());
        Returns(first, second);

        // Act
        var result = await _handler.Handle(new DisableDiscountPoliciesCommand { Ids = [second.Id, first.Id] }, CancellationToken.None);

        // Assert
        first.DisabledAt.Should().Be(Now.UtcDateTime);
        second.DisabledAt.Should().Be(Now.UtcDateTime);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<IReadOnlyCollection<DiscountPolicy>>(policies => policies.Count == 2), Arg.Any<CancellationToken>());
        result.Policies.Select(policy => policy.Id).Should().Equal(second.Id, first.Id);
        result.Policies.Should().OnlyContain(policy => policy.DisabledAt == Now.UtcDateTime);
    }

    [Fact(DisplayName = "Given an already disabled policy When handled Then it keeps its first instant")]
    public async Task Given_AlreadyDisabled_When_Handled_Then_KeepsFirstInstant()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(Guid.NewGuid());
        var first = Now.UtcDateTime.AddDays(-1);
        policy.Disable(first);
        Returns(policy);

        // Act
        var result = await _handler.Handle(new DisableDiscountPoliciesCommand { Ids = [policy.Id] }, CancellationToken.None);

        // Assert
        result.Policies.Should().ContainSingle().Which.DisabledAt.Should().Be(first);
    }

    [Fact(DisplayName = "Given a clock with sub-microsecond ticks When handled Then DisabledAt is truncated to microseconds")]
    public async Task Given_SubMicrosecondClock_When_Handled_Then_DisabledAtTruncated()
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(Now.AddTicks(1_234_567));
        var policy = DiscountPolicyTestData.Create(Guid.NewGuid());
        Returns(policy);

        // Act
        await _handler.Handle(new DisableDiscountPoliciesCommand { Ids = [policy.Id] }, CancellationToken.None);

        // Assert
        policy.DisabledAt.Should().Be(Now.UtcDateTime.AddTicks(1_234_560));
    }

    [Fact(DisplayName = "Given a repeated id When handled Then ValidationException and nothing is loaded")]
    public async Task Given_RepeatedId_When_Handled_Then_ValidationException()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var act = () => _handler.Handle(new DisableDiscountPoliciesCommand { Ids = [id, id] }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        await _repository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    private void Returns(params DiscountPolicy[] policies)
    {
        IReadOnlyList<DiscountPolicy> found = policies;
        _repository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(found);
    }
}
