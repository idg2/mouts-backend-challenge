using Ambev.DeveloperEvaluation.WebApi.Seeding;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Seeding;

// Work item: BUG-012
/// <summary>
/// Contains unit tests for the <see cref="AdminSeedSettings"/> class.
/// </summary>
public class AdminSeedSettingsTests
{
    /// <summary>
    /// Tests that a complete configuration yields its values.
    /// </summary>
    [Fact(DisplayName = "Given a complete configuration When reading Then returns its values")]
    public void Given_CompleteConfiguration_When_Reading_Then_ReturnsValues()
    {
        // Arrange
        var configuration = Build(ValidValues());

        // Act
        var settings = AdminSeedSettings.FromConfiguration(configuration);

        // Assert
        settings.Username.Should().Be("admin");
        settings.Email.Should().Be("admin@example.com");
        settings.Password.Should().Be("Adm1n@Pass");
        settings.Phone.Should().Be("+5511999990000");
    }

    /// <summary>
    /// Tests that each missing or blank key fails with a message naming it.
    /// </summary>
    [Theory(DisplayName = "Given a missing or blank key When reading Then throws naming the key")]
    [InlineData(AdminSeedSettings.UsernameKey, null)]
    [InlineData(AdminSeedSettings.UsernameKey, "  ")]
    [InlineData(AdminSeedSettings.EmailKey, null)]
    [InlineData(AdminSeedSettings.EmailKey, "  ")]
    [InlineData(AdminSeedSettings.PasswordKey, null)]
    [InlineData(AdminSeedSettings.PasswordKey, "  ")]
    [InlineData(AdminSeedSettings.PhoneKey, null)]
    [InlineData(AdminSeedSettings.PhoneKey, "  ")]
    public void Given_MissingOrBlankKey_When_Reading_Then_ThrowsNamingTheKey(string key, string? value)
    {
        // Arrange
        var values = ValidValues();
        if (value is null)
            values.Remove(key);
        else
            values[key] = value;

        // Act
        var act = () => AdminSeedSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    private static Dictionary<string, string?> ValidValues() => new()
    {
        [AdminSeedSettings.UsernameKey] = "admin",
        [AdminSeedSettings.EmailKey] = "admin@example.com",
        [AdminSeedSettings.PasswordKey] = "Adm1n@Pass",
        [AdminSeedSettings.PhoneKey] = "+5511999990000"
    };

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
