using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

// Work item: TASK-060 (FEAT-001)
/// <summary>
/// Builds discount policies for tests. With no arguments it builds the seeded README policy: default scope, starting
/// 2026-01-01 UTC, at most 20 units, 4 to 9 units 10%, 10 to 20 units 20%.
/// </summary>
public static class DiscountPolicyTestData
{
    /// <summary>
    /// The start of the seeded default policy, 2026-01-01T00:00:00Z.
    /// </summary>
    public static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Returns the README tiers.
    /// </summary>
    public static IReadOnlyList<DiscountTier> ReadmeTiers() =>
        [new DiscountTier(4, 9, 10m), new DiscountTier(10, 20, 20m)];

    /// <summary>
    /// Creates a policy. The creation instant defaults to the start, the latest instant a policy may be created at.
    /// </summary>
    public static DiscountPolicy Create(
        Guid? productId = null,
        Guid? branchId = null,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        int max = 20,
        IEnumerable<DiscountTier>? tiers = null,
        DateTime? createdAt = null)
    {
        var from = validFrom ?? Start;
        return DiscountPolicy.Create(productId, branchId, from, validTo, max, tiers ?? ReadmeTiers(), now: createdAt ?? from);
    }
}
