using System.Security.Claims;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: BUG-001, TD-007 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="BaseController"/> class.
/// </summary>
public class BaseControllerTests
{
    /// <summary>
    /// Tests that the current user id is read from a NameIdentifier claim holding a Guid.
    /// </summary>
    [Fact(DisplayName = "Given a Guid NameIdentifier claim When getting the current user id Then returns the Guid")]
    public void Given_GuidNameIdentifierClaim_When_GetCurrentUserId_Then_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var controller = new TestController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }))
            }
        };

        // Act
        var result = controller.CurrentUserId();

        // Assert
        Assert.Equal(userId, result);
    }

    // Work item: TD-007 (FEAT-010)
    /// <summary>
    /// Tests that Ok returns the given response as the body, without wrapping it in another ApiResponse (BUG-004).
    /// </summary>
    [Fact(DisplayName = "Given an ApiResponse When calling Ok Then the body is that response, not wrapped again")]
    public void Given_ApiResponse_When_Ok_Then_BodyIsNotWrappedAgain()
    {
        // Arrange
        var response = new ApiResponseWithData<string> { Success = true, Message = "ok", Data = "value" };
        var controller = new TestController();

        // Act
        var result = controller.OkResponse(response);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    /// <summary>
    /// Exposes the protected members of <see cref="BaseController"/> to the tests.
    /// </summary>
    private sealed class TestController : BaseController
    {
        public Guid CurrentUserId() => GetCurrentUserId();

        // Work item: TD-007 (FEAT-010)
        public IActionResult OkResponse(object value) => Ok(value);
    }
}
