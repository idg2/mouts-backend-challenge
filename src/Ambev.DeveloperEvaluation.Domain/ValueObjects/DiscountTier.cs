namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

// Work item: TASK-060 (FEAT-001)
/// <summary>
/// One quantity band of a discount policy: a product total from <see cref="MinQuantity"/> to <see cref="MaxQuantity"/>
/// (inclusive, open when null) allows at most <see cref="Percentage"/> percent of discount.
/// </summary>
public sealed class DiscountTier
{
    /// <summary>
    /// Gets the lowest product total the tier covers, at least 1.
    /// </summary>
    public int MinQuantity { get; private set; }

    /// <summary>
    /// Gets the highest product total the tier covers, or null for no upper bound below the policy maximum.
    /// </summary>
    public int? MaxQuantity { get; private set; }

    /// <summary>
    /// Gets the discount ceiling in percent, above 0 and at most 100.
    /// </summary>
    public decimal Percentage { get; private set; }

    // Used by EF Core to materialize the owned rows.
    private DiscountTier()
    {
    }

    /// <summary>
    /// Initializes a new tier.
    /// </summary>
    /// <param name="minQuantity">The lowest product total the tier covers</param>
    /// <param name="maxQuantity">The highest product total the tier covers, or null</param>
    /// <param name="percentage">The discount ceiling in percent</param>
    /// <exception cref="DomainException">A bound or the percentage is out of range</exception>
    public DiscountTier(int minQuantity, int? maxQuantity, decimal percentage)
    {
        if (minQuantity < 1)
            throw new DomainException("Tier minimum quantity must be at least 1");
        if (maxQuantity is not null && maxQuantity < minQuantity)
            throw new DomainException($"Tier maximum quantity {maxQuantity} is below its minimum {minQuantity}");
        if (percentage <= 0 || percentage > 100)
            throw new DomainException($"Tier percentage {percentage} must be greater than 0 and at most 100");

        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        Percentage = percentage;
    }

    /// <summary>
    /// Returns whether a product total falls in the tier.
    /// </summary>
    /// <param name="quantity">The product total</param>
    /// <returns>True when the total is within the bounds</returns>
    public bool Contains(int quantity) =>
        quantity >= MinQuantity && (MaxQuantity is null || quantity <= MaxQuantity);

    /// <summary>
    /// Checks a tier set in the order given: tiers sorted by minimum, not overlapping, and within the policy maximum.
    /// Gaps are allowed, and an empty set is valid.
    /// </summary>
    /// <param name="ranges">The tier bounds, in the order they will be stored</param>
    /// <param name="maxQuantityPerProduct">The policy maximum per product</param>
    /// <returns>A message describing the first violation, or null when the set is valid</returns>
    public static string? FindSetViolation(IEnumerable<(int Min, int? Max)> ranges, int maxQuantityPerProduct)
    {
        (int Min, int? Max)? previous = null;
        foreach (var range in ranges)
        {
            if (range.Min > maxQuantityPerProduct || range.Max > maxQuantityPerProduct)
                return $"Tier {Describe(range)} exceeds the policy maximum of {maxQuantityPerProduct} units";

            if (previous is { } prior && (prior.Max is null || range.Min <= prior.Max))
                return $"Tier {Describe(range)} overlaps or is out of order after tier {Describe(prior)}";

            previous = range;
        }

        return null;
    }

    private static string Describe((int Min, int? Max) range) =>
        range.Max is null ? $"{range.Min}+" : $"{range.Min}-{range.Max}";
}
