using System.Reflection;
using Ambev.DeveloperEvaluation.WebApi.Features.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

// Work item: BUG-012
/// <summary>
/// Contains unit tests for the authorization rules of <see cref="UsersController"/>.
/// </summary>
public class UsersControllerAuthorizationTests
{
    /// <summary>
    /// Tests that the controller requires a token with the Admin or Manager role.
    /// </summary>
    [Fact(DisplayName = "Given the users controller When reading its authorization Then requires Admin or Manager")]
    public void Given_UsersController_When_ReadingAuthorization_Then_RequiresAdminOrManager()
    {
        // Act
        var authorize = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be("Admin,Manager");
    }

    /// <summary>
    /// Tests that no action opts out of the controller's authorization.
    /// </summary>
    [Fact(DisplayName = "Given the users controller actions When reading their attributes Then none allows anonymous")]
    public void Given_UsersControllerActions_When_ReadingAttributes_Then_NoneAllowsAnonymous()
    {
        // Act
        var anonymousActions = typeof(UsersController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(method => method.Name);

        // Assert
        anonymousActions.Should().BeEmpty();
    }
}
