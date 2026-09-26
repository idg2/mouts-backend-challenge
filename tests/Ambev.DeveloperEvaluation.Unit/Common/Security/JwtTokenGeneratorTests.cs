using Ambev.DeveloperEvaluation.Common.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

// Work item: TD-041
/// <summary>
/// Contains unit tests for <see cref="JwtTokenGenerator"/> when its configuration is incomplete.
/// </summary>
public class JwtTokenGeneratorTests
{
    /// <summary>
    /// Tests that a missing signing key fails with a message that names the setting, like every other required key.
    /// </summary>
    [Fact(DisplayName = "Given no Jwt:SecretKey When generating a token Then fails naming the setting")]
    public void Given_NoSecretKey_When_GeneratingToken_Then_FailsNamingTheSetting()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var generator = new JwtTokenGenerator(configuration);
        var user = Substitute.For<IUser>();

        // Act
        var act = () => generator.GenerateToken(user);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:SecretKey*");
    }
}
