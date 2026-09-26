using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Handler for processing ListProductsCommand requests.
/// </summary>
public class ListProductsHandler : IRequestHandler<ListProductsCommand, ListProductsResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of ListProductsHandler.
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public ListProductsHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    // Work item: TASK-025 (FEAT-011), TASK-051 (FEAT-017)
    /// <summary>
    /// Handles the ListProductsCommand request.
    /// </summary>
    /// <param name="command">The ListProducts command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of products and the total count</returns>
    public async Task<ListProductsResult> Handle(ListProductsCommand command, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-10", "Handler validates the command and runs the use case", [("request", nameof(ListProductsCommand)), ("page", command.Page), ("size", command.Size)]);
        var validator = new ListProductsValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var query = new ListQuery
        {
            Page = command.Page,
            Size = command.Size,
            Filters = command.Filters,
            Order = command.Order
        };
        var (products, totalCount) = await _productRepository.ListAsync(query, cancellationToken);
        StepTrace.Step("PRD-LST-03", "Query one page", [("page", query.Page), ("size", query.Size), ("filters", query.Filters.Count), ("order", query.Order.Count), ("count", products.Count), ("total", totalCount)]);

        return new ListProductsResult
        {
            Items = _mapper.Map<List<ListProductsItem>>(products),
            TotalCount = totalCount,
            Page = command.Page,
            Size = command.Size
        };
    }
}
