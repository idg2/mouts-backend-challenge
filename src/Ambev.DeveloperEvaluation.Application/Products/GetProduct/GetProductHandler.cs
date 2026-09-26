using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Handler for processing GetProductCommand requests.
/// </summary>
public class GetProductHandler : IRequestHandler<GetProductCommand, GetProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of GetProductHandler.
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public GetProductHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    // Work item: TASK-051 (FEAT-017)
    /// <summary>
    /// Handles the GetProductCommand request.
    /// </summary>
    /// <param name="request">The GetProduct command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product details if found</returns>
    public async Task<GetProductResult> Handle(GetProductCommand request, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(GetProductCommand)), ("id", request.Id)]);
        var validator = new GetProductValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("PRD-GET-02", "Load the product", [("id", request.Id)]);
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        StepTrace.Step("PRD-GET-03", "Product found?", [("id", request.Id), ("found", product != null)]);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {request.Id} not found");

        return _mapper.Map<GetProductResult>(product);
    }
}
