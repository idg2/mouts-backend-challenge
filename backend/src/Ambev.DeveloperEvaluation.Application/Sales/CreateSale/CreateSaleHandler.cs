using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
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

    /// <summary>
    /// Initializes a new instance of CreateSaleHandler.
    /// </summary>
    /// <param name="saleRepository">The sale repository</param>
    /// <param name="customerRepository">The customer repository</param>
    /// <param name="branchRepository">The branch repository</param>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="timeProvider">The source of the sale date</param>
    public CreateSaleHandler(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        IMapper mapper,
        TimeProvider timeProvider)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _mapper = mapper;
        _timeProvider = timeProvider;
    }

    // Work item: TD-010 (FEAT-010), TASK-037 (FEAT-006)
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

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        if (command.Id is Guid presetId)
        {
            var stored = await _saleRepository.GetByIdAsync(presetId, cancellationToken);
            if (stored != null)
                return _mapper.Map<SaleResult>(stored);
        }

        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        var branch = await _branchRepository.GetByIdAsync(command.BranchId, cancellationToken);
        var productIds = command.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(product => product.Id);

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

        if (customer == null || branch == null || failures.Count > 0)
            throw new ValidationException(failures);

        var sale = new Sale
        {
            // Guid.Empty leaves the id to the column default (gen_random_uuid()).
            Id = command.Id ?? Guid.Empty,
            SaleDate = _timeProvider.GetUtcNow().UtcDateTime,
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

        var createdSale = await _saleRepository.CreateAsync(sale, cancellationToken);
        return _mapper.Map<SaleResult>(createdSale);
    }

    private static ValidationFailure ReferenceNotFound(string propertyName, string message) =>
        new(propertyName, message) { ErrorCode = "ReferenceNotFound" };
}
