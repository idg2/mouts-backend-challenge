using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.WebApi.Seeding;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Seeding;

// Work item: BUG-012
/// <summary>
/// Contains unit tests for the <see cref="AdminSeeder"/> class.
/// </summary>
public class AdminSeederTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminSeeder _seeder;
    private readonly AdminSeedSettings _settings = AdminSeedSettings.FromConfiguration(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AdminSeedSettings.UsernameKey] = "admin",
            [AdminSeedSettings.EmailKey] = "admin@example.com",
            [AdminSeedSettings.PasswordKey] = "Adm1n@Pass",
            [AdminSeedSettings.PhoneKey] = "+5511999990000"
        }).Build());

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminSeederTests"/> class.
    /// </summary>
    public AdminSeederTests()
    {
        _mediator.Send(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreateUserResult { Id = Guid.NewGuid() });
        _seeder = new AdminSeeder(_userRepository, _mediator, NullLogger<AdminSeeder>.Instance);
    }

    /// <summary>
    /// Tests that the administrator is created as an active Admin with the configured values when its e-mail is free.
    /// </summary>
    [Fact(DisplayName = "Given no user with the configured e-mail When seeding Then creates an active Admin")]
    public async Task Given_NoUserWithConfiguredEmail_When_Seeding_Then_CreatesActiveAdmin()
    {
        // Arrange
        _userRepository.GetByEmailAsync("admin@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await _seeder.SeedAsync(_settings, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<CreateUserCommand>(command =>
                command.Username == "admin" &&
                command.Email == "admin@example.com" &&
                command.Password == "Adm1n@Pass" &&
                command.Phone == "+5511999990000" &&
                command.Role == UserRole.Admin &&
                command.Status == UserStatus.Active),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that nothing is created when a user already has the configured e-mail, so a restart does not fail.
    /// </summary>
    [Fact(DisplayName = "Given a user with the configured e-mail When seeding Then creates nothing")]
    public async Task Given_UserWithConfiguredEmail_When_Seeding_Then_CreatesNothing()
    {
        // Arrange
        _userRepository.GetByEmailAsync("admin@example.com", Arg.Any<CancellationToken>())
            .Returns(new User { Email = "admin@example.com" });

        // Act
        await _seeder.SeedAsync(_settings, CancellationToken.None);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>());
    }
}
