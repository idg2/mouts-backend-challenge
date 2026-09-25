using Ambev.DeveloperEvaluation.DevConsole.Trace;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="DatabaseWipe"/> step of the trace command.
/// </summary>
public class DatabaseWipeTests
{
    private static readonly (string Postgres, string Mongo) Names = ("developer_evaluation", "developer_evaluation_bus");

    [Fact(DisplayName = "Given the WebApi connection strings When reading the names Then both databases are named")]
    public void Given_ConnectionStrings_When_ReadingNames_Then_BothNamed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=developer_evaluation;Username=u;Password=p",
            ["ConnectionStrings:MessageBus"] = "mongodb://u:p@localhost:27017/developer_evaluation_bus?authSource=admin"
        }).Build();

        // Act
        var names = DatabaseWipe.Names(configuration);

        // Assert
        names.Should().Be(Names);
    }

    // Work item: TASK-056 (FEAT-017), TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given the user declines When wiping Then nothing is dropped and the exit code is 1")]
    public async Task Given_Declined_When_Wiping_Then_NothingDropped()
    {
        // Arrange
        var dropped = false;
        var output = new StringWriter();

        // Act
        var code = await DatabaseWipe.RunAsync(Names, output, new StringWriter(), () => false, () => { dropped = true; return Task.CompletedTask; }, () => { dropped = true; return Task.CompletedTask; });

        // Assert
        code.Should().Be(1);
        dropped.Should().BeFalse();
        output.ToString().Should().Contain("developer_evaluation").And.Contain("developer_evaluation_bus");
    }

    // Work item: TASK-056 (FEAT-017), TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given other sessions hold the database When wiping Then the stop command prints to the error writer and the exit code is 1")]
    public async Task Given_OtherSessions_When_Wiping_Then_PrintsStopCommandAndExits()
    {
        // Arrange
        var output = new StringWriter();
        var error = new StringWriter();
        var mongoDropped = false;
        Task DropPostgres() => throw new PostgresException("being accessed by other users", "ERROR", "ERROR", "55006");

        // Act
        var code = await DatabaseWipe.RunAsync(Names, output, error, () => true, DropPostgres, () => { mongoDropped = true; return Task.CompletedTask; });

        // Assert
        code.Should().Be(1);
        mongoDropped.Should().BeFalse();
        error.ToString().Should().Contain("docker compose stop ambev.developerevaluation.webapi");
        output.ToString().Should().NotContain("docker compose stop");
    }

    // Work item: TASK-056 (FEAT-017), TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given confirmation When wiping Then both drops run and the exit code is 0")]
    public async Task Given_Confirmed_When_Wiping_Then_BothDropped()
    {
        // Arrange
        var drops = 0;

        // Act
        var code = await DatabaseWipe.RunAsync(Names, new StringWriter(), new StringWriter(), () => true, () => { drops++; return Task.CompletedTask; }, () => { drops++; return Task.CompletedTask; });

        // Assert
        code.Should().Be(0);
        drops.Should().Be(2);
    }

    // Work item: TASK-056 (FEAT-017), TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given the MongoDB drop fails after the PostgreSQL drop When wiping Then the error writer says which one was dropped")]
    public async Task Given_MongoDropFails_When_Wiping_Then_SaysPostgresDroppedMongoNot()
    {
        // Arrange
        var output = new StringWriter();
        var error = new StringWriter();
        Task DropMongo() => throw new TimeoutException("no server");

        // Act
        var code = await DatabaseWipe.RunAsync(Names, output, error, () => true, () => Task.CompletedTask, DropMongo);

        // Assert
        code.Should().Be(1);
        error.ToString().Should()
            .Contain("PostgreSQL database \"developer_evaluation\" was dropped")
            .And.Contain("MongoDB queue database \"developer_evaluation_bus\" was not")
            .And.Contain("TimeoutException: no server");
        output.ToString().Should().NotContain("was not").And.NotContain("Both databases dropped.");
    }

    // Work item: TASK-056 (FEAT-017)
    [Fact(DisplayName = "Given the application connection string When building the maintenance one Then only the database changes")]
    public void Given_ApplicationConnection_When_BuildingMaintenance_Then_OnlyDatabaseChanges()
    {
        // Act
        var maintenance = new NpgsqlConnectionStringBuilder(
            DatabaseWipe.MaintenanceConnectionString("Host=db.local;Port=6543;Database=developer_evaluation;Username=u;Password=p", "maint"));

        // Assert
        maintenance.Database.Should().Be("maint");
        maintenance.Host.Should().Be("db.local");
        maintenance.Port.Should().Be(6543);
        maintenance.Username.Should().Be("u");
        maintenance.Password.Should().Be("p");
        maintenance.Pooling.Should().BeFalse();
    }

    // Work item: TASK-056 (FEAT-017)
    [Fact(DisplayName = "Given a blank connection string When reading the names Then it throws naming the key")]
    public void Given_BlankConnection_When_ReadingNames_Then_ThrowsNamingKey()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "   ",
            ["ConnectionStrings:MessageBus"] = "mongodb://u:p@localhost:27017/developer_evaluation_bus?authSource=admin"
        }).Build();

        // Act
        var act = () => DatabaseWipe.Names(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("ConnectionStrings:DefaultConnection is not configured.");
    }
}
