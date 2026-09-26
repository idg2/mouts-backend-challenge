using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Users;

// Work item: BUG-005
/// <summary>
/// Contains unit tests for the Application <see cref="GetUserProfile"/> class.
/// </summary>
public class GetUserProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GetUserProfile>()).CreateMapper();

    /// <summary>
    /// Tests that the user entity maps to the result with the username as the name.
    /// </summary>
    [Fact(DisplayName = "Given a user When mapping to the result Then name comes from the username")]
    public void Given_User_When_MappingToResult_Then_NameComesFromUsername()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "bug005",
            Email = "bug005@example.com",
            Phone = "+5511999998888",
            Role = UserRole.Manager,
            Status = UserStatus.Active
        };

        // Act
        var result = _mapper.Map<GetUserResult>(user);

        // Assert
        result.Should().BeEquivalentTo(new GetUserResult
        {
            Id = user.Id,
            Name = "bug005",
            Email = "bug005@example.com",
            Phone = "+5511999998888",
            Role = UserRole.Manager,
            Status = UserStatus.Active
        });
    }
}
