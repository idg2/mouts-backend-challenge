using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.DeleteUser;
using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Application.Users.DeleteUser;
using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users;

// Work item: BUG-012
/// <summary>
/// Controller for managing user operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = WriteRoles)]
public class UsersController : BaseController
{
    // Work item: BUG-012
    private const string WriteRoles = "Admin,Manager";

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of UsersController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public UsersController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    // Work item: TASK-048 (FEAT-017), TASK-071 (FEAT-018), BUG-013
    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">The user creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("USR-CRT-01", "CMN-PIP-04", "Validate the request", [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<CreateUserCommand>(request);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(CreateUserCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("USR-CRT-06", "CMN-PIP-06", "201 with id, name, email, phone, role, and status", [("userId", response.Id), ("role", response.Role), ("userStatus", response.Status)]);

        return CreatedAtAction(nameof(GetUser), new { id = response.Id }, new ApiResponseWithData<CreateUserResponse>
        {
            Success = true,
            Message = "User created successfully",
            Data = _mapper.Map<CreateUserResponse>(response)
        });
    }

    // Work item: TASK-048 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Retrieves a user by their ID
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user details if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetUserRequest { Id = id };
        var validator = new GetUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("USR-GET-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<GetUserCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(GetUserCommand))]);
        var response = await _mediator.Send(command, cancellationToken);
        StepTrace.Step("USR-GET-04", "CMN-PIP-06", "200 with the user", [("id", response.Id)]);

        return Ok(new ApiResponseWithData<GetUserResponse>
        {
            Success = true,
            Message = "User retrieved successfully",
            Data = _mapper.Map<GetUserResponse>(response)
        });
    }

    // Work item: TASK-048 (FEAT-017), TASK-071 (FEAT-018)
    /// <summary>
    /// Deletes a user by their ID
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if the user was deleted</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteUserRequest { Id = id };
        var validator = new DeleteUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        StepTrace.Step("USR-DEL-01", "CMN-PIP-04", "Validate the id", [("id", request.Id), ("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            return BadRequest(validationResult);

        var command = _mapper.Map<DeleteUserCommand>(request.Id);
        StepTrace.Step("CMN-PIP-05", "AutoMapper maps the request to a command", [("command", nameof(DeleteUserCommand))]);
        await _mediator.Send(command, cancellationToken);
        StepTrace.Step("USR-DEL-04", "CMN-PIP-06", "200", [("id", request.Id)]);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "User deleted successfully"
        });
    }
}
