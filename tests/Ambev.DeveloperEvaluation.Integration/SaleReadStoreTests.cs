using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.ReadModel;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-075 (FEAT-003)
/// <summary>
/// Contains integration tests for the <see cref="SaleReadStore"/> class on the compose MongoDB. Every test uses
/// its own sale ids, and the list tests tag their rows with a unique marker, because the class shares one database.
/// </summary>
public class SaleReadStoreTests : IClassFixture<MongoReadModelFixture>
{
    private static long _nextNumber = 1000;

    private readonly MongoReadModelFixture _fixture;
    private readonly SaleReadStore _store;

    public SaleReadStoreTests(MongoReadModelFixture fixture)
    {
        _fixture = fixture;
        _store = fixture.CreateStore();
    }

    /// <summary>
    /// Tests that the first event inserts the document and that Guids, decimals, and dates come back unchanged.
    /// </summary>
    [Fact(DisplayName = "Given no document When upserting Then inserts and the snapshot reads back equivalent")]
    public async Task Given_NoDocument_When_Upserting_Then_InsertsAndReadsBack()
    {
        // Arrange
        var snapshot = Snapshot(Guid.NewGuid(), 1, "Acme Market", 95.5m);

        // Act
        var applied = await _store.UpsertAsync(snapshot, version: 5);
        var stored = await _store.GetAsync(snapshot.SaleId);

        // Assert
        Assert.True(applied);
        Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(stored));
    }

    /// <summary>
    /// Tests that a later event replaces the document.
    /// </summary>
    [Fact(DisplayName = "Given a document When upserting a greater version Then replaces it")]
    public async Task Given_Document_When_UpsertingGreaterVersion_Then_Replaces()
    {
        // Arrange
        var id = Guid.NewGuid();
        await _store.UpsertAsync(Snapshot(id, 2, "Before", 10m), version: 1);

        // Act
        var applied = await _store.UpsertAsync(Snapshot(id, 2, "After", 20m), version: 2);
        var stored = await _store.GetAsync(id);

        // Assert
        Assert.True(applied);
        Assert.Equal("After", stored!.CustomerName);
        Assert.Equal(20m, stored.TotalAmount);
    }

    /// <summary>
    /// Tests that an event with the same or a lower sequence is reported as ignored and changes nothing
    /// (Review Focus 1: two events of one sale handled concurrently, the older finishing last).
    /// </summary>
    [Theory(DisplayName = "Given a document When upserting an equal or lower version Then returns false and keeps it")]
    [InlineData(2)]
    [InlineData(1)]
    public async Task Given_Document_When_UpsertingEqualOrLowerVersion_Then_FalseAndKept(long olderVersion)
    {
        // Arrange
        var id = Guid.NewGuid();
        await _store.UpsertAsync(Snapshot(id, 3, "Current", 30m), version: 2);

        // Act
        var applied = await _store.UpsertAsync(Snapshot(id, 3, "Stale", 1m), version: olderVersion);
        var stored = await _store.GetAsync(id);

        // Assert
        Assert.False(applied);
        Assert.Equal("Current", stored!.CustomerName);
    }

    /// <summary>
    /// Tests that a tombstone hides the sale and wins over a delayed SaleModified (Review Focus 2 and 5).
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When an older modification arrives Then it stays deleted and get returns null")]
    public async Task Given_DeletedSale_When_OlderModificationArrives_Then_StaysDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        await _store.UpsertAsync(Snapshot(id, 4, "Alive", 10m), version: 1);

        // Act
        var deleted = await _store.MarkDeletedAsync(id, version: 3);
        var revived = await _store.UpsertAsync(Snapshot(id, 4, "Revived", 10m), version: 2);
        var stored = await _store.GetAsync(id);

        // Assert
        Assert.True(deleted);
        Assert.False(revived);
        Assert.Null(stored);
    }

    /// <summary>
    /// Tests that a SaleDeleted delivered before any snapshot leaves a tombstone that a later-arriving older
    /// SaleCreated cannot overwrite.
    /// </summary>
    [Fact(DisplayName = "Given no document When marking deleted Then a tombstone is inserted and an older create is ignored")]
    public async Task Given_NoDocument_When_MarkingDeleted_Then_TombstoneInsertedAndOlderCreateIgnored()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var deleted = await _store.MarkDeletedAsync(id, version: 2);
        var created = await _store.UpsertAsync(Snapshot(id, 5, "Late", 10m), version: 1);
        var raw = await _fixture.RawCollection()
            .Find(new BsonDocument("_id", new BsonBinaryData(id, GuidRepresentation.Standard)))
            .FirstOrDefaultAsync();

        // Assert
        Assert.True(deleted);
        Assert.False(created);
        Assert.Null(await _store.GetAsync(id));
        Assert.True(raw["IsDeleted"].AsBoolean);
        Assert.Equal(2L, raw["Version"].AsInt64);
    }

    /// <summary>
    /// Tests the three Like forms and the escaped underscore against the same rows PostgreSQL would match
    /// (Review Focus 3).
    /// </summary>
    [Fact(DisplayName = "Given a Like filter When listing Then matches prefix, suffix, contains, and escaped characters")]
    public async Task Given_LikeFilter_When_Listing_Then_MatchesLikePostgres()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await SeedAsync(marker, ("Acme Market", 10m), ("Zeta Market", 20m), ("Acme Store", 30m), ("a_b", 40m), ("axb", 50m));

        // Act
        var prefix = await ListAsync(Like($"{marker} acme%"));
        var suffix = await ListAsync(Like($"{marker}%market"));
        var contains = await ListAsync(Like($"%{marker}%"));
        var underscore = await ListAsync(Like($"{marker} a\\_b"));
        var dot = await ListAsync(Like($"{marker} a.b"));

        // Assert
        Assert.Equal(2, prefix.TotalCount);
        Assert.Equal(2, suffix.TotalCount);
        Assert.Equal(5, contains.TotalCount);
        Assert.Equal(1, underscore.TotalCount);
        Assert.Equal("a_b", underscore.Items[0].CustomerName[(marker.Length + 1)..]);
        Assert.Equal(0, dot.TotalCount);
    }

    /// <summary>
    /// Tests equality, day, and range filters together with the marker.
    /// </summary>
    [Fact(DisplayName = "Given equal, day, and range filters When listing Then narrows like the repositories")]
    public async Task Given_EqualDayAndRangeFilters_When_Listing_Then_Narrows()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        var customerId = Guid.NewGuid();
        var day = new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc);
        await _store.UpsertAsync(Snapshot(Guid.NewGuid(), 10, $"{marker} one", 10m, customerId, day.AddHours(10)), 1);
        await _store.UpsertAsync(Snapshot(Guid.NewGuid(), 11, $"{marker} two", 50m, customerId, day.AddDays(-1).AddHours(10)), 2);
        await _store.UpsertAsync(Snapshot(Guid.NewGuid(), 12, $"{marker} three", 90m, Guid.NewGuid(), day.AddHours(23)), 3);

        // Act
        var byCustomer = await ListAsync(Like($"{marker}%"), new FieldFilter("CustomerId", FilterOperator.Equal, customerId));
        var onDay = await ListAsync(Like($"{marker}%"), new FieldFilter("SaleDate", FilterOperator.OnDay, day));
        var inRange = await ListAsync(Like($"{marker}%"),
            new FieldFilter("TotalAmount", FilterOperator.GreaterThanOrEqual, 30m),
            new FieldFilter("TotalAmount", FilterOperator.LessThanOrEqual, 60m));

        // Assert
        Assert.Equal(2, byCustomer.TotalCount);
        Assert.Equal(2, onDay.TotalCount);
        Assert.Equal(new[] { 50m }, inRange.Items.Select(sale => sale.TotalAmount));
    }

    /// <summary>
    /// Tests the requested order, the default order, the page past the end, and the total count.
    /// </summary>
    [Fact(DisplayName = "Given three sales When listing with an order and pages Then orders, counts, and pages like the repositories")]
    public async Task Given_ThreeSales_When_ListingWithOrderAndPages_Then_OrdersCountsAndPages()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await SeedAsync(marker, ("one", 30m), ("two", 10m), ("three", 20m));

        // Act
        var byNumber = await ListAsync(Like($"{marker}%"));
        var byAmountDescending = await _store.ListAsync(new ListQuery
        {
            Filters = [Like($"{marker}%")], Order = [new SortField("TotalAmount", true)]
        });
        var secondPage = await _store.ListAsync(new ListQuery { Page = 2, Size = 2, Filters = [Like($"{marker}%")] });
        var pastEnd = await _store.ListAsync(new ListQuery { Page = 50, Size = 10, Filters = [Like($"{marker}%")] });

        // Assert
        Assert.True(byNumber.Items[0].SaleNumber < byNumber.Items[1].SaleNumber && byNumber.Items[1].SaleNumber < byNumber.Items[2].SaleNumber);
        Assert.Equal(new[] { 30m, 20m, 10m }, byAmountDescending.Items.Select(sale => sale.TotalAmount));
        Assert.Single(secondPage.Items);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Empty(pastEnd.Items);
        Assert.Equal(3, pastEnd.TotalCount);
    }

    /// <summary>
    /// Tests that ordering by a text field follows the linguistic order PostgreSQL gave (case and accents folded),
    /// not the byte order MongoDB uses without a collation (final review finding).
    /// </summary>
    [Fact(DisplayName = "Given mixed-case and accented names When ordering by customer name Then follows the linguistic order")]
    public async Task Given_MixedCaseAndAccentedNames_When_OrderingByCustomerName_Then_LinguisticOrder()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await SeedAsync(marker, ("Beta", 10m), ("zeta", 20m), ("acme", 30m), ("Álvaro", 40m));

        // Act
        var ordered = await _store.ListAsync(new ListQuery
        {
            Filters = [Like($"{marker}%")], Order = [new SortField("CustomerName", false)]
        });

        // Assert
        Assert.Equal(new[] { "acme", "Álvaro", "Beta", "zeta" }, ordered.Items.Select(sale => sale.CustomerName[(marker.Length + 1)..]));
    }

    private async Task SeedAsync(string marker, params (string Name, decimal Total)[] sales)
    {
        var version = 1;
        foreach (var (name, total) in sales)
            await _store.UpsertAsync(Snapshot(Guid.NewGuid(), Interlocked.Increment(ref _nextNumber), $"{marker} {name}", total), version++);
    }

    private Task<(IReadOnlyList<SaleSnapshot> Items, int TotalCount)> ListAsync(params FieldFilter[] filters) =>
        _store.ListAsync(new ListQuery { Page = 1, Size = 10, Filters = filters });

    private static FieldFilter Like(string pattern) => new("CustomerName", FilterOperator.Like, pattern);

    private static SaleSnapshot Snapshot(Guid id, long number, string customerName, decimal total, Guid? customerId = null, DateTime? saleDate = null) => new(
        id, number, saleDate ?? new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc),
        customerId ?? Guid.NewGuid(), customerName, Guid.NewGuid(), "Downtown", total, false,
        [new SaleSnapshotItem(Guid.NewGuid(), 1, Guid.NewGuid(), "Beer", 10.05m, 5, 10m, 5.03m, total, false, null, Guid.NewGuid(), 10m)]);
}
