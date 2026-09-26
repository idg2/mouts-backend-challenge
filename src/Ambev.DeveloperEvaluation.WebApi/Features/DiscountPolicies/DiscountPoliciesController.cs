using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.GetDiscountPolicy;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.ListDiscountPolicies;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.CreateDiscountPolicy;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.DisableDiscountPolicies;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.GetDiscountPolicy;
using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.ListDiscountPolicies;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies;

// Work item: TASK-063 (FEAT-001), TD-032
/// <summary>
/// Controller for discount policies: create and disable (Admin, Manager), get, and list. Policies are never edited or
/// deleted.
/// </summary>
[ApiController]
[Route("api/discount-policies")]
[Authorize]
public class DiscountPoliciesController : BaseController
{
    private const string WriteRoles = "Admin,Manager";

    // Work item: TD-032
    private const string IncludeDisabledKey = "includeDisabled";

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of DiscountPoliciesController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public DiscountPoliciesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    // Work item: TASK-071 (FEAT-018), BUG-013
    /// <summary>
    /// Creates a discount policy. It applies to sales dated from ValidFrom, which may not be in the past
    /// </summary>
    /// <param name="request">The policy creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created policy with its tiers</returns>
    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<DiscountPolicyResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDiscountPolicy([FromBody] CreateDiscountPolicyRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateDiscountPolicyRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("DSC-CRT-01", "CMN-PIP-04", "Validate the request",
            [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("tiers", request.Tiers?.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<CreateDiscountPolicyCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(CreateDiscountPolicyCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("DSC-CRT-05", "CMN-PIP-06", "201 with the policy", [("id", response.Id), ("tiers", response.Tiers.Count)]);

        return CreatedAtAction(nameof(GetDiscountPolicy), new { id = response.Id }, new ApiResponseWithData<DiscountPolicyResult>
        {
            Success = true,
            Message = "Discount policy created successfully",
            Data = response
        });
    }

    // Work item: TASK-071 (FEAT-018)
    /// <summary>
    /// Retrieves a discount policy with its tiers by its ID
    /// </summary>
    /// <param name="id">The policy id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policy with its tiers if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<DiscountPolicyResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscountPolicy([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetDiscountPolicyRequest { Id = id };
        var validator = new GetDiscountPolicyRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("DSC-GET-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<GetDiscountPolicyCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(GetDiscountPolicyCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("DSC-GET-04", "CMN-PIP-06", "200 with the policy", [("id", response.Id), ("tiers", response.Tiers.Count)]);

        return Ok(new ApiResponseWithData<DiscountPolicyResult>
        {
            Success = true,
            Message = "Discount policy retrieved successfully",
            Data = response
        });
    }

    // Work item: TASK-063 (FEAT-001), TD-032, TASK-071 (FEAT-018)
    /// <summary>
    /// Lists discount policies one page at a time, each with its tiers. Query keys: productId and branchId (one id
    /// each, repeatable), id, validFrom and createdAt (a date or an instant), _minValidFrom, _maxValidFrom,
    /// _minCreatedAt, _maxCreatedAt; _order sorts by those fields, for example "validFrom desc"; without it the list is
    /// ordered by validFrom. Disabled policies are listed only with includeDisabled=true
    /// </summary>
    /// <param name="page">The page number, starting at 1 (query parameter _page)</param>
    /// <param name="size">The page size, from 1 to 100 (query parameter _size)</param>
    /// <param name="order">Comma-separated fields, each optionally followed by asc or desc (query parameter _order)</param>
    /// <param name="includeDisabled">true to list disabled policies too; false or absent hides them</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page of policies</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DiscountPolicyResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListDiscountPolicies(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        [FromQuery(Name = IncludeDisabledKey)] string? includeDisabled = null,
        CancellationToken cancellationToken = default)
    {
        var includeDisabledValues = Request.Query[IncludeDisabledKey];
        var showDisabled = false;
        var includeDisabledValid = includeDisabledValues.Count == 0
            || (includeDisabledValues.Count == 1 && bool.TryParse(includeDisabledValues[0], out showDisabled));
        var fieldQuery = new QueryCollection(Request.Query
            .Where(pair => !pair.Key.Equals(IncludeDisabledKey, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));
        var (filters, sortFields) = ListQueryParser.Parse<DiscountPolicyListFields>(fieldQuery);
        StepTrace.Step("DSC-LST-01", "Parse filters and order, see CMN-LST",
            [("filters", filters.Count), ("order", sortFields.Count), ("includeDisabled", showDisabled), ("includeDisabledValid", includeDisabledValid)]);
        if (!includeDisabledValid)
            throw new ValidationException(new[]
            {
                new ValidationFailure(IncludeDisabledKey, $"'{includeDisabledValues}' is not valid for '{IncludeDisabledKey}': expected true or false, once.")
                {
                    ErrorCode = "InvalidValue"
                }
            });

        var request = new ListDiscountPoliciesRequest { Page = page, Size = size };
        var validator = new ListDiscountPoliciesRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("DSC-LST-02", "CMN-LST-08", "Validate _page and _size",
            [("page", request.Page), ("size", request.Size), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<ListDiscountPoliciesCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(ListDiscountPoliciesCommand))]);
        command.Filters = filters;
        command.Order = sortFields;
        command.IncludeDisabled = showDisabled;
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("CMN-PIP-06", "Send the command", [("total", response.TotalCount)]);

        StepTrace.Step("DSC-LST-04", "200 with the page", [("count", response.Items.Count), ("total", response.TotalCount), ("page", response.Page), ("size", response.Size)]);
        return OkPaginated(new PaginatedList<DiscountPolicyResult>(response.Items, response.TotalCount, response.Page, response.Size));
    }

    // Work item: TD-032, TASK-071 (FEAT-018)
    /// <summary>
    /// Disables discount policies, all or nothing: an unknown id answers 404 and none is disabled. A disabled policy no
    /// longer prices sales; the sales it already priced keep their discounts. Disabling cannot be undone
    /// </summary>
    /// <param name="request">The ids of the policies to disable</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The disabled policies, in the order of the ids</returns>
    [HttpPost("disable")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(ApiResponseWithData<DisableDiscountPoliciesResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableDiscountPolicies([FromBody] DisableDiscountPoliciesRequest request, CancellationToken cancellationToken)
    {
        var validator = new DisableDiscountPoliciesRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("DSC-DIS-01", "CMN-PIP-04", "Validate the request",
            [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count), ("ids", request.Ids?.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<DisableDiscountPoliciesCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(DisableDiscountPoliciesCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("DSC-DIS-06", "CMN-PIP-06", "200 with the policies", [("policies", response.Policies.Count)]);

        return Ok(new ApiResponseWithData<DisableDiscountPoliciesResult>
        {
            Success = true,
            Message = "Discount policies disabled successfully",
            Data = response
        });
    }
}
