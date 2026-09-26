using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

// Work item: TASK-060 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="DiscountPolicyResolver"/>: scope precedence, then the latest start, then the
/// latest creation.
/// </summary>
public class DiscountPolicyResolverTests
{
    private static readonly DateTime Start = DiscountPolicyTestData.Start;
    private static readonly DateTime SaleDate = Start.AddDays(30);
    private readonly Guid _branch = Guid.NewGuid();
    private readonly Guid _product = Guid.NewGuid();
    private readonly IDiscountPolicyRepository _repository = Substitute.For<IDiscountPolicyRepository>();
    private readonly DiscountPolicyResolver _resolver;

    /// <summary>
    /// Initializes the resolver over a substituted repository.
    /// </summary>
    public DiscountPolicyResolverTests()
    {
        _resolver = new DiscountPolicyResolver(_repository);
    }

    [Fact(DisplayName = "Given every scope When resolving Then the product and branch policy wins")]
    public async Task Given_AllScopes_When_Resolved_Then_ProductBranchWins()
    {
        // Arrange
        var productBranch = DiscountPolicyTestData.Create(_product, _branch);
        RepositoryReturns(
            DiscountPolicyTestData.Create(),
            DiscountPolicyTestData.Create(branchId: _branch),
            DiscountPolicyTestData.Create(_product),
            productBranch);

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(productBranch);
    }

    [Fact(DisplayName = "Given a product policy and a branch policy When resolving Then the product policy wins")]
    public async Task Given_ProductAndBranch_When_Resolved_Then_ProductWins()
    {
        // Arrange
        var product = DiscountPolicyTestData.Create(_product);
        RepositoryReturns(DiscountPolicyTestData.Create(branchId: _branch), product, DiscountPolicyTestData.Create());

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(product);
    }

    [Fact(DisplayName = "Given a branch policy and the default When resolving Then the branch policy wins")]
    public async Task Given_BranchAndDefault_When_Resolved_Then_BranchWins()
    {
        // Arrange
        var branch = DiscountPolicyTestData.Create(branchId: _branch);
        RepositoryReturns(DiscountPolicyTestData.Create(), branch);

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(branch);
    }

    [Fact(DisplayName = "Given only the default When resolving Then the default applies")]
    public async Task Given_OnlyDefault_When_Resolved_Then_DefaultApplies()
    {
        // Arrange
        var fallback = DiscountPolicyTestData.Create();
        RepositoryReturns(fallback);

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(fallback);
    }

    [Fact(DisplayName = "Given a more specific policy that started earlier When resolving Then specificity wins over the later start")]
    public async Task Given_MoreSpecificButOlder_When_Resolved_Then_SpecificityWins()
    {
        // Arrange
        var product = DiscountPolicyTestData.Create(_product, validFrom: Start);
        RepositoryReturns(product, DiscountPolicyTestData.Create(validFrom: Start.AddDays(10)));

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(product);
    }

    [Fact(DisplayName = "Given two default policies with different starts When resolving Then the later start wins")]
    public async Task Given_TwoDefaults_When_Resolved_Then_LaterValidFromWins()
    {
        // Arrange
        var later = DiscountPolicyTestData.Create(validFrom: Start.AddDays(10), createdAt: Start);
        RepositoryReturns(DiscountPolicyTestData.Create(validFrom: Start, createdAt: Start), later);

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(later);
    }

    [Fact(DisplayName = "Given two default policies with the same start When resolving Then the later creation wins")]
    public async Task Given_SameValidFrom_When_Resolved_Then_LaterCreatedAtWins()
    {
        // Arrange
        var newer = DiscountPolicyTestData.Create(validFrom: Start, createdAt: Start.AddDays(-1));
        RepositoryReturns(newer, DiscountPolicyTestData.Create(validFrom: Start, createdAt: Start.AddDays(-2)));

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(newer);
    }

    [Fact(DisplayName = "Given a policy of another product When resolving Then it is not applied to this product")]
    public async Task Given_OtherProductPolicy_When_Resolved_Then_Ignored()
    {
        // Arrange
        var other = Guid.NewGuid();
        var fallback = DiscountPolicyTestData.Create();
        RepositoryReturns(DiscountPolicyTestData.Create(other), fallback);

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product, other], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(fallback);
        result[other].ProductId.Should().Be(other);
    }

    [Fact(DisplayName = "Given a policy of another branch When resolving Then it is not applied")]
    public async Task Given_OtherBranchPolicy_When_Resolved_Then_Ignored()
    {
        // Arrange
        var fallback = DiscountPolicyTestData.Create();
        RepositoryReturns(DiscountPolicyTestData.Create(branchId: Guid.NewGuid()), fallback);

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result[_product].Should().BeSameAs(fallback);
    }

    [Fact(DisplayName = "Given no candidate When resolving Then the product is absent from the result")]
    public async Task Given_NoPolicy_When_Resolved_Then_ProductAbsent()
    {
        // Arrange
        RepositoryReturns();

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product], SaleDate);

        // Assert
        result.Should().NotContainKey(_product);
    }

    [Fact(DisplayName = "Given repeated product ids When resolving Then the repository is queried once with distinct ids and the sale date")]
    public async Task Given_DuplicateProducts_When_Resolved_Then_SingleDistinctQuery()
    {
        // Arrange
        RepositoryReturns(DiscountPolicyTestData.Create());

        // Act
        var result = await _resolver.ResolveAsync(_branch, [_product, _product], SaleDate);

        // Assert
        result.Should().ContainSingle();
        await _repository.Received(1).GetApplicableAsync(
            _branch,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(_product)),
            SaleDate,
            Arg.Any<CancellationToken>());
    }

    private void RepositoryReturns(params DiscountPolicy[] policies) =>
        _repository.GetApplicableAsync(_branch, Arg.Any<IReadOnlyCollection<Guid>>(), SaleDate, Arg.Any<CancellationToken>())
            .Returns(policies);
}
