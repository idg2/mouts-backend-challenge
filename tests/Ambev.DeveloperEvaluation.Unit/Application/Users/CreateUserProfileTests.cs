using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Users;

// Work item: BUG-010
/// <summary>
/// Contains unit tests for the Application <see cref="CreateUserProfile"/> class.
/// </summary>
public class CreateUserProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<CreateUserProfile>()).CreateMapper();

    /// <summary>
    /// Tests that the created user maps to the result with every field the response echoes.
    /// </summary>
    [Fact(DisplayName = "Given a created user When mapping to the result Then carries every field with the username as the name")]
    public void Given_CreatedUser_When_MappingToResult_Then_CarriesEveryField()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "bug010",
            Email = "bug010@example.com",
            Phone = "+5511999998888",
            Role = UserRole.Manager,
            Status = UserStatus.Active
        };

        // Act
        var result = _mapper.Map<CreateUserResult>(user);

        // Assert
        result.Should().BeEquivalentTo(new CreateUserResult
        {
            Id = user.Id,
            Name = "bug010",
            Email = "bug010@example.com",
            Phone = "+5511999998888",
            Role = UserRole.Manager,
            Status = UserStatus.Active
        });
    }
}
