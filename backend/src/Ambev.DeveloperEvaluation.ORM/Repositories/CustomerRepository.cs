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

    /// <summary>
    /// Retrieves one page of customers ordered by name
    /// </summary>
    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking().OrderBy(c => c.Name).ThenBy(c => c.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
