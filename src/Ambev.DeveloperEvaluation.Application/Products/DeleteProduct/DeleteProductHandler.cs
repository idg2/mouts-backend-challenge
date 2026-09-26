using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Handler for processing DeleteProductCommand requests.
/// </summary>
public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, DeleteProductResult>
{
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Initializes a new instance of DeleteProductHandler.
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    public DeleteProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // Work item: TASK-051 (FEAT-017)
    /// <summary>
    /// Handles the DeleteProductCommand request.
    /// </summary>
    /// <param name="request">The DeleteProduct command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the delete operation</returns>
    public async Task<DeleteProductResult> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(DeleteProductCommand)), ("id", request.Id)]);
        var validator = new DeleteProductValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        StepTrace.Step("PRD-DEL-02", "Delete the product", [("id", request.Id)]);
        var success = await _productRepository.DeleteAsync(request.Id, cancellationToken);
        StepTrace.Step("PRD-DEL-03", "Product existed?", [("id", request.Id), ("existed", success)]);
        if (!success)
            throw new KeyNotFoundException($"Product with ID {request.Id} not found");

        return new DeleteProductResult { Success = true };
    }
}
