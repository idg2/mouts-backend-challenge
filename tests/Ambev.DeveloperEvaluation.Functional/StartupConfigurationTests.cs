using MongoDB.Driver;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

// Work item: TD-043
/// <summary>
/// Proves that a missing required setting stops the API at startup with a message that names the key.
/// </summary>
public class StartupConfigurationTests
{
    // Work item: TD-043
    [Fact(DisplayName = "Given no DefaultConnection When the API starts Then it fails naming ConnectionStrings:DefaultConnection")]
    public async Task Given_NoDefaultConnection_When_Starting_Then_FailsNamingKey()
    {
        // Arrange
        // The failed start logs a fatal line; it goes to a throwaway log database, dropped below.
        var logDatabase = $"functional_logs_{Guid.NewGuid():N}";
        await using var factory = new SalesApiFixture.SalesApiFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "",
            ["LogStorage:Database"] = logDatabase
        });

        // Act
        var exception = Record.Exception(() => factory.CreateClient());

        // Assert
        Assert.NotNull(exception);
        Assert.Contains("ConnectionStrings:DefaultConnection", exception.Message);
        var logStorage = SalesApiFixture.AppConfiguration()["ConnectionStrings:LogStorage"];
        await new MongoClient(logStorage).DropDatabaseAsync(logDatabase);
    }
}
