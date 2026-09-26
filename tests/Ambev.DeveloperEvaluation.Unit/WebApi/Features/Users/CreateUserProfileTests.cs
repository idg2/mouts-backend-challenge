using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using AutoMapper;
using FluentAssertions;
using Xunit;
using CreateUserProfile = Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser.CreateUserProfile;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

// Work item: BUG-010
/// <summary>
/// Contains unit tests for the WebApi <see cref="CreateUserProfile"/> class.
/// </summary>
public class CreateUserProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<CreateUserProfile>()).CreateMapper();

    /// <summary>
    /// Tests that the handler result maps to the 201 response the controller returns.
    /// </summary>
    [Fact(DisplayName = "Given a create user result When mapping to the response Then copies every field")]
    public void Given_CreateUserResult_When_MappingToResponse_Then_CopiesEveryField()
    {
        // Arrange
        var result = new CreateUserResult
        {
            Id = Guid.NewGuid(),
            Name = "bug010",
            Email = "bug010@example.com",
            Phone = "+5511999998888",
            Role = UserRole.Manager,
            Status = UserStatus.Active
        };

        // Act
        var response = _mapper.Map<CreateUserResponse>(result);

        // Assert
        response.Should().BeEquivalentTo(result);
    }
}
