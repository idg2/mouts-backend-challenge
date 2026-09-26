using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Domain.Services;

// Work item: TASK-060 (FEAT-001)
/// <summary>
/// Picks the discount policy of each product of a sale. The repository returns every policy in effect at the sale date
/// for the products and the branch; the resolver keeps, per product, the most specific scope (product and branch,
/// product, branch, default), then the latest <see cref="DiscountPolicy.ValidFrom"/>, then the latest
/// <see cref="DiscountPolicy.CreatedAt"/>, then the highest id so the choice is always the same.
/// </summary>
public class DiscountPolicyResolver
{
    private readonly IDiscountPolicyRepository _repository;

    /// <summary>
    /// Initializes a new instance of DiscountPolicyResolver
    /// </summary>
    /// <param name="repository">The discount policy repository</param>
    public DiscountPolicyResolver(IDiscountPolicyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Resolves the policy of every product with one repository query.
    /// </summary>
    /// <param name="branchId">The sale's branch</param>
    /// <param name="productIds">The sale's products; repeated ids are ignored</param>
    /// <param name="saleDate">The sale date; never the current date for an existing sale</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policy per product id; a product without any policy is absent</returns>
    public async Task<IReadOnlyDictionary<Guid, DiscountPolicy>> ResolveAsync(
        Guid branchId, IReadOnlyCollection<Guid> productIds, DateTime saleDate, CancellationToken cancellationToken = default)
    {
        var distinctProducts = productIds.Distinct().ToList();
        var candidates = await _repository.GetApplicableAsync(branchId, distinctProducts, saleDate, cancellationToken);

        var result = new Dictionary<Guid, DiscountPolicy>();
        foreach (var productId in distinctProducts)
        {
            var policy = candidates
                .Where(candidate => (candidate.ProductId is null || candidate.ProductId == productId)
                                    && (candidate.BranchId is null || candidate.BranchId == branchId))
                .OrderByDescending(candidate => candidate.SpecificityRank)
                .ThenByDescending(candidate => candidate.ValidFrom)
                .ThenByDescending(candidate => candidate.CreatedAt)
                .ThenByDescending(candidate => candidate.Id)
                .FirstOrDefault();

            if (policy is not null)
                result[productId] = policy;
        }

        return result;
    }
}
