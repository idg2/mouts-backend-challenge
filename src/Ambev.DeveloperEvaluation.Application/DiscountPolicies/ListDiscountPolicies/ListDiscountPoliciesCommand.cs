using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Command for retrieving one page of discount policies.
/// </summary>
public class ListDiscountPoliciesCommand : IRequest<ListDiscountPoliciesResult>
{
    /// <summary>
    /// Gets or sets the page number, starting at 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size, from 1 to 100.
    /// </summary>
    public int Size { get; set; } = 10;

    /// <summary>
    /// Gets or sets the filters; repeated matches on one field are combined with OR, everything else with AND.
    /// </summary>
    public IReadOnlyList<FieldFilter> Filters { get; set; } = [];

    /// <summary>
    /// Gets or sets the sort fields; empty keeps the default order (start date, then id).
    /// </summary>
    public IReadOnlyList<SortField> Order { get; set; } = [];

    // Work item: TD-032
    /// <summary>
    /// Gets or sets whether disabled policies are listed too; false hides them.
    /// </summary>
    public bool IncludeDisabled { get; set; }
}
