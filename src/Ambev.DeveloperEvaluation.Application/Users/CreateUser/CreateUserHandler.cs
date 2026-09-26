using AutoMapper;
using MediatR;
using FluentValidation;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.Application.Users.CreateUser;

/// <summary>
/// Handler for processing CreateUserCommand requests
/// </summary>
public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of CreateUserHandler
    /// </summary>
    /// <param name="userRepository">The user repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="validator">The validator for CreateUserCommand</param>
    public CreateUserHandler(IUserRepository userRepository, IMapper mapper, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    // Work item: BUG-003, TASK-048 (FEAT-017)
    /// <summary>
    /// Handles the CreateUserCommand request
    /// </summary>
    /// <param name="command">The CreateUser command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user details</returns>
    public async Task<CreateUserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateUserCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        StepTrace.Step("USR-CRT-02", "CMN-PIP-10", "Validate the command", [("valid", validationResult.IsValid), ("errors", validationResult.Errors.Count)]);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var existingUser = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        StepTrace.Step("USR-CRT-03", "E-mail already stored?", [("exists", existingUser != null), ("existingId", existingUser?.Id)]);
        if (existingUser != null)
            throw new DuplicateEntryException($"User with email {command.Email} already exists");

        var user = _mapper.Map<User>(command);
        user.Password = _passwordHasher.HashPassword(command.Password);
        StepTrace.Step("USR-CRT-04", "Hash the password with BCrypt", [("hashed", !string.IsNullOrEmpty(user.Password)), ("role", user.Role), ("status", user.Status)]);

        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);
        StepTrace.Step("USR-CRT-05", "Insert the user", [("userId", createdUser.Id)]);
        var result = _mapper.Map<CreateUserResult>(createdUser);
        return result;
    }
}
