using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Controller for managing sale records
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : BaseController
{
    private const string WriteRoles = "Admin,Manager";

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    // Work item: TASK-039 (FEAT-006)
    private readonly IBus _bus;

    // Work item: TASK-039 (FEAT-006)
    /// <summary>
    /// Initializes a new instance of SalesController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="bus">The bus that queues sales sent with Prefer: respond-async</param>
    public SalesController(IMediator mediator, IMapper mapper, IBus bus)
    {
        _mediator = mediator;
        _mapper = mapper;
        _bus = bus;
    }

    // Work item: TASK-039 (FEAT-006)
    /// <summary>
    /// Creates a new sale with its items. With the header Prefer: respond-async the sale is queued instead: the
    /// response is 202 with the id it will be stored under, and GET /api/sales/{id} answers 404 until it is stored
    /// </summary>
    /// <param name="request">The sale creation request</param>
    /// <param name="prefer">The RFC 7240 Prefer header; respond-async queues the sale</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created sale, or the id of the queued sale</returns>
    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleAcceptedResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale(
        [FromBody] CreateSaleRequest request,
        [FromHeader(Name = "Prefer")] string? prefer,
        CancellationToken cancellationToken)
    {
        var validator = new CreateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<CreateSaleCommand>(request);

        if (PreferHeader.RequestsRespondAsync(prefer))
        {
            command.Id = Guid.NewGuid();
            await _bus.SendLocal(command);

            Response.Headers["Preference-Applied"] = PreferHeader.RespondAsync;
            return AcceptedAtAction(nameof(GetSale), new { id = command.Id }, new ApiResponseWithData<SaleAcceptedResponse>
            {
                Success = true,
                Message = "Sale sent for processing",
                Data = new SaleAcceptedResponse { Id = command.Id.Value }
            });
        }

        var response = await _mediator.Send(command, cancellationToken);

        return Created(string.Empty, new ApiResponseWithData<SaleResponse>
        {
            Success = true,
            Message = "Sale created successfully",
            Data = _mapper.Map<SaleResponse>(response)
        });
    }

    /// <summary>
    /// Retrieves a sale with its items by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the sale</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sale with its items if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetSaleRequest { Id = id };
        var validator = new GetSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<GetSaleCommand>(request.Id);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponseWithData<SaleResponse>
        {
            Success = true,
            Message = "Sale retrieved successfully",
            Data = _mapper.Map<SaleResponse>(response)
        });
    }

    // Work item: TASK-027 (FEAT-011)
    /// <summary>
    /// Lists sales one page at a time. Any response field (id, saleNumber, saleDate, customerId, customerName, branchId, branchName, totalAmount, isCancelled) can be a query key: text matches ignore
    /// case and accept '*' at the start or end, and a repeated key matches any of its values. Numeric and date fields
    /// also accept _min and _max prefixed keys. _order sorts by response fields, for example "saleNumber desc"; without it
    /// the list is ordered by sale number
    /// </summary>
    /// <param name="page">The page number, starting at 1 (query parameter _page)</param>
    /// <param name="size">The page size, from 1 to 100 (query parameter _size)</param>
    /// <param name="order">Comma-separated response fields, each optionally followed by asc or desc (query parameter _order)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of sale headers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ListSalesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListSales(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var (filters, sortFields) = ListQueryParser.Parse<ListSalesResponse>(Request.Query);

        var request = new ListSalesRequest { Page = page, Size = size };
        var validator = new ListSalesRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<ListSalesCommand>(request);
        command.Filters = filters;
        command.Order = sortFields;
        var response = await _mediator.Send(command, cancellationToken);

        var sales = _mapper.Map<List<ListSalesResponse>>(response.Items);
        return OkPaginated(new PaginatedList<ListSalesResponse>(sales, response.TotalCount, response.Page, response.Size));
    }

    // Work item: TASK-022 (FEAT-010)
    /// <summary>
    /// Updates a sale and its items; items are matched by id
    /// </summary>
    /// <param name="id">The unique identifier of the sale to update</param>
    /// <param name="request">The sale update request with the complete item list</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated sale with its items</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSale([FromRoute] Guid id, [FromBody] UpdateSaleRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        var validator = new UpdateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<UpdateSaleCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponseWithData<SaleResponse>
        {
            Success = true,
            Message = "Sale updated successfully",
            Data = _mapper.Map<SaleResponse>(response)
        });
    }

    // Work item: TASK-022 (FEAT-010)
    /// <summary>
    /// Deletes a sale and its items by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the sale to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if the sale was deleted</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteSaleRequest { Id = id };
        var validator = new DeleteSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<DeleteSaleCommand>(request.Id);
        await _mediator.Send(command, cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Sale deleted successfully"
        });
    }
}
