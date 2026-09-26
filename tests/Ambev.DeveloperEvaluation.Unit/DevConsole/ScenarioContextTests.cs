using Ambev.DeveloperEvaluation.DevConsole.Trace;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Contains unit tests for the body redaction of the <see cref="ScenarioContext"/>.
/// </summary>
public class ScenarioContextTests
{
    [Fact(DisplayName = "Given a login request body When redacting Then the password is masked and the rest kept")]
    public void Given_LoginRequest_When_Redacting_Then_PasswordMasked()
    {
        // Act
        var redacted = ScenarioContext.Redact("""{"email":"admin@example.com","password":"Adm1n@Pass","name":"João"}""");

        // Assert
        redacted.Should().Be("""{"email":"admin@example.com","password":"***","name":"João"}""");
    }

    [Fact(DisplayName = "Given a nested response envelope When redacting Then the token is masked")]
    public void Given_NestedEnvelope_When_Redacting_Then_TokenMasked()
    {
        // Act
        var redacted = ScenarioContext.Redact("""{"data":{"token":"eyJhbGci.x.y","role":"Admin","users":[{"password":"h"}]},"success":true}""");

        // Assert
        redacted.Should().Be("""{"data":{"token":"***","role":"Admin","users":[{"password":"***"}]},"success":true}""");
    }

    [Fact(DisplayName = "Given mixed-case property names When redacting Then they are masked too")]
    public void Given_MixedCaseNames_When_Redacting_Then_Masked()
    {
        // Act
        var redacted = ScenarioContext.Redact("""{"PassWord":"a","TOKEN":"b"}""");

        // Assert
        redacted.Should().Be("""{"PassWord":"***","TOKEN":"***"}""");
    }

    // Work item: TASK-058 (FEAT-017)
    [Fact(DisplayName = "Given a validation failure for a password When redacting Then its attempted value is masked")]
    public void Given_PasswordValidationFailure_When_Redacting_Then_AttemptedValueMasked()
    {
        // Act
        var redacted = ScenarioContext.Redact(
            """[{"propertyName":"NewPassword","attemptedValue":"weak1","errorMessage":"Too short"},{"propertyName":"Email","attemptedValue":"a@b.c"}]""");

        // Assert
        redacted.Should().Be(
            """[{"propertyName":"NewPassword","attemptedValue":"***","errorMessage":"Too short"},{"propertyName":"Email","attemptedValue":"a@b.c"}]""");
    }

    // Work item: TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given a validation failure for a password When redacting Then its message placeholder values are masked")]
    public void Given_PasswordValidationFailure_When_Redacting_Then_PlaceholderValuesMasked()
    {
        // Act
        var redacted = ScenarioContext.Redact(
            """[{"propertyName":"Password","attemptedValue":"weak1","FormattedMessagePlaceholderValues":{"PropertyName":"Password","PropertyValue":"weak1"}},{"propertyName":"Email","formattedMessagePlaceholderValues":{"PropertyValue":"a@b.c"}}]""");

        // Assert
        redacted.Should().Be(
            """[{"propertyName":"Password","attemptedValue":"***","FormattedMessagePlaceholderValues":"***"},{"propertyName":"Email","formattedMessagePlaceholderValues":{"PropertyValue":"a@b.c"}}]""");
    }

    // Work item: TASK-058 (FEAT-017), TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given JSON with a duplicate property name When redacting Then a placeholder replaces the body")]
    public void Given_DuplicatePropertyName_When_Redacting_Then_Placeholder()
    {
        // Act
        var redacted = ScenarioContext.Redact("""{"password":"a","password":"b"}""");

        // Assert
        redacted.Should().Be("[unparsed body]");
    }

    // Work item: TASK-059 (FEAT-017)
    [Theory(DisplayName = "Given malformed text that looks like JSON When redacting Then a placeholder replaces the body")]
    [InlineData("""{"email":"a@b.c","password":"Adm1n@Pass" """)]
    [InlineData("""[{"password":"x"},""")]
    public void Given_MalformedJson_When_Redacting_Then_Placeholder(string text)
    {
        // Act
        var redacted = ScenarioContext.Redact(text);

        // Assert
        redacted.Should().Be("[unparsed body]");
    }

    [Theory(DisplayName = "Given text that is not JSON When redacting Then it is returned as is")]
    [InlineData("")]
    [InlineData("not json {\"password\":")]
    [InlineData("<html>password</html>")]
    public void Given_NotJson_When_Redacting_Then_Unchanged(string text)
    {
        // Act
        var redacted = ScenarioContext.Redact(text);

        // Assert
        redacted.Should().Be(text);
    }
}
