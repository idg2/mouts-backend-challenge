using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Auth;

// Work item: TASK-035 (FEAT-016)
/// <summary>
/// Contains unit tests for the <see cref="AuthenticateUserHandler"/> class.
/// </summary>
public class AuthenticateUserHandlerTests
{
    private const string Password = "Typed@Password123";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ILogger<AuthenticateUserHandler> _logger = Substitute.For<ILogger<AuthenticateUserHandler>>();
    private readonly AuthenticateUserHandler _handler;
    private readonly User _user;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticateUserHandlerTests"/> class.
    /// </summary>
    public AuthenticateUserHandlerTests()
    {
        _handler = new AuthenticateUserHandler(_userRepository, _passwordHasher, _jwtTokenGenerator, _logger);
        _user = UserTestData.GenerateValidUser();
        _user.Id = Guid.NewGuid();
        _user.Status = UserStatus.Active;
    }

    /// <summary>
    /// Tests that an unknown e-mail logs a Warning without identifying data.
    /// </summary>
    [Fact(DisplayName = "Given an unknown e-mail When authenticating Then logs a Warning and rejects")]
    public async Task Given_UnknownEmail_When_Authenticating_Then_LogsWarningAndRejects()
    {
        // Arrange
        _userRepository.GetByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var act = () => _handler.Handle(Command(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials");
        var entry = _logger.Entries().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Be("Authentication failed: unknown user");
        AssertNoCredentials();
    }

    /// <summary>
    /// Tests that a wrong password logs a Warning with the user id.
    /// </summary>
    [Fact(DisplayName = "Given a wrong password When authenticating Then logs a Warning with the user id and rejects")]
    public async Task Given_WrongPassword_When_Authenticating_Then_LogsWarningWithUserIdAndRejects()
    {
        // Arrange
        _userRepository.GetByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns(_user);
        _passwordHasher.VerifyPassword(Password, _user.Password).Returns(false);

        // Act
        var act = () => _handler.Handle(Command(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials");
        var entry = _logger.Entries().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Be($"Authentication failed: wrong password for user {_user.Id}");
        entry.Properties.Should().Contain(new KeyValuePair<string, object?>("UserId", _user.Id));
        AssertNoCredentials();
    }

    /// <summary>
    /// Tests that an inactive user logs a Warning with the user id.
    /// </summary>
    [Fact(DisplayName = "Given an inactive user When authenticating Then logs a Warning with the user id and rejects")]
    public async Task Given_InactiveUser_When_Authenticating_Then_LogsWarningWithUserIdAndRejects()
    {
        // Arrange
        _user.Status = UserStatus.Suspended;
        _userRepository.GetByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns(_user);
        _passwordHasher.VerifyPassword(Password, _user.Password).Returns(true);

        // Act
        var act = () => _handler.Handle(Command(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("User is not active");
        var entry = _logger.Entries().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Be($"Authentication failed: user {_user.Id} is not active");
        entry.Properties.Should().Contain(new KeyValuePair<string, object?>("UserId", _user.Id));
        AssertNoCredentials();
    }

    /// <summary>
    /// Tests that a successful login logs Information with the user id and returns the token.
    /// </summary>
    [Fact(DisplayName = "Given valid credentials When authenticating Then logs Information with the user id and returns the token")]
    public async Task Given_ValidCredentials_When_Authenticating_Then_LogsInformationAndReturnsToken()
    {
        // Arrange
        _userRepository.GetByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns(_user);
        _passwordHasher.VerifyPassword(Password, _user.Password).Returns(true);
        _jwtTokenGenerator.GenerateToken(_user).Returns("token");

        // Act
        var result = await _handler.Handle(Command(), CancellationToken.None);

        // Assert
        result.Token.Should().Be("token");
        var entry = _logger.Entries().Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Be($"User {_user.Id} authenticated");
        entry.Properties.Should().Contain(new KeyValuePair<string, object?>("UserId", _user.Id));
        AssertNoCredentials();
    }

    private AuthenticateUserCommand Command() => new() { Email = _user.Email, Password = Password };

    private void AssertNoCredentials()
    {
        foreach (var entry in _logger.Entries())
        {
            var values = entry.Properties.Select(property => property.Value?.ToString() ?? string.Empty).Append(entry.Message);
            values.Should().NotContain(value => value.Contains(_user.Email) || value.Contains(Password));
        }
    }
}
