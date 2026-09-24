using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// Implementation of ICustomerRepository using Entity Framework Core
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly DefaultContext _context;

    // Work item: TASK-025 (FEAT-011)
    private static readonly SortField[] DefaultOrder = [new("Name", false)];

    /// <summary>
    /// Initializes a new instance of CustomerRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public CustomerRepository(DefaultContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new customer in the database
    /// </summary>
    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    /// <summary>
    /// Retrieves a tracked customer by its unique identifier
    /// </summary>
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Saves the changes made to a tracked customer
    /// </summary>
    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    /// <summary>
    /// Deletes a customer from the database
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await GetByIdAsync(id, cancellationToken);
        if (customer == null)
            return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Work item: BUG-008 (FEAT-010), TASK-025 (FEAT-011)
    /// <summary>
    /// Retrieves one page of customers that match the query's filters, in the query's order or else by name. A page
    /// past the last one, including one whose offset does not fit an int, is empty
    /// </summary>
    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListAsync(ListQuery query, CancellationToken cancellationToken = default)
    {
        var rows = _context.Customers.AsNoTracking()
            .ApplyFilters(query.Filters)
            .ApplyOrder(query.Order, DefaultOrder);
        var totalCount = await rows.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.Size;
        if (offset >= totalCount)
            return ([], totalCount);

        var items = await rows.Skip((int)offset).Take(query.Size).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
