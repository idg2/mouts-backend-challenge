using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010), TASK-029 (FEAT-004), TASK-064 (FEAT-001)
/// <summary>
/// Handler for processing UpdateSaleCommand requests.
/// </summary>
/// <remarks>
/// Copied values are refreshed only when their reference changes: the customer or branch name when its id
/// changes, and the product description and unit price for new items or items whose product changes. The
/// sale number and date never change. Discounts are recalculated from the policies in effect at the stored sale date,
/// never the current date; a cancelled item keeps the values it was last priced with. The sale and its items are saved
/// in a single SaveChangesAsync. After the save it enqueues SaleModified, then ItemCancelled for each existing item
/// that went from active to cancelled (in incoming order), then SaleCancelled when the sale went from active to cancelled.
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

    // Work item: TASK-064 (FEAT-001)
    private readonly DiscountPolicyResolver _discountPolicyResolver;

    // Work item: TASK-029 (FEAT-004), TASK-064 (FEAT-001)
    /// <summary>
    /// Initializes a new instance of UpdateSaleHandler.
    /// </summary>
    /// <param name="saleRepository">The sale repository</param>
    /// <param name="customerRepository">The customer repository</param>
    /// <param name="branchRepository">The branch repository</param>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="outbox">The outbox the sale events are recorded in</param>
    /// <param name="discountPolicyResolver">Picks the discount policy of each product at the sale date</param>
    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        IMapper mapper,
        IOutbox outbox,
        DiscountPolicyResolver discountPolicyResolver)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _mapper = mapper;
        _outbox = outbox;
        _discountPolicyResolver = discountPolicyResolver;
    }

    // Work item: TASK-029 (FEAT-004), TASK-053 (FEAT-017), TASK-064 (FEAT-001), TD-039, TD-043
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
        StepTrace.Step("SAL-UPD-02", "CMN-PIP-10", "Validate the command, item ids unique", [("saleId", command.Id), ("items", command.Items.Count), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("SAL-UPD-03", "Load the sale with its items", [("saleId", command.Id)]);
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        StepTrace.Step("SAL-UPD-04", "Sale found?", [("saleId", command.Id), ("found", sale != null), ("items", sale?.Items.Count)]);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        var wasCancelled = sale.IsCancelled;
        var activeItemIds = sale.Items.Where(item => !item.IsCancelled).Select(item => item.Id).ToHashSet();
        StepTrace.Step("SAL-UPD-05", "Remember the cancelled state and the active item ids", [("saleId", sale.Id), ("wasCancelled", wasCancelled), ("activeItems", activeItemIds.Count), ("items", sale.Items.Count)]);

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

        StepTrace.Step("SAL-UPD-06", "Every item id belongs to the sale?", [("existing", existingItems.Count), ("incomingWithId", command.Items.Count(item => item.Id.HasValue)), ("failures", failures.Count)]);
        if (failures.Count > 0)
            throw new ValidationException(failures);

        // An existing item sent as cancelled takes only the flag (Sale.SyncItems): its stored product stays, so the
        // catalog is not read for it and the discount check and ItemCancelled use the stored product.
        bool KeepsStoredValues(UpdateSaleItemInput item) => item.Id.HasValue && item.IsCancelled;

        Guid EffectiveProductId(UpdateSaleItemInput item) =>
            KeepsStoredValues(item) ? existingItems[item.Id!.Value].ProductId : item.ProductId;

        bool CopiesFromCatalog(UpdateSaleItemInput item) =>
            !item.Id.HasValue || (!KeepsStoredValues(item) && existingItems[item.Id.Value].ProductId != item.ProductId);

        var productIds = command.Items.Where(CopiesFromCatalog).Select(item => item.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _productRepository.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);
        StepTrace.Step("SAL-UPD-07", "Load products of new items and changed products", [("productIds", productIds.Count), ("productsFound", products.Count)]);

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

        StepTrace.Step("SAL-UPD-08", "Changed customer, branch, and products exist?", [("customerChanged", command.CustomerId != sale.CustomerId), ("customerFound", customer != null), ("branchChanged", command.BranchId != sale.BranchId), ("branchFound", branch != null), ("failures", failures.Count)]);
        if (failures.Count > 0)
            throw new ValidationException(failures);

        // The stored sale date, never the current one: a later policy never reprices an older sale.
        var policyProductIds = command.Items.Select(EffectiveProductId).Distinct().ToList();
        var policies = await _discountPolicyResolver.ResolveAsync(command.BranchId, policyProductIds, sale.SaleDate, cancellationToken);
        StepTrace.Step("SAL-UPD-18", "Resolve the discount policies at the sale date",
            [("saleId", sale.Id), ("branchId", command.BranchId), ("saleDate", sale.SaleDate), ("productIds", policyProductIds.Count), ("resolved", policies.Count)]);

        var lines = command.Items
            .Select(item => new SaleDiscountLine(EffectiveProductId(item), item.Quantity, item.DiscountPercentage, item.IsCancelled))
            .ToList();
        var discountFailures = SaleDiscountRules.Check(lines, command.BranchId, sale.SaleDate, policies);
        StepTrace.Step("SAL-UPD-19", "Discount rules respected?", [("saleId", sale.Id), ("lines", lines.Count), ("failures", discountFailures.Count)]);
        if (discountFailures.Count > 0)
            throw new ValidationException(discountFailures);

        if (customer != null)
            sale.ChangeCustomer(customer.Id, customer.Name);

        if (branch != null)
            sale.ChangeBranch(branch.Id, branch.Name);

        sale.SetCancelled(command.IsCancelled);

        var incomingLines = new List<SaleLine>();
        foreach (var item in command.Items)
        {
            var keptCopy = CopiesFromCatalog(item) ? null : existingItems[item.Id!.Value];
            incomingLines.Add(new SaleLine(
                item.Id,
                item.ProductId,
                keptCopy?.ProductDescription ?? products[item.ProductId].Description,
                keptCopy?.UnitPrice ?? products[item.ProductId].UnitPrice,
                item.Quantity,
                item.DiscountPercentage,
                item.IsCancelled));
        }
        StepTrace.Step("SAL-UPD-09", "Apply header values and copy new names", [("saleId", sale.Id), ("customerName", sale.CustomerName), ("branchName", sale.BranchName), ("isCancelled", sale.IsCancelled), ("incoming", incomingLines.Count)]);

        sale.SyncItems(incomingLines);

        sale.ApplyDiscounts(policies);
        StepTrace.Step("SAL-UPD-20", "Apply the discounts per product total", [("saleId", sale.Id), ("items", sale.Items.Count), ("totalAmount", sale.TotalAmount)]);

        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);
        StepTrace.Step("SAL-UPD-11", "Save the sale and its items", [("saleId", updatedSale.Id), ("saleNumber", updatedSale.SaleNumber), ("items", updatedSale.Items.Count)]);
        await _outbox.EnqueueAsync(new SaleModified(SaleSnapshot.From(updatedSale)), cancellationToken);
        StepTrace.Step("SAL-UPD-12", "Enqueue SaleModified", [("saleId", updatedSale.Id), ("eventType", nameof(SaleModified))]);
        foreach (var item in command.Items.Where(item => item.IsCancelled && item.Id.HasValue && activeItemIds.Contains(item.Id.Value)))
        {
            var productId = EffectiveProductId(item);
            await _outbox.EnqueueAsync(new ItemCancelled(updatedSale.Id, item.Id!.Value, productId), cancellationToken);
            StepTrace.Step("SAL-UPD-13", "Enqueue ItemCancelled per item turned cancelled", [("saleId", updatedSale.Id), ("itemId", item.Id!.Value), ("productId", productId), ("eventType", nameof(ItemCancelled))]);
        }
        StepTrace.Step("SAL-UPD-14", "Sale turned cancelled?", [("saleId", updatedSale.Id), ("wasCancelled", wasCancelled), ("isCancelled", updatedSale.IsCancelled)]);
        if (!wasCancelled && updatedSale.IsCancelled)
        {
            await _outbox.EnqueueAsync(new SaleCancelled(updatedSale.Id), cancellationToken);
            StepTrace.Step("SAL-UPD-15", "Enqueue SaleCancelled", [("saleId", updatedSale.Id), ("eventType", nameof(SaleCancelled))]);
        }

        return _mapper.Map<SaleResult>(updatedSale);
    }

    private static ValidationFailure ReferenceNotFound(string propertyName, string message) =>
        new(propertyName, message) { ErrorCode = "ReferenceNotFound" };
}
