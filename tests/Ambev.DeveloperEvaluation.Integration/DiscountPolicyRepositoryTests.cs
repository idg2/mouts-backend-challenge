using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-061 (FEAT-001), TD-032
/// <summary>
/// Contains integration tests for the <see cref="DiscountPolicyRepository"/> class against PostgreSQL. No test creates a
/// default-scope policy, so the seeded one is the only default in the shared database; every other policy uses fresh
/// product or branch ids.
/// </summary>
public class DiscountPolicyRepositoryTests : IClassFixture<PostgresFixture>
{
    private static readonly DateTime Start = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public DiscountPolicyRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Given the migrations When loading the default policy Then it carries the README rules")]
    public async Task Given_Migrations_When_LoadingDefault_Then_ReadmeRules()
    {
        // Act
        await using var context = _fixture.CreateContext();
        var policy = await new DiscountPolicyRepository(context).GetByIdAsync(DiscountPolicyConfiguration.DefaultPolicyId);

        // Assert
        Assert.NotNull(policy);
        Assert.Null(policy.ProductId);
        Assert.Null(policy.BranchId);
        Assert.Null(policy.ValidTo);
        Assert.Equal(20, policy.MaxQuantityPerProduct);
        Assert.Equal(DiscountPolicyConfiguration.DefaultPolicyValidFrom, policy.ValidFrom);
        Assert.Equal(DateTimeKind.Utc, policy.ValidFrom.Kind);
        Assert.Equal(policy.ValidFrom, policy.CreatedAt);
        Assert.Collection(policy.Tiers.OrderBy(tier => tier.MinQuantity),
            tier => { Assert.Equal(4, tier.MinQuantity); Assert.Equal(9, tier.MaxQuantity); Assert.Equal(10m, tier.Percentage); },
            tier => { Assert.Equal(10, tier.MinQuantity); Assert.Equal(20, tier.MaxQuantity); Assert.Equal(20m, tier.Percentage); });
    }

    [Fact(DisplayName = "Given a new policy When reading it back Then every field and tier is stored")]
    public async Task Given_NewPolicy_When_ReadBack_Then_EveryFieldAndTierStored()
    {
        // Arrange
        var created = DiscountPolicy.Create(Guid.NewGuid(), Guid.NewGuid(), Start, Start.AddDays(30), 12,
            [new DiscountTier(2, 3, 5m), new DiscountTier(4, null, 7.5m)], now: Start.AddDays(-1));
        await SaveAsync(created);

        // Act
        await using var context = _fixture.CreateContext();
        var loaded = await new DiscountPolicyRepository(context).GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(created.ProductId, loaded.ProductId);
        Assert.Equal(created.BranchId, loaded.BranchId);
        Assert.Equal(Start, loaded.ValidFrom);
        Assert.Equal(Start.AddDays(30), loaded.ValidTo);
        Assert.Equal(12, loaded.MaxQuantityPerProduct);
        Assert.Equal(Start.AddDays(-1), loaded.CreatedAt);
        Assert.Equal(
            new[] { (2, (int?)3, 5m), (4, (int?)null, 7.5m) },
            loaded.Tiers.OrderBy(tier => tier.MinQuantity).Select(tier => (tier.MinQuantity, tier.MaxQuantity, tier.Percentage)));
    }

    [Fact(DisplayName = "Given policies of several scopes When getting the applicable ones Then only matching scopes in effect return, with tiers")]
    public async Task Given_Policies_When_GettingApplicable_Then_ScopeAndDateFiltered()
    {
        // Arrange
        var product = Guid.NewGuid();
        var branch = Guid.NewGuid();
        var productPolicy = await SaveAsync(Policy(product, null, Start));
        var branchPolicy = await SaveAsync(Policy(null, branch, Start));
        await SaveAsync(Policy(Guid.NewGuid(), null, Start));
        await SaveAsync(Policy(null, Guid.NewGuid(), Start));
        await SaveAsync(Policy(product, branch, Start.AddDays(100)));

        // Act
        await using var context = _fixture.CreateContext();
        var result = await new DiscountPolicyRepository(context).GetApplicableAsync(branch, [product], Start.AddDays(1));

        // Assert
        Assert.Equal(
            new[] { DiscountPolicyConfiguration.DefaultPolicyId, productPolicy.Id, branchPolicy.Id }.Order(),
            result.Select(policy => policy.Id).Order());
        Assert.All(result, policy => Assert.NotEmpty(policy.Tiers));
    }

    [Fact(DisplayName = "Given a bounded policy When the date equals its ValidTo Then it is not applicable")]
    public async Task Given_BoundedPolicy_When_DateEqualsValidTo_Then_Excluded()
    {
        // Arrange
        var product = Guid.NewGuid();
        var end = Start.AddDays(10);
        var bounded = await SaveAsync(Policy(product, null, Start, end));

        // Act
        await using var context = _fixture.CreateContext();
        var repository = new DiscountPolicyRepository(context);
        var justBefore = await repository.GetApplicableAsync(Guid.NewGuid(), [product], end.AddTicks(-10));
        var atEnd = await repository.GetApplicableAsync(Guid.NewGuid(), [product], end);

        // Assert
        Assert.Contains(bounded.Id, justBefore.Select(policy => policy.Id));
        Assert.DoesNotContain(bounded.Id, atEnd.Select(policy => policy.Id));
    }

    [Fact(DisplayName = "Given a product filter, an order, and a page When listing Then returns that page with tiers and the total")]
    public async Task Given_ProductFilter_When_Listing_Then_FilteredOrderedPagedWithTiers()
    {
        // Arrange
        var product = Guid.NewGuid();
        await SaveAsync(Policy(product, null, Start));
        var second = await SaveAsync(Policy(product, null, Start.AddDays(1)));
        var third = await SaveAsync(Policy(product, null, Start.AddDays(2)));
        await SaveAsync(Policy(Guid.NewGuid(), null, Start.AddDays(3)));
        var query = new ListQuery
        {
            Page = 1,
            Size = 2,
            Filters = [new FieldFilter("ProductId", FilterOperator.Equal, product)],
            Order = [new SortField("ValidFrom", true)]
        };

        // Act
        await using var context = _fixture.CreateContext();
        var (items, total) = await new DiscountPolicyRepository(context).ListAsync(query);

        // Assert
        Assert.Equal(3, total);
        Assert.Equal(new[] { third.Id, second.Id }, items.Select(policy => policy.Id));
        Assert.All(items, policy => Assert.NotEmpty(policy.Tiers));
    }

    [Fact(DisplayName = "Given a ValidFrom range When listing Then only policies starting in the range return")]
    public async Task Given_ValidFromRange_When_Listing_Then_OnlyPoliciesStartingInRange()
    {
        // Arrange
        var product = Guid.NewGuid();
        await SaveAsync(Policy(product, null, Start));
        var inside = await SaveAsync(Policy(product, null, Start.AddDays(5)));
        await SaveAsync(Policy(product, null, Start.AddDays(10)));
        var query = new ListQuery
        {
            Filters =
            [
                new FieldFilter("ProductId", FilterOperator.Equal, product),
                new FieldFilter("ValidFrom", FilterOperator.GreaterThanOrEqual, Start.AddDays(1)),
                new FieldFilter("ValidFrom", FilterOperator.LessThanOrEqual, Start.AddDays(9))
            ]
        };

        // Act
        await using var context = _fixture.CreateContext();
        var (items, total) = await new DiscountPolicyRepository(context).ListAsync(query);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal(inside.Id, Assert.Single(items).Id);
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given a disabled policy inside its validity When getting the applicable ones Then it is not returned")]
    public async Task Given_DisabledPolicy_When_GettingApplicable_Then_Excluded()
    {
        // Arrange
        var product = Guid.NewGuid();
        var active = await SaveAsync(Policy(product, null, Start));
        var disabled = Policy(product, null, Start);
        disabled.Disable(Start.AddDays(1));
        await SaveAsync(disabled);

        // Act
        await using var context = _fixture.CreateContext();
        var result = await new DiscountPolicyRepository(context).GetApplicableAsync(Guid.NewGuid(), [product], Start.AddDays(2));

        // Assert
        var ids = result.Select(policy => policy.Id).ToList();
        Assert.Contains(active.Id, ids);
        Assert.DoesNotContain(disabled.Id, ids);
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given a disabled policy When listing Then it is hidden by default and returned with includeDisabled")]
    public async Task Given_DisabledPolicy_When_Listing_Then_HiddenUnlessIncluded()
    {
        // Arrange
        var product = Guid.NewGuid();
        var active = await SaveAsync(Policy(product, null, Start));
        var disabled = Policy(product, null, Start.AddDays(1));
        disabled.Disable(Start);
        await SaveAsync(disabled);
        var query = new ListQuery { Filters = [new FieldFilter("ProductId", FilterOperator.Equal, product)] };

        // Act
        await using var context = _fixture.CreateContext();
        var repository = new DiscountPolicyRepository(context);
        var (hidden, hiddenTotal) = await repository.ListAsync(query);
        var (included, includedTotal) = await repository.ListAsync(query, includeDisabled: true);

        // Assert
        Assert.Equal(1, hiddenTotal);
        Assert.Equal(active.Id, Assert.Single(hidden).Id);
        Assert.Equal(2, includedTotal);
        Assert.Equal(new[] { active.Id, disabled.Id }, included.Select(policy => policy.Id));
        Assert.Equal(Start, included[1].DisabledAt);
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given stored policies When getting them by ids, disabling, and updating Then DisabledAt persists")]
    public async Task Given_StoredPolicies_When_DisabledAndUpdated_Then_DisabledAtPersists()
    {
        // Arrange
        var first = await SaveAsync(Policy(Guid.NewGuid(), null, Start));
        var second = await SaveAsync(Policy(Guid.NewGuid(), null, Start));
        var untouched = await SaveAsync(Policy(Guid.NewGuid(), null, Start));
        var disabledAt = new DateTime(2026, 9, 26, 10, 30, 15, DateTimeKind.Utc);

        // Act
        await using (var context = _fixture.CreateContext())
        {
            var repository = new DiscountPolicyRepository(context);
            var loaded = await repository.GetByIdsAsync([first.Id, second.Id, Guid.NewGuid()]);
            Assert.Equal(new[] { first.Id, second.Id }.Order(), loaded.Select(policy => policy.Id).Order());
            Assert.All(loaded, policy => Assert.NotEmpty(policy.Tiers));
            foreach (var policy in loaded)
                policy.Disable(disabledAt);
            await repository.UpdateAsync(loaded);
        }

        // Assert
        await using var readContext = _fixture.CreateContext();
        var readRepository = new DiscountPolicyRepository(readContext);
        Assert.Equal(disabledAt, (await readRepository.GetByIdAsync(first.Id))!.DisabledAt);
        Assert.Equal(disabledAt, (await readRepository.GetByIdAsync(second.Id))!.DisabledAt);
        Assert.Null((await readRepository.GetByIdAsync(untouched.Id))!.DisabledAt);
    }

    // Work item: TD-032
    [Fact(DisplayName = "Given a policy read without tracking When disabled and updated in another context Then DisabledAt persists and the tiers stay")]
    public async Task Given_DetachedPolicy_When_DisabledAndUpdated_Then_DisabledAtPersistsAndTiersStay()
    {
        // Arrange
        var stored = await SaveAsync(DiscountPolicy.Create(Guid.NewGuid(), null, Start, null, 20,
            [new DiscountTier(4, 9, 10m), new DiscountTier(10, 20, 20m)], now: Start));
        var disabledAt = new DateTime(2026, 9, 26, 11, 0, 0, DateTimeKind.Utc);
        DiscountPolicy detached;
        await using (var readContext = _fixture.CreateContext())
            detached = (await new DiscountPolicyRepository(readContext).GetByIdAsync(stored.Id))!;
        detached.Disable(disabledAt);

        // Act
        await using (var writeContext = _fixture.CreateContext())
            await new DiscountPolicyRepository(writeContext).UpdateAsync([detached]);

        // Assert
        await using var checkContext = _fixture.CreateContext();
        var loaded = await new DiscountPolicyRepository(checkContext).GetByIdAsync(stored.Id);
        Assert.NotNull(loaded);
        Assert.Equal(disabledAt, loaded.DisabledAt);
        Assert.Equal(
            new[] { (4, (int?)9, 10m), (10, (int?)20, 20m) },
            loaded.Tiers.OrderBy(tier => tier.MinQuantity).Select(tier => (tier.MinQuantity, tier.MaxQuantity, tier.Percentage)));
    }

    private static DiscountPolicy Policy(Guid? product, Guid? branch, DateTime from, DateTime? to = null) =>
        DiscountPolicy.Create(product, branch, from, to, 20, [new DiscountTier(4, 9, 10m)], now: from);

    private async Task<DiscountPolicy> SaveAsync(DiscountPolicy policy)
    {
        await using var context = _fixture.CreateContext();
        return await new DiscountPolicyRepository(context).CreateAsync(policy);
    }
}
