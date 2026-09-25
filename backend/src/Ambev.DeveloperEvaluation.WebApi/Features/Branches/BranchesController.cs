using Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;
using Ambev.DeveloperEvaluation.Application.Branches.GetBranch;
using Ambev.DeveloperEvaluation.Application.Branches.ListBranches;
using Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches.DeleteBranch;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches.GetBranch;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches.ListBranches;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches.UpdateBranch;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Controller for managing branch operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : BaseController
{
    private const string WriteRoles = "Admin,Manager";

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of BranchesController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public BranchesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    // Work item: TASK-050 (FEAT-017)
    /// <summary>
    /// Creates a new branch
    /// </summary>
    /// <param name="request">The branch creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created branch details</returns>
    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateBranchResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateBranchRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("BRN-CRT-01", "CMN-PIP-04", "Validate the request", [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<CreateBranchCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(CreateBranchCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("BRN-CRT-04", "CMN-PIP-06", "201 with id and name", [("id", response.Id)]);

        return Created(string.Empty, new ApiResponseWithData<CreateBranchResponse>
        {
            Success = true,
            Message = "Branch created successfully",
            Data = _mapper.Map<CreateBranchResponse>(response)
        });
    }

    // Work item: TASK-050 (FEAT-017)
    /// <summary>
    /// Retrieves a branch by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the branch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The branch details if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetBranchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranch([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetBranchRequest { Id = id };
        var validator = new GetBranchRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("BRN-GET-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<GetBranchCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(GetBranchCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("BRN-GET-04", "CMN-PIP-06", "200 with the branch", [("id", response.Id)]);

        return Ok(new ApiResponseWithData<GetBranchResponse>
        {
            Success = true,
            Message = "Branch retrieved successfully",
            Data = _mapper.Map<GetBranchResponse>(response)
        });
    }

    // Work item: TASK-027 (FEAT-011), TASK-050 (FEAT-017)
    /// <summary>
    /// Lists branches one page at a time. Any response field (id, name) can be a query key: text matches ignore
    /// case and accept '*' at the start or end, and a repeated key matches any of its values. Numeric and date fields
    /// also accept _min and _max prefixed keys. _order sorts by response fields, for example "name desc"; without it
    /// the list is ordered by name
    /// </summary>
    /// <param name="page">The page number, starting at 1 (query parameter _page)</param>
    /// <param name="size">The page size, from 1 to 100 (query parameter _size)</param>
    /// <param name="order">Comma-separated response fields, each optionally followed by asc or desc (query parameter _order)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of branches</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ListBranchesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListBranches(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var (filters, sortFields) = ListQueryParser.Parse<ListBranchesResponse>(Request.Query);
        StepTrace.Step("BRN-LST-01", "Parse filters and order, see CMN-LST", [("filters", filters.Count), ("order", sortFields.Count)]);

        var request = new ListBranchesRequest { Page = page, Size = size };
        var validator = new ListBranchesRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("BRN-LST-02", "CMN-LST-08", "Validate _page and _size", [("page", request.Page), ("size", request.Size), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<ListBranchesCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(ListBranchesCommand))]);
        command.Filters = filters;
        command.Order = sortFields;
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CMN-PIP-06", "Send the command", [("total", response.TotalCount)]);

        var branches = _mapper.Map<List<ListBranchesResponse>>(response.Items);
        StepTrace.Step("BRN-LST-04", "200 with the page", [("count", branches.Count), ("total", response.TotalCount), ("page", response.Page), ("size", response.Size)]);
        return OkPaginated(new PaginatedList<ListBranchesResponse>(branches, response.TotalCount, response.Page, response.Size));
    }

    // Work item: TASK-050 (FEAT-017)
    /// <summary>
    /// Updates a branch
    /// </summary>
    /// <param name="id">The unique identifier of the branch to update</param>
    /// <param name="request">The branch update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated branch details</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateBranchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBranch([FromRoute] Guid id, [FromBody] UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        var validator = new UpdateBranchRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("BRN-UPD-01", "CMN-PIP-04", "Validate the request", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<UpdateBranchCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(UpdateBranchCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("BRN-UPD-06", "CMN-PIP-06", "200 with the branch", [("id", response.Id)]);

        return Ok(new ApiResponseWithData<UpdateBranchResponse>
        {
            Success = true,
            Message = "Branch updated successfully",
            Data = _mapper.Map<UpdateBranchResponse>(response)
        });
    }

    // Work item: TASK-050 (FEAT-017)
    /// <summary>
    /// Deletes a branch by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the branch to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if the branch was deleted</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBranch([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteBranchRequest { Id = id };
        var validator = new DeleteBranchRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("BRN-DEL-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var command = _mapper.Map<DeleteBranchCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(DeleteBranchCommand))]);
        await _mediator.Send(command, cancellationToken);
        StepTrace.Step("BRN-DEL-04", "CMN-PIP-06", "200", [("id", request.Id)]);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Branch deleted successfully"
        });
    }
}
