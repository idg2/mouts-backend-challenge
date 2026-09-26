using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Command for retrieving one page of sales ordered by sale number, without their items.
/// </summary>
public class ListSalesCommand : IRequest<ListSalesResult>
{
    /// <summary>
    /// Gets or sets the page number, starting at 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size, from 1 to 100.
    /// </summary>
    public int Size { get; set; } = 10;

    // Work item: TASK-025 (FEAT-011)
    /// <summary>
    /// Gets or sets the filters; repeated matches on one field are combined with OR, everything else with AND.
    /// </summary>
    public IReadOnlyList<FieldFilter> Filters { get; set; } = [];

    // Work item: TASK-025 (FEAT-011)
    /// <summary>
    /// Gets or sets the sort fields; empty keeps the default order.
    /// </summary>
    public IReadOnlyList<SortField> Order { get; set; } = [];
}
