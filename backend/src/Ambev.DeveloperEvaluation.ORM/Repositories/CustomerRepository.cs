using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    // Work item: TASK-016 (FEAT-010), FEAT-012
    /// <summary>
    /// Creates a new customer in the database. A document already stored raises <see cref="DuplicateEntryException"/>
    /// </summary>
    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
        await SaveChangesAsync(customer, cancellationToken);
        return customer;
    }

    /// <summary>
    /// Retrieves a tracked customer by its unique identifier
    /// </summary>
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    // Work item: FEAT-012
    /// <summary>
    /// Retrieves a customer by its normalized document, without tracking it
    /// </summary>
    public async Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Document == document, cancellationToken);
    }

    // Work item: TASK-016 (FEAT-010), FEAT-012
    /// <summary>
    /// Saves the changes made to a tracked customer. A document used by another customer raises
    /// <see cref="DuplicateEntryException"/>
    /// </summary>
    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(customer, cancellationToken);
        return customer;
    }

    // Work item: FEAT-012
    /// <summary>
    /// Saves the pending changes and turns a violation of the unique document index into <see cref="DuplicateEntryException"/>
    /// </summary>
    private async Task SaveChangesAsync(Customer customer, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation,
                                               ConstraintName: "IX_Customers_Document"
                                           })
        {
            throw new DuplicateEntryException($"Customer with document {customer.Document} already exists");
        }
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
