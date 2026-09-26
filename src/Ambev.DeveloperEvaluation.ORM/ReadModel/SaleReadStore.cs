using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Ambev.DeveloperEvaluation.ORM.ReadModel;

// Work item: TASK-075 (FEAT-003)
/// <summary>
/// Implementation of ISaleReadStore over one MongoDB collection of <see cref="SaleDocument"/>. Each write is a
/// server operation whose filter carries the version check, so two events of the same sale handled at the same
/// time cannot leave the older state. A filter that does not match makes the upsert try to insert a second
/// document with the same id, which the server refuses with a duplicate key; that refusal means either "older
/// event" or "another handler inserted the document a moment ago", so the write is retried once without upsert:
/// a newer event then matches and wins, an older one matches nothing and is reported as ignored. That reading of the
/// duplicate key holds because _id is the collection's only unique index; a unique index on another field would
/// need this path revisited. Lists run under an English collation so text order matches PostgreSQL's (case and
/// accents folded), not the byte order MongoDB uses by default.
/// </summary>
public class SaleReadStore : ISaleReadStore
{
    private static readonly SortField[] DefaultOrder = [new("SaleNumber", false)];

    // PostgreSQL orders text linguistically (en_US.utf8); without a collation MongoDB compares bytes, which puts
    // every lowercase name after every uppercase one and accented names after "z".
    private static readonly AggregateOptions ListOptions = new() { Collation = new Collation("en") };

    private readonly IMongoCollection<SaleDocument> _collection;

    /// <summary>
    /// Initializes a new instance of SaleReadStore
    /// </summary>
    /// <param name="client">The read model client</param>
    /// <param name="settings">The database and collection names</param>
    public SaleReadStore(IMongoClient client, ReadModelSettings settings)
    {
        _collection = client.GetDatabase(settings.Database).GetCollection<SaleDocument>(settings.Collection);
    }

    /// <inheritdoc />
    public async Task<bool> UpsertAsync(SaleSnapshot snapshot, long version, CancellationToken cancellationToken = default)
    {
        var filter = OlderThan(snapshot.SaleId, version);
        var document = SaleDocument.From(snapshot, version);
        try
        {
            await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var retry = await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = false }, cancellationToken);
            return retry.MatchedCount > 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkDeletedAsync(Guid saleId, long version, CancellationToken cancellationToken = default)
    {
        var tombstone = Builders<SaleDocument>.Update
            .Set(document => document.IsDeleted, true)
            .Set(document => document.Version, version);
        var filter = OlderThan(saleId, version);
        try
        {
            await _collection.UpdateOneAsync(filter, tombstone, new UpdateOptions { IsUpsert = true }, cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var retry = await _collection.UpdateOneAsync(filter, tombstone, new UpdateOptions { IsUpsert = false }, cancellationToken);
            return retry.MatchedCount > 0;
        }
    }

    /// <inheritdoc />
    public async Task<SaleSnapshot?> GetAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var document = await _collection
            .Find(document => document.Id == saleId && !document.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToSnapshot();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<SaleSnapshot> Items, int TotalCount)> ListAsync(ListQuery query, CancellationToken cancellationToken = default)
    {
        var rows = _collection.AsQueryable(ListOptions)
            .Where(document => !document.IsDeleted)
            .ApplyFilters(query.Filters, MongoLike.Translate)
            .ApplyOrder(query.Order, DefaultOrder);
        var totalCount = checked((int)await rows.LongCountAsync(cancellationToken));
        var offset = (long)(query.Page - 1) * query.Size;
        StepTrace.Step("CMN-LST-09", "Filter, order with Id as tiebreak, count, then page",
            [("filters", query.Filters.Count), ("sortFields", query.Order.Count), ("total", totalCount), ("page", query.Page), ("size", query.Size),
             ("pastEnd", offset >= totalCount)]);
        if (offset >= totalCount)
            return ([], totalCount);

        var documents = await rows.Skip((int)offset).Take(query.Size).ToListAsync(cancellationToken);
        return (documents.Select(document => document.ToSnapshot()).ToList(), totalCount);
    }

    // The filter of every write: this sale, and only while the stored version is older than the event's.
    private static FilterDefinition<SaleDocument> OlderThan(Guid saleId, long version) =>
        Builders<SaleDocument>.Filter.Eq(document => document.Id, saleId) &
        Builders<SaleDocument>.Filter.Lt(document => document.Version, version);
}
