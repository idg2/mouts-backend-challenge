#if DEBUG
using System.Security.Claims;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Tracing;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

// Work item: TASK-046 (FEAT-017)
/// <summary>
/// Contains unit tests for the trace-only <see cref="JwtBearerEvents"/> that
/// <see cref="AuthenticationExtension.AddJwtAuthentication"/> installs in Debug builds.
/// </summary>
[Collection("StepTrace")]
public class AuthenticationExtensionTraceTests : IDisposable
{
    private readonly List<StepEvent> _events = new();
    private readonly JwtBearerOptions _options;
    private readonly AuthenticationScheme _scheme =
        new(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

    public AuthenticationExtensionTraceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = new string('k', 64) })
            .Build();
        var provider = new ServiceCollection().AddJwtAuthentication(configuration).BuildServiceProvider();
        _options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    [Fact(DisplayName = "Given an authenticated user When authorization forbids Then AUT-06 carries the user id and the role")]
    public async Task Given_AuthenticatedUser_When_Forbidden_Then_Aut06CarriesUserIdAndRole()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-42"), new Claim(ClaimTypes.Role, "Customer")], "Bearer"))
        };

        // Act
        await _options.Events.OnForbidden(new ForbiddenContext(httpContext, _scheme, _options));

        // Assert
        var forbidden = _events.Single(e => e.Key == "CMN-AUT-06");
        forbidden.Values.Should().Contain(("allowed", "False"))
            .And.Contain(("userId", "user-42"))
            .And.Contain(("role", "Customer"));
    }

    [Fact(DisplayName = "Given a Bearer header When the message is received Then AUT-01 says hasBearer")]
    public async Task Given_BearerHeader_When_MessageReceived_Then_Aut01HasBearer()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/users";
        httpContext.Request.Headers.Authorization = "Bearer x";

        // Act
        await _options.Events.OnMessageReceived(new MessageReceivedContext(httpContext, _scheme, _options));

        // Assert
        var received = _events.Single(e => e.Key == "CMN-AUT-01");
        received.Values.Should().Contain(("hasBearer", "True")).And.Contain(("path", "/api/users"));
    }
}
#endif
