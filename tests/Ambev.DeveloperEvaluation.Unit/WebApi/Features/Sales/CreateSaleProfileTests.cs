using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

// Work item: TD-012
/// <summary>
/// Contains unit tests for <see cref="CreateSaleProfile"/>.
/// </summary>
public class CreateSaleProfileTests
{
    /// <summary>
    /// Tests that every command member is mapped or explicitly ignored, so the command id stays server-assigned by
    /// design and a future request field cannot fill it silently.
    /// </summary>
    [Fact(DisplayName = "Given the create sale profile When validating the configuration Then every member is mapped or ignored")]
    public void Given_CreateSaleProfile_When_ConfigurationValidated_Then_EveryMemberIsMappedOrIgnored()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<CreateSaleProfile>());

        // Act
        var act = () => configuration.AssertConfigurationIsValid();

        // Assert
        var exception = Record.Exception(act);
        Assert.Null(exception);
    }
}
