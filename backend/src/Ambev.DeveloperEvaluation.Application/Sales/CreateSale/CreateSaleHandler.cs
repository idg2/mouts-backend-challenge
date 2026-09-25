using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Handler for processing CreateSaleCommand requests.
/// </summary>
/// <remarks>
/// Customer, branch, and products are external identities: the handler loads them to reject unknown ids and
/// copies the customer name, branch name, product description, and unit price into the sale. Discount and
/// total values are stored as received; nothing is calculated.
/// </remarks>
public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;

    // Work item: TASK-029 (FEAT-004)
    private readonly IOutbox _outbox;

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Initializes a new instance of CreateSaleHandler.
    /// </summary>
    /// <param name="saleRepository">The sale repository</param>
    /// <param name="customerRepository">The customer repository</param>
    /// <param name="branchRepository">The branch repository</param>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="timeProvider">The source of the sale date</param>
    /// <param name="outbox">The outbox the sale events are recorded in</param>
    public CreateSaleHandler(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        IMapper mapper,
        TimeProvider timeProvider,
        IOutbox outbox)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _mapper = mapper;
        _timeProvider = timeProvider;
        _outbox = outbox;
    }

    // Work item: TD-010 (FEAT-010), TASK-037 (FEAT-006), TASK-029 (FEAT-004), TASK-052 (FEAT-017)
    /// <summary>
    /// Handles the CreateSaleCommand request.
    /// </summary>
    /// <param name="command">The CreateSale command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created sale with its items and sale number</returns>
    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        StepTrace.Step("SAL-CRT-04", "CMN-PIP-10", "Validate the command", [("presetId", command.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        if (command.Id is Guid presetId)
        {
            var stored = await _saleRepository.GetByIdAsync(presetId, cancellationToken);
            StepTrace.Step("SAL-CRT-05", "Preset id already stored?", [("saleId", presetId), ("stored", stored != null)]);
            if (stored != null)
            {
                StepTrace.Step("SAL-CRT-06", "Return the stored sale, no new event", [("saleId", stored.Id), ("saleNumber", stored.SaleNumber)]);
                return _mapper.Map<SaleResult>(stored);
            }
        }

        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        var branch = await _branchRepository.GetByIdAsync(command.BranchId, cancellationToken);
        var productIds = command.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(product => product.Id);
        StepTrace.Step("SAL-CRT-07", "Load customer, branch, and products", [("customerFound", customer != null), ("branchFound", branch != null), ("productIds", productIds.Count), ("productsFound", products.Count)]);

        var failures = new List<ValidationFailure>();
        if (customer == null)
            failures.Add(ReferenceNotFound(nameof(command.CustomerId), $"Customer {command.CustomerId} not found"));
        if (branch == null)
            failures.Add(ReferenceNotFound(nameof(command.BranchId), $"Branch {command.BranchId} not found"));
        for (var index = 0; index < command.Items.Count; index++)
        {
            var productId = command.Items[index].ProductId;
            if (!products.ContainsKey(productId))
                failures.Add(ReferenceNotFound($"Items[{index}].ProductId", $"Product {productId} not found"));
        }

        StepTrace.Step("SAL-CRT-08", "All references exist?", [("failures", failures.Count)]);
        if (customer == null || branch == null || failures.Count > 0)
            throw new ValidationException(failures);

        // PostgreSQL stores microseconds: truncating here keeps the SaleCreated snapshot and the response equal to
        // what a later read returns.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var saleDate = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));

        var sale = new Sale
        {
            // Guid.Empty leaves the id to the column default (gen_random_uuid()).
            Id = command.Id ?? Guid.Empty,
            SaleDate = saleDate,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            BranchId = branch.Id,
            BranchName = branch.Name,
            TotalAmount = command.TotalAmount,
            IsCancelled = false,
            Items = command.Items.Select((item, index) => new SaleItem
            {
                LineNumber = index + 1,
                ProductId = item.ProductId,
                ProductDescription = products[item.ProductId].Description,
                UnitPrice = products[item.ProductId].UnitPrice,
                Quantity = item.Quantity,
                DiscountPercentage = item.DiscountPercentage,
                DiscountAmount = item.DiscountAmount,
                TotalAmount = item.TotalAmount,
                IsCancelled = false
            }).ToList()
        };
        StepTrace.Step("SAL-CRT-09", "Build the sale with copied names and prices", [("presetId", command.Id), ("saleDate", sale.SaleDate), ("customerName", sale.CustomerName), ("branchName", sale.BranchName), ("items", sale.Items.Count), ("totalAmount", sale.TotalAmount)]);

        var createdSale = await _saleRepository.CreateAsync(sale, cancellationToken);
        StepTrace.Step("SAL-CRT-10", "Insert the sale and its items", [("saleId", createdSale.Id), ("saleNumber", createdSale.SaleNumber), ("items", createdSale.Items.Count)]);
        await _outbox.EnqueueAsync(new SaleCreated(SaleSnapshot.From(createdSale)), cancellationToken);
        StepTrace.Step("SAL-CRT-11", "Enqueue SaleCreated", [("saleId", createdSale.Id), ("saleNumber", createdSale.SaleNumber), ("eventType", nameof(SaleCreated))]);
        return _mapper.Map<SaleResult>(createdSale);
    }

    private static ValidationFailure ReferenceNotFound(string propertyName, string message) =>
        new(propertyName, message) { ErrorCode = "ReferenceNotFound" };
}
