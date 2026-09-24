using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using AutoMapper;
using FluentAssertions;
using Xunit;
using GetUserProfile = Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser.GetUserProfile;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

// Work item: BUG-005
/// <summary>
/// Contains unit tests for the WebApi <see cref="GetUserProfile"/> class.
/// </summary>
public class GetUserProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GetUserProfile>()).CreateMapper();

    /// <summary>
    /// Tests that the handler result maps to the API response the controller returns.
    /// </summary>
    [Fact(DisplayName = "Given a get user result When mapping to the response Then copies every field")]
    public void Given_GetUserResult_When_MappingToResponse_Then_CopiesEveryField()
    {
        // Arrange
        var result = new GetUserResult
        {
            Id = Guid.NewGuid(),
            Name = "bug005",
            Email = "bug005@example.com",
            Phone = "+5511999998888",
            Role = UserRole.Manager,
            Status = UserStatus.Active
        };

        // Act
        var response = _mapper.Map<GetUserResponse>(result);

        // Assert
        response.Should().BeEquivalentTo(result);
    }
}
