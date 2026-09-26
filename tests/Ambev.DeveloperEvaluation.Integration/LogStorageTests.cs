using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-033 (FEAT-016)
/// <summary>
/// Checks the production logging wiring against the compose MongoDB: stored events, the TTL index, and the health filter.
/// </summary>
public class LogStorageTests : IClassFixture<MongoLogStorageFixture>
{
    private const string TtlIndexName = "serilog_sink_expired_ttl";

    private readonly MongoLogStorageFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogStorageTests"/> class.
    /// </summary>
    public LogStorageTests(MongoLogStorageFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that Information and Warning events are both stored with the host properties.
    /// </summary>
    [Fact(DisplayName = "Given the production wiring When writing Information and Warning events Then both are stored with the host properties")]
    public async Task Given_ProductionWiring_When_WritingInformationAndWarning_Then_BothAreStored()
    {
        // Arrange
        var collection = NewCollectionName();

        // Act
        using (var logger = _fixture.CreateLogger(collection))
        {
            logger.Information("Probe information");
            logger.Warning("Probe warning");
        }

        // Assert
        var documents = await _fixture.Collection(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(new[] { "Information", "Warning" }, documents.Select(document => document["Level"].AsString).OrderBy(level => level));
        Assert.All(documents, document =>
        {
            Assert.Equal(MongoLogStorageFixture.ApplicationName, document["Properties"]["Application"].AsString);
            Assert.Equal(MongoLogStorageFixture.EnvironmentName, document["Properties"]["Environment"].AsString);
        });
    }

    /// <summary>
    /// Tests that the first write creates a TTL index with the configured time span.
    /// </summary>
    [Fact(DisplayName = "Given the configured ExpireAfter When writing the first event Then the TTL index matches it")]
    public async Task Given_ConfiguredExpireAfter_When_WritingFirstEvent_Then_TtlIndexMatches()
    {
        // Arrange
        var collection = NewCollectionName();

        // Act
        using (var logger = _fixture.CreateLogger(collection))
            logger.Information("Probe");

        // Assert
        var index = await TtlIndex(collection);
        Assert.Equal(_fixture.Settings.ExpireAfter.TotalSeconds, index["expireAfterSeconds"].ToDouble());
        Assert.Equal("UtcTimeStamp", index["key"].AsBsonDocument.Names.Single());
    }

    /// <summary>
    /// Tests that a new ExpireAfter on an existing collection updates the index and keeps writing.
    /// </summary>
    [Fact(DisplayName = "Given a new ExpireAfter When restarting on the same collection Then the index is updated and writes continue")]
    public async Task Given_NewExpireAfter_When_RestartingOnSameCollection_Then_IndexIsUpdatedAndWritesContinue()
    {
        // Arrange
        var collection = NewCollectionName();
        using (var logger = _fixture.CreateLogger(collection))
            logger.Information("First run");

        // Act
        using (var logger = _fixture.CreateLogger(collection, TimeSpan.FromHours(12)))
            logger.Information("Second run");

        // Assert
        var index = await TtlIndex(collection);
        Assert.Equal(TimeSpan.FromHours(12).TotalSeconds, index["expireAfterSeconds"].ToDouble());
        Assert.Equal(2, await _fixture.Collection(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    /// <summary>
    /// Tests that the configured filter drops only successful health probes.
    /// </summary>
    [Theory(DisplayName = "Given a request-log event When logging Then only successful health probes are dropped")]
    [InlineData("/health", 200, false)]
    [InlineData("/health/live", 200, false)]
    [InlineData("/health/ready", 503, true)]
    [InlineData("/api/users", 200, true)]
    public async Task Given_RequestLogEvent_When_Logging_Then_OnlySuccessfulHealthProbesAreDropped(string path, int statusCode, bool stored)
    {
        // Arrange
        var collection = NewCollectionName();

        // Act
        using (var logger = _fixture.CreateLogger(collection))
            logger.Information("HTTP {RequestMethod} {RequestPath} responded {StatusCode}", "GET", path, statusCode);

        // Assert
        var count = await _fixture.Collection(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(stored ? 1 : 0, count);
    }

    // Work item: TASK-033 (FEAT-016)
    /// <summary>
    /// Tests that a failed save logged with its exception keeps the exception details but none of the entity values.
    /// </summary>
    [Fact(DisplayName = "Given a DbUpdateException with tracked entries When logging it Then no entity value is stored")]
    public async Task Given_DbUpdateExceptionWithEntries_When_Logging_Then_NoEntityValueIsStored()
    {
        // Arrange
        var collection = NewCollectionName();
        const string email = "leak.probe@example.com";
        const string passwordHash = "$2a$11$leakprobehashvalue";
        // The context only builds the model and tracks the entry; it never opens a connection.
        await using var context = new DefaultContext(
            new DbContextOptionsBuilder<DefaultContext>().UseNpgsql("Host=never-opened").Options);
        var user = new User { Email = email, Password = passwordHash, Username = "leak probe", Phone = "+5511900000000" };
        context.Users.Add(user);
        var failure = new DbUpdateException("Save failed", null, new[] { context.Entry(user) });

        // Act
        using (var logger = _fixture.CreateLogger(collection))
            logger.Error(failure, "Save failed");

        // Assert
        var document = (await _fixture.Collection(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Single();
        var json = document.ToJson();
        Assert.Contains("DbUpdateException", document["Properties"]["ExceptionDetail"].ToJson());
        Assert.DoesNotContain(email, json);
        Assert.DoesNotContain(passwordHash, json);
    }

    // Work item: TASK-033 (FEAT-016)
    /// <summary>
    /// Tests that a batch the sink cannot write is reported on standard error, so a dead log store is visible.
    /// </summary>
    [Fact(DisplayName = "Given an unreachable MongoDB When a batch fails Then the failure is reported on standard error")]
    public void Given_UnreachableMongo_When_BatchFails_Then_FailureIsReportedOnStandardError()
    {
        // Arrange
        // Port 1 on the loopback interface never runs MongoDB; the short timeout keeps the test fast.
        const string unreachable = "mongodb://localhost:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";
        var originalError = Console.Error;
        var captured = new StringWriter();
        Console.SetError(captured);

        // Act
        try
        {
            using var logger = _fixture.CreateLogger(NewCollectionName(), connectionString: unreachable);
            logger.Information("Probe");
        }
        finally
        {
            Console.SetError(originalError);
        }

        // Assert
        Assert.Contains("Exception while emitting periodic batch", captured.ToString());
    }

    private static string NewCollectionName() => $"logs_{Guid.NewGuid():N}";

    private async Task<BsonDocument> TtlIndex(string collection)
    {
        var indexes = await (await _fixture.Collection(collection).Indexes.ListAsync()).ToListAsync();
        return indexes.Single(index => index["name"].AsString == TtlIndexName);
    }
}
