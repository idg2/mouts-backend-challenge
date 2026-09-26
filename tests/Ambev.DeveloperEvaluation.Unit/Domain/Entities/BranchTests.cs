using Ambev.DeveloperEvaluation.Domain.Entities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Contains unit tests for the Branch entity class.
/// </summary>
public class BranchTests
{
    /// <summary>
    /// Tests that validation passes when the branch data is valid.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for valid branch data")]
    public void Given_ValidBranch_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var branch = new Branch { Id = Guid.NewGuid(), Name = "Downtown" };

        // Act
        var result = branch.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
