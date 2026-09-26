using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
using Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.Application.Products.GetProduct;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.GetProduct;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;
using Ambev.DeveloperEvaluation.WebApi.Features.Products.UpdateProduct;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Controller for managing product operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : BaseController
{
    private const string WriteRoles = "Admin,Manager";

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of ProductsController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public ProductsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    // Work item: TASK-051 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="request">The product creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created product details</returns>
    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateProductRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("PRD-CRT-01", "CMN-PIP-04", "Validate the request", [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("code", request.Code)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<CreateProductCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(CreateProductCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("PRD-CRT-06", "CMN-PIP-06", "201 with id, code, description, and unit price", [("id", response.Id), ("code", response.Code)]);

        return Created(string.Empty, new ApiResponseWithData<CreateProductResponse>
        {
            Success = true,
            Message = "Product created successfully",
            Data = _mapper.Map<CreateProductResponse>(response)
        });
    }

    // Work item: TASK-051 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Retrieves a product by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the product</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product details if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetProductRequest { Id = id };
        var validator = new GetProductRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("PRD-GET-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<GetProductCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(GetProductCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("PRD-GET-04", "CMN-PIP-06", "200 with the product", [("id", response.Id)]);

        return Ok(new ApiResponseWithData<GetProductResponse>
        {
            Success = true,
            Message = "Product retrieved successfully",
            Data = _mapper.Map<GetProductResponse>(response)
        });
    }

    // Work item: TASK-027 (FEAT-011), TASK-051 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Lists products one page at a time. Any response field (id, code, description, unitPrice) can be a query key: text matches ignore
    /// case and accept '*' at the start or end, and a repeated key matches any of its values. Numeric and date fields
    /// also accept _min and _max prefixed keys. _order sorts by response fields, for example "unitPrice desc"; without it
    /// the list is ordered by description
    /// </summary>
    /// <param name="page">The page number, starting at 1 (query parameter _page)</param>
    /// <param name="size">The page size, from 1 to 100 (query parameter _size)</param>
    /// <param name="order">Comma-separated response fields, each optionally followed by asc or desc (query parameter _order)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ListProductsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListProducts(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var (filters, sortFields) = ListQueryParser.Parse<ListProductsResponse>(Request.Query);
        StepTrace.Step("PRD-LST-01", "Parse filters and order, see CMN-LST", [("filters", filters.Count), ("order", sortFields.Count)]);

        var request = new ListProductsRequest { Page = page, Size = size };
        var validator = new ListProductsRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("PRD-LST-02", "CMN-LST-08", "Validate _page and _size", [("page", request.Page), ("size", request.Size), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<ListProductsCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(ListProductsCommand))]);
        command.Filters = filters;
        command.Order = sortFields;
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CMN-PIP-06", "Send the command", [("total", response.TotalCount)]);

        var products = _mapper.Map<List<ListProductsResponse>>(response.Items);
        StepTrace.Step("PRD-LST-04", "200 with the page", [("count", products.Count), ("total", response.TotalCount), ("page", response.Page), ("size", response.Size)]);
        return OkPaginated(new PaginatedList<ListProductsResponse>(products, response.TotalCount, response.Page, response.Size));
    }

    // Work item: TASK-051 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Updates a product
    /// </summary>
    /// <param name="id">The unique identifier of the product to update</param>
    /// <param name="request">The product update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated product details</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        var validator = new UpdateProductRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("PRD-UPD-01", "CMN-PIP-04", "Validate the request", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("code", request.Code)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<UpdateProductCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(UpdateProductCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("PRD-UPD-08", "CMN-PIP-06", "200 with the product", [("id", response.Id)]);

        return Ok(new ApiResponseWithData<UpdateProductResponse>
        {
            Success = true,
            Message = "Product updated successfully",
            Data = _mapper.Map<UpdateProductResponse>(response)
        });
    }

    // Work item: TASK-051 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Deletes a product by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the product to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if the product was deleted</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteProductRequest { Id = id };
        var validator = new DeleteProductRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("PRD-DEL-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<DeleteProductCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(DeleteProductCommand))]);
        await _mediator.Send(command, cancellationToken);
        StepTrace.Step("PRD-DEL-04", "CMN-PIP-06", "200", [("id", request.Id)]);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Product deleted successfully"
        });
    }
}
