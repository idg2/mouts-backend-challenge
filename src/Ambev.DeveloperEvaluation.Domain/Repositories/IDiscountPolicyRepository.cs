using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-060 (FEAT-001), TD-032
/// <summary>
/// Repository interface for discount policies. Policies are never edited or deleted; the only update is disabling.
/// </summary>
public interface IDiscountPolicyRepository
{
    /// <summary>
    /// Stores a new policy with its tiers
    /// </summary>
    /// <param name="policy">The policy to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored policy</returns>
    Task<DiscountPolicy> CreateAsync(DiscountPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a policy with its tiers, without tracking
    /// </summary>
    /// <param name="id">The policy id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policy if found, null otherwise</returns>
    Task<DiscountPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Work item: TD-032
    /// <summary>
    /// Retrieves, in one query and tracked for <see cref="UpdateAsync"/>, the policies with the given ids and their tiers
    /// </summary>
    /// <param name="ids">The policy ids</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policies found; an unknown id is simply missing</returns>
    Task<IReadOnlyList<DiscountPolicy>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    // Work item: TD-032
    /// <summary>
    /// Saves the given policies' own fields in a single save, whether they were loaded by <see cref="GetByIdsAsync"/> or
    /// not; tiers are never written, since they do not change after creation
    /// </summary>
    /// <param name="policies">The changed policies, usually loaded by <see cref="GetByIdsAsync"/></param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(IReadOnlyCollection<DiscountPolicy> policies, CancellationToken cancellationToken = default);

    // Work item: TASK-060 (FEAT-001), TD-032
    /// <summary>
    /// Retrieves, in one query and with their tiers, the policies not disabled and in effect at <paramref name="date"/>
    /// whose product is null or one of <paramref name="productIds"/> and whose branch is null or <paramref name="branchId"/>
    /// </summary>
    /// <param name="branchId">The sale's branch</param>
    /// <param name="productIds">The sale's products</param>
    /// <param name="date">The sale date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Every candidate; choosing one per product is the resolver's job</returns>
    Task<IReadOnlyList<DiscountPolicy>> GetApplicableAsync(
        Guid branchId, IReadOnlyCollection<Guid> productIds, DateTime date, CancellationToken cancellationToken = default);

    // Work item: TASK-060 (FEAT-001), TD-032
    /// <summary>
    /// Retrieves one page of policies, with their tiers, that match the query's filters, in the query's order or else
    /// by start date. Disabled policies are left out unless <paramref name="includeDisabled"/> is true
    /// </summary>
    /// <param name="query">The page, size, filters, and sort fields</param>
    /// <param name="includeDisabled">Whether disabled policies are listed too</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policies on the page and the number of policies that match the filters</returns>
    Task<(IReadOnlyList<DiscountPolicy> Items, int TotalCount)> ListAsync(
        ListQuery query, bool includeDisabled = false, CancellationToken cancellationToken = default);
}
