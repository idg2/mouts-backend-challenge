using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.ListDiscountPolicies;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.DiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="ListDiscountPoliciesHandler"/> class.
/// </summary>
public class ListDiscountPoliciesHandlerTests
{
    private readonly IDiscountPolicyRepository _repository = Substitute.For<IDiscountPolicyRepository>();
    private readonly ListDiscountPoliciesHandler _handler;

    /// <summary>
    /// Initializes the handler with a real mapper.
    /// </summary>
    public ListDiscountPoliciesHandlerTests()
    {
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<DiscountPolicyProfile>()).CreateMapper();
        _handler = new ListDiscountPoliciesHandler(_repository, mapper);
    }

    // Work item: TASK-063 (FEAT-001), TD-032
    [Fact(DisplayName = "Given filters, an order, and a page When listing Then passes them to the repository and maps the page")]
    public async Task Given_Query_When_Handled_Then_PassesItThroughAndMapsThePage()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(Guid.NewGuid());
        var filters = new[] { new FieldFilter("ProductId", FilterOperator.Equal, policy.ProductId!.Value) };
        var order = new[] { new SortField("ValidFrom", true) };
        IReadOnlyList<DiscountPolicy> policies = new List<DiscountPolicy> { policy };
        _repository.ListAsync(Arg.Any<ListQuery>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns((policies, 7));
        var command = new ListDiscountPoliciesCommand { Page = 2, Size = 3, Filters = filters, Order = order };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).ListAsync(
            Arg.Is<ListQuery>(query => query.Page == 2 && query.Size == 3 && query.Filters.SequenceEqual(filters) && query.Order.SequenceEqual(order)),
            false,
            Arg.Any<CancellationToken>());
        result.TotalCount.Should().Be(7);
        result.Page.Should().Be(2);
        result.Size.Should().Be(3);
        result.Items.Should().ContainSingle().Which.Tiers.Should().HaveCount(2);
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given IncludeDisabled When listing Then asks the repository for disabled policies and maps DisabledAt")]
    public async Task Given_IncludeDisabled_When_Handled_Then_PassedAndDisabledAtMapped()
    {
        // Arrange
        var policy = DiscountPolicyTestData.Create(Guid.NewGuid());
        var disabledAt = DiscountPolicyTestData.Start.AddDays(2);
        policy.Disable(disabledAt);
        IReadOnlyList<DiscountPolicy> policies = new List<DiscountPolicy> { policy };
        _repository.ListAsync(Arg.Any<ListQuery>(), true, Arg.Any<CancellationToken>()).Returns((policies, 1));

        // Act
        var result = await _handler.Handle(new ListDiscountPoliciesCommand { IncludeDisabled = true }, CancellationToken.None);

        // Assert
        await _repository.Received(1).ListAsync(Arg.Any<ListQuery>(), true, Arg.Any<CancellationToken>());
        result.Items.Should().ContainSingle().Which.DisabledAt.Should().Be(disabledAt);
    }

    [Fact(DisplayName = "Given a size of zero When listing Then throws ValidationException")]
    public async Task Given_SizeZero_When_Handled_Then_ValidationException()
    {
        // Act
        var act = () => _handler.Handle(new ListDiscountPoliciesCommand { Size = 0 }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
