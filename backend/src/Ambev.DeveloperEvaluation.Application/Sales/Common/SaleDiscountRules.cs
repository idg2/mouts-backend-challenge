using System.Globalization;
using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation.Results;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

// Work item: TASK-064 (FEAT-001)
/// <summary>
/// Checks the lines of a sale against the discount policies resolved for it, before the sale is built, so every
/// violation reaches the client as one 400 entry (A15) instead of a DomainException from <see cref="Sale.ApplyDiscounts"/>.
/// </summary>
public static class SaleDiscountRules
{
    /// <summary>
    /// The error code of a product whose active lines add up to more than its policy maximum.
    /// </summary>
    public const string QuantityLimitExceeded = "QuantityLimitExceeded";

    /// <summary>
    /// The error code of a line that asks for more discount than the tier of its product's total allows.
    /// </summary>
    public const string DiscountAboveAllowed = "DiscountAboveAllowed";

    /// <summary>
    /// The error code of a line whose product has no discount policy in effect at the sale date.
    /// </summary>
    public const string NoDiscountPolicy = "NoDiscountPolicy";

    // Work item: TASK-066 (FEAT-001)
    /// <summary>
    /// Checks the lines. A line whose product has no policy fails with <see cref="NoDiscountPolicy"/>, cancelled or
    /// not, because every stored item points to a policy. The active lines of each product with a policy are summed:
    /// above the maximum every one of them fails with <see cref="QuantityLimitExceeded"/>; otherwise each line whose
    /// requested discount is above the ceiling fails with <see cref="DiscountAboveAllowed"/> (below the first tier the
    /// ceiling is 0). The totals are summed as <see cref="long"/>, so a total above <see cref="int.MaxValue"/> fails with
    /// <see cref="QuantityLimitExceeded"/> instead of overflowing.
    /// </summary>
    /// <param name="lines">The command's lines, in request order</param>
    /// <param name="branchId">The sale's branch, for the message</param>
    /// <param name="saleDate">The sale date, for the message</param>
    /// <param name="policies">The policy per product, from the resolver</param>
    /// <returns>The failures in line order; empty when every rule holds</returns>
    public static IReadOnlyList<ValidationFailure> Check(
        IReadOnlyList<SaleDiscountLine> lines, Guid branchId, DateTime saleDate, IReadOnlyDictionary<Guid, DiscountPolicy> policies)
    {
        var failures = new List<(int Index, ValidationFailure Failure)>();
        for (var index = 0; index < lines.Count; index++)
        {
            var productId = lines[index].ProductId;
            if (!policies.ContainsKey(productId))
                failures.Add((index, Failure($"Items[{index}].ProductId", NoDiscountPolicy,
                    $"No discount policy in effect for product {productId} at branch {branchId} on {saleDate.ToString("O", CultureInfo.InvariantCulture)}")));
        }

        var groups = lines
            .Select((line, index) => (Line: line, Index: index))
            .Where(entry => !entry.Line.IsCancelled && policies.ContainsKey(entry.Line.ProductId))
            .GroupBy(entry => entry.Line.ProductId);
        foreach (var group in groups)
        {
            var total = group.Sum(entry => (long)entry.Line.Quantity);
            var decision = policies[group.Key].Evaluate((int)Math.Min(total, int.MaxValue));
            if (total > int.MaxValue || !decision.IsAllowed)
            {
                foreach (var entry in group)
                    failures.Add((entry.Index, Failure($"Items[{entry.Index}].Quantity", QuantityLimitExceeded,
                        $"Total of {total} units for product {group.Key} exceeds maximum of {decision.MaxQuantity}")));
                continue;
            }

            foreach (var entry in group.Where(entry => entry.Line.RequestedDiscountPercentage > decision.CeilingPercentage))
                failures.Add((entry.Index, Failure($"Items[{entry.Index}].DiscountPercentage", DiscountAboveAllowed,
                    $"Requested discount {Percent(entry.Line.RequestedDiscountPercentage!.Value)}% exceeds {Percent(decision.CeilingPercentage)}% allowed for {total} units")));
        }

        return failures.OrderBy(entry => entry.Index).Select(entry => entry.Failure).ToList();
    }

    private static ValidationFailure Failure(string propertyName, string errorCode, string message) =>
        new(propertyName, message) { ErrorCode = errorCode };

    private static string Percent(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
