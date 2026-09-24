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

    /// <summary>
    /// Handles the DeleteProductCommand request.
    /// </summary>
    /// <param name="request">The DeleteProduct command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the delete operation</returns>
    public async Task<DeleteProductResult> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteProductValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var success = await _productRepository.DeleteAsync(request.Id, cancellationToken);
        if (!success)
            throw new KeyNotFoundException($"Product with ID {request.Id} not found");

        return new DeleteProductResult { Success = true };
    }
}
