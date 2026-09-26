using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.DeleteCustomer;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Controller for managing customer operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : BaseController
{
    private const string WriteRoles = "Admin,Manager";

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of CustomersController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public CustomersController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    // Work item: TASK-049 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Creates a new customer
    /// </summary>
    /// <param name="request">The customer creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created customer details</returns>
    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateCustomerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateCustomerRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("CUS-CRT-01", "CMN-PIP-04", "Validate the request", [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("document", request.Document)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<CreateCustomerCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(CreateCustomerCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CUS-CRT-06", "CMN-PIP-06", "201 with id, name, and document", [("id", response.Id), ("document", response.Document)]);

        return Created(string.Empty, new ApiResponseWithData<CreateCustomerResponse>
        {
            Success = true,
            Message = "Customer created successfully",
            Data = _mapper.Map<CreateCustomerResponse>(response)
        });
    }

    // Work item: TASK-049 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Retrieves a customer by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the customer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The customer details if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetCustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetCustomerRequest { Id = id };
        var validator = new GetCustomerRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("CUS-GET-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<GetCustomerCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(GetCustomerCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CUS-GET-04", "CMN-PIP-06", "200 with the customer", [("id", response.Id)]);

        return Ok(new ApiResponseWithData<GetCustomerResponse>
        {
            Success = true,
            Message = "Customer retrieved successfully",
            Data = _mapper.Map<GetCustomerResponse>(response)
        });
    }

    // Work item: TASK-027 (FEAT-011), TASK-049 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Lists customers one page at a time. Any response field (id, name) can be a query key: text matches ignore
    /// case and accept '*' at the start or end, and a repeated key matches any of its values. Numeric and date fields
    /// also accept _min and _max prefixed keys. _order sorts by response fields, for example "name desc"; without it
    /// the list is ordered by name
    /// </summary>
    /// <param name="page">The page number, starting at 1 (query parameter _page)</param>
    /// <param name="size">The page size, from 1 to 100 (query parameter _size)</param>
    /// <param name="order">Comma-separated response fields, each optionally followed by asc or desc (query parameter _order)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ListCustomersResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListCustomers(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var (filters, sortFields) = ListQueryParser.Parse<ListCustomersResponse>(Request.Query);
        StepTrace.Step("CUS-LST-01", "Parse filters and order, see CMN-LST", [("filters", filters.Count), ("order", sortFields.Count)]);

        var request = new ListCustomersRequest { Page = page, Size = size };
        var validator = new ListCustomersRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("CUS-LST-02", "CMN-LST-08", "Validate _page and _size", [("page", request.Page), ("size", request.Size), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<ListCustomersCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(ListCustomersCommand))]);
        command.Filters = filters;
        command.Order = sortFields;
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CMN-PIP-06", "Send the command", [("total", response.TotalCount)]);

        var customers = _mapper.Map<List<ListCustomersResponse>>(response.Items);
        StepTrace.Step("CUS-LST-04", "200 with the page", [("count", customers.Count), ("total", response.TotalCount), ("page", response.Page), ("size", response.Size)]);
        return OkPaginated(new PaginatedList<ListCustomersResponse>(customers, response.TotalCount, response.Page, response.Size));
    }

    // Work item: TASK-049 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Updates a customer
    /// </summary>
    /// <param name="id">The unique identifier of the customer to update</param>
    /// <param name="request">The customer update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated customer details</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateCustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer([FromRoute] Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        var validator = new UpdateCustomerRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("CUS-UPD-01", "CMN-PIP-04", "Validate the request", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("document", request.Document)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<UpdateCustomerCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(UpdateCustomerCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CUS-UPD-08", "CMN-PIP-06", "200 with the customer", [("id", response.Id)]);

        return Ok(new ApiResponseWithData<UpdateCustomerResponse>
        {
            Success = true,
            Message = "Customer updated successfully",
            Data = _mapper.Map<UpdateCustomerResponse>(response)
        });
    }

    // Work item: TASK-049 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Deletes a customer by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the customer to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if the customer was deleted</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteCustomerRequest { Id = id };
        var validator = new DeleteCustomerRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("CUS-DEL-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<DeleteCustomerCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(DeleteCustomerCommand))]);
        await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CUS-DEL-04", "CMN-PIP-06", "200", [("id", request.Id)]);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Customer deleted successfully"
        });
    }
}
