using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Handler for processing UpdateProductCommand requests.
/// </summary>
public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, UpdateProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of UpdateProductHandler.
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public UpdateProductHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    // Work item: TASK-020 (FEAT-010), FEAT-013, TASK-051 (FEAT-017)
    /// <summary>
    /// Handles the UpdateProductCommand request. The code is stored trimmed and in upper case, and must not be used by
    /// another product.
    /// </summary>
    /// <param name="command">The UpdateProduct command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated product</returns>
    public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateProductValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        StepTrace.Step("PRD-UPD-02", "CMN-PIP-10", "Validate the command", [("id", command.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("PRD-UPD-03", "Load the product", [("id", command.Id)]);
        var product = await _productRepository.GetByIdAsync(command.Id, cancellationToken);
        StepTrace.Step("PRD-UPD-04", "Product found?", [("id", command.Id), ("found", product != null)]);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {command.Id} not found");

        var code = command.Code.Trim().ToUpperInvariant();
        StepTrace.Step("PRD-UPD-05", "Normalize the code", [("normalized", code)]);
        var productWithCode = await _productRepository.GetByCodeAsync(code, cancellationToken);
        StepTrace.Step("PRD-UPD-06", "Code used by another product?", [("code", code), ("usedByOther", productWithCode != null && productWithCode.Id != product.Id), ("otherId", productWithCode?.Id)]);
        if (productWithCode != null && productWithCode.Id != product.Id)
            throw new DuplicateEntryException($"Product with code {code} already exists");

        product.Code = code;
        product.Description = command.Description;
        product.UnitPrice = command.UnitPrice;

        var updatedProduct = await _productRepository.UpdateAsync(product, cancellationToken);
        StepTrace.Step("PRD-UPD-07", "Save the product", [("id", updatedProduct.Id), ("code", updatedProduct.Code)]);
        return _mapper.Map<UpdateProductResult>(updatedProduct);
    }
}
