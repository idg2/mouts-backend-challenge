using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Handler for processing CreateProductCommand requests.
/// </summary>
public class CreateProductHandler : IRequestHandler<CreateProductCommand, CreateProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of CreateProductHandler.
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public CreateProductHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    // Work item: TASK-020 (FEAT-010), FEAT-013, TASK-051 (FEAT-017)
    /// <summary>
    /// Handles the CreateProductCommand request. The code is stored trimmed and in upper case, and must not be in use.
    /// </summary>
    /// <param name="command">The CreateProduct command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created product</returns>
    public async Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateProductValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        StepTrace.Step("PRD-CRT-02", "CMN-PIP-10", "Validate the command", [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var code = command.Code.Trim().ToUpperInvariant();
        StepTrace.Step("PRD-CRT-03", "Normalize the code", [("normalized", code)]);
        var existing = await _productRepository.GetByCodeAsync(code, cancellationToken);
        StepTrace.Step("PRD-CRT-04", "Code already stored?", [("code", code), ("exists", existing != null)]);
        if (existing != null)
            throw new DuplicateEntryException($"Product with code {code} already exists");

        var product = _mapper.Map<Product>(command);
        product.Code = code;
        var createdProduct = await _productRepository.CreateAsync(product, cancellationToken);
        StepTrace.Step("PRD-CRT-05", "Insert the product", [("id", createdProduct.Id), ("code", createdProduct.Code)]);
        return _mapper.Map<CreateProductResult>(createdProduct);
    }
}
