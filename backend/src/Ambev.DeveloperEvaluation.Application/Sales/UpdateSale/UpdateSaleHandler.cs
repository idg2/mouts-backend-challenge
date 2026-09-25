using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010), TASK-029 (FEAT-004)
/// <summary>
/// Handler for processing UpdateSaleCommand requests.
/// </summary>
/// <remarks>
/// Copied values are refreshed only when their reference changes: the customer or branch name when its id
/// changes, and the product description and unit price for new items or items whose product changes. The
/// sale number and date never change. The sale and its items are saved in a single SaveChangesAsync.
/// After the save it enqueues SaleModified, then ItemCancelled for each existing item that went from active to cancelled
/// (in incoming order), then SaleCancelled when the sale went from active to cancelled.
/// </remarks>
public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    // Work item: TASK-029 (FEAT-004)
    private readonly IOutbox _outbox;

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Initializes a new instance of UpdateSaleHandler.
    /// </summary>
    /// <param name="saleRepository">The sale repository</param>
    /// <param name="customerRepository">The customer repository</param>
    /// <param name="branchRepository">The branch repository</param>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="outbox">The outbox the sale events are recorded in</param>
    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        IMapper mapper,
        IOutbox outbox)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _mapper = mapper;
        _outbox = outbox;
    }

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Handles the UpdateSaleCommand request.
    /// </summary>
    /// <param name="command">The UpdateSale command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated sale with its items</returns>
    public async Task<SaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        var wasCancelled = sale.IsCancelled;
        var activeItemIds = sale.Items.Where(item => !item.IsCancelled).Select(item => item.Id).ToHashSet();

        var existingItems = sale.Items.ToDictionary(item => item.Id);
        var failures = new List<ValidationFailure>();
        for (var index = 0; index < command.Items.Count; index++)
        {
            var itemId = command.Items[index].Id;
            if (itemId.HasValue && !existingItems.ContainsKey(itemId.Value))
                failures.Add(new ValidationFailure($"Items[{index}].Id", $"Item {itemId} does not belong to sale {sale.Id}")
                {
                    ErrorCode = "ItemNotInSale"
                });
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        bool CopiesFromCatalog(UpdateSaleItemInput item) =>
            !item.Id.HasValue || existingItems[item.Id.Value].ProductId != item.ProductId;

        var productIds = command.Items.Where(CopiesFromCatalog).Select(item => item.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _productRepository.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);

        Customer? customer = null;
        if (command.CustomerId != sale.CustomerId)
        {
            customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
            if (customer == null)
                failures.Add(ReferenceNotFound(nameof(command.CustomerId), $"Customer {command.CustomerId} not found"));
        }

        Branch? branch = null;
        if (command.BranchId != sale.BranchId)
        {
            branch = await _branchRepository.GetByIdAsync(command.BranchId, cancellationToken);
            if (branch == null)
                failures.Add(ReferenceNotFound(nameof(command.BranchId), $"Branch {command.BranchId} not found"));
        }

        for (var index = 0; index < command.Items.Count; index++)
        {
            var item = command.Items[index];
            if (CopiesFromCatalog(item) && !products.ContainsKey(item.ProductId))
                failures.Add(ReferenceNotFound($"Items[{index}].ProductId", $"Product {item.ProductId} not found"));
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        if (customer != null)
        {
            sale.CustomerId = customer.Id;
            sale.CustomerName = customer.Name;
        }

        if (branch != null)
        {
            sale.BranchId = branch.Id;
            sale.BranchName = branch.Name;
        }

        sale.TotalAmount = command.TotalAmount;
        sale.IsCancelled = command.IsCancelled;

        var incomingItems = new List<SaleItem>();
        foreach (var item in command.Items)
        {
            var keptCopy = CopiesFromCatalog(item) ? null : existingItems[item.Id!.Value];
            incomingItems.Add(new SaleItem
            {
                Id = item.Id ?? Guid.Empty,
                ProductId = item.ProductId,
                ProductDescription = keptCopy?.ProductDescription ?? products[item.ProductId].Description,
                UnitPrice = keptCopy?.UnitPrice ?? products[item.ProductId].UnitPrice,
                Quantity = item.Quantity,
                DiscountPercentage = item.DiscountPercentage,
                DiscountAmount = item.DiscountAmount,
                TotalAmount = item.TotalAmount,
                IsCancelled = item.IsCancelled
            });
        }

        sale.SyncItems(incomingItems);

        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);
        await _outbox.EnqueueAsync(new SaleModified(SaleSnapshot.From(updatedSale)), cancellationToken);
        foreach (var item in command.Items.Where(item => item.IsCancelled && item.Id.HasValue && activeItemIds.Contains(item.Id.Value)))
            await _outbox.EnqueueAsync(new ItemCancelled(updatedSale.Id, item.Id!.Value, item.ProductId), cancellationToken);
        if (!wasCancelled && updatedSale.IsCancelled)
            await _outbox.EnqueueAsync(new SaleCancelled(updatedSale.Id), cancellationToken);

        return _mapper.Map<SaleResult>(updatedSale);
    }

    private static ValidationFailure ReferenceNotFound(string propertyName, string message) =>
        new(propertyName, message) { ErrorCode = "ReferenceNotFound" };
}
