using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

// Work item: TASK-061 (FEAT-001)
/// <summary>
/// Implementation of IDiscountPolicyRepository using Entity Framework Core. Tiers are an owned collection, so every
/// query loads them with their policy.
/// </summary>
public class DiscountPolicyRepository : IDiscountPolicyRepository
{
    private static readonly SortField[] DefaultOrder = [new("ValidFrom", false)];

    private readonly DefaultContext _context;

    /// <summary>
    /// Initializes a new instance of DiscountPolicyRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public DiscountPolicyRepository(DefaultContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Stores a new policy with its tiers in a single save
    /// </summary>
    public async Task<DiscountPolicy> CreateAsync(DiscountPolicy policy, CancellationToken cancellationToken = default)
    {
        await _context.DiscountPolicies.AddAsync(policy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return policy;
    }

    /// <summary>
    /// Retrieves a policy with its tiers, without tracking
    /// </summary>
    public async Task<DiscountPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DiscountPolicies.AsNoTracking().FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);
    }

    // Work item: TD-032
    /// <summary>
    /// Retrieves the policies with the given ids and their tiers in one query, tracked so that <see cref="UpdateAsync"/>
    /// saves what changed
    /// </summary>
    public async Task<IReadOnlyList<DiscountPolicy>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var distinctIds = ids.Distinct().ToList();
        return await _context.DiscountPolicies
            .Where(policy => distinctIds.Contains(policy.Id))
            .ToListAsync(cancellationToken);
    }

    // Work item: TD-032
    /// <summary>
    /// Saves the policies in a single save: tracked ones by their changes, detached ones by marking only the policy row
    /// modified first. Tiers never change after creation, and marking a detached graph would try to insert them again,
    /// because their shadow key is lost when a policy is read without tracking
    /// </summary>
    public async Task UpdateAsync(IReadOnlyCollection<DiscountPolicy> policies, CancellationToken cancellationToken = default)
    {
        foreach (var policy in policies)
        {
            var entry = _context.Entry(policy);
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Modified;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // Work item: TASK-061 (FEAT-001), TD-032
    /// <summary>
    /// Retrieves the policies not disabled and in effect at the date for the products (or every product) and the branch
    /// (or every branch)
    /// </summary>
    public async Task<IReadOnlyList<DiscountPolicy>> GetApplicableAsync(
        Guid branchId, IReadOnlyCollection<Guid> productIds, DateTime date, CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToList();
        return await _context.DiscountPolicies.AsNoTracking()
            .Where(policy => (policy.ProductId == null || ids.Contains(policy.ProductId.Value))
                             && (policy.BranchId == null || policy.BranchId == branchId)
                             && policy.DisabledAt == null
                             && policy.ValidFrom <= date
                             && (policy.ValidTo == null || date < policy.ValidTo))
            .ToListAsync(cancellationToken);
    }

    // Work item: TASK-061 (FEAT-001), TD-032
    /// <summary>
    /// Retrieves one page of policies that match the query's filters, in the query's order or else by start date,
    /// leaving out disabled policies unless asked. A page past the last one, including one whose offset does not fit an
    /// int, is empty
    /// </summary>
    public async Task<(IReadOnlyList<DiscountPolicy> Items, int TotalCount)> ListAsync(
        ListQuery query, bool includeDisabled = false, CancellationToken cancellationToken = default)
    {
        var policies = _context.DiscountPolicies.AsNoTracking();
        if (!includeDisabled)
            policies = policies.Where(policy => policy.DisabledAt == null);

        var rows = policies
            .ApplyFilters(query.Filters)
            .ApplyOrder(query.Order, DefaultOrder);
        var totalCount = await rows.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.Size;
        StepTrace.Step("CMN-LST-09", "Filter, order with Id as tiebreak, count, then page",
            [("filters", query.Filters.Count), ("sortFields", query.Order.Count), ("total", totalCount), ("page", query.Page), ("size", query.Size),
             ("pastEnd", offset >= totalCount)]);
        if (offset >= totalCount)
            return ([], totalCount);

        var items = await rows.Skip((int)offset).Take(query.Size).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
