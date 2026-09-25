using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.WebApi.Seeding;

// Work item: BUG-012
/// <summary>
/// Creates the configured administrator, the first user able to call the users API, unless a user already has its
/// e-mail.
/// </summary>
public sealed class AdminSeeder
{
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<AdminSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of AdminSeeder
    /// </summary>
    /// <param name="userRepository">The user repository</param>
    /// <param name="mediator">The mediator that runs the create user command</param>
    /// <param name="logger">The logger</param>
    public AdminSeeder(IUserRepository userRepository, IMediator mediator, ILogger<AdminSeeder> logger)
    {
        _userRepository = userRepository;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates the administrator as an active Admin through the create user command, which validates and hashes it.
    /// </summary>
    /// <param name="settings">The administrator to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SeedAsync(AdminSeedSettings settings, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(settings.Email, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Administrator seed skipped: user {UserId} already has the configured e-mail", existing.Id);
            return;
        }

        var created = await _mediator.Send(new CreateUserCommand
        {
            Username = settings.Username,
            Email = settings.Email,
            Password = settings.Password,
            Phone = settings.Phone,
            Status = UserStatus.Active,
            Role = UserRole.Admin
        }, cancellationToken);

        _logger.LogInformation("Administrator {UserId} seeded", created.Id);
    }
}
