# Configuration reference

Every setting the API reads, and how to override it. Back to the [README](../../README.md).

Configuration comes from `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, overridden by environment variables. An environment variable replaces `:` with `__`; for example, `ConnectionStrings:MessageBus` becomes `ConnectionStrings__MessageBus`. Every key below is **required**. A missing or invalid key stops the API at startup with a message that names the key; there are no silent defaults.

| Key | Value in `appsettings.json` | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL on `localhost:5433` | Npgsql format. Append `;Maximum Pool Size=N` to change the pool size (default 100). |
| `Jwt:SecretKey` | Development key | At least 32 bytes. Replace it outside development. |
| `ConnectionStrings:LogStorage` | MongoDB on `localhost:27017` | MongoDB URL for the log sink. Percent-encode `@` in passwords as `%40`. |
| `LogStorage:Database` / `Collection` | `developer_evaluation_logs` / `logs` | Where Serilog stores events. |
| `LogStorage:ExpireAfter` | `1.00:00:00` | Log TTL, as a .NET time span. |
| `ConnectionStrings:MessageBus` | `mongodb://…/developer_evaluation_bus?authSource=admin` | MongoDB URL of the Rebus queue. The path **must** name a database, and it cannot be the `LogStorage` database. |
| `Rebus:InputQueue` | `sales-intake` | Queue the API sends to and consumes from. |
| `Rebus:Workers` | `1` | Rebus worker threads. |
| `Rebus:MaxParallelism` | `20` | Queued messages processed at the same time. Keep it **below** the Npgsql pool size. |
| `Outbox:PollingInterval` | `00:00:00.500` | Wait between outbox relay cycles; after a full batch the next cycle starts at once. A time span (hh:mm:ss) from `00:00:00.1` to `01:00:00`; a bare number is rejected, since .NET reads it as days. |
| `Outbox:BatchSize` | `50` | Outbox rows one relay cycle sends. |
| `ConnectionStrings:ReadModel` | MongoDB on `localhost:27017` | MongoDB URL of the sales read model (no database in the path; the database is `ReadModel:Database`). |
| `ReadModel:Database` / `Collection` | `developer_evaluation_read` / `sales` | Where the sale events are projected and where `GET /api/sales` reads. |
| `Seed:Admin:Username` / `Email` / `Password` / `Phone` | `admin` / `admin@example.com` / `Adm1n@Pass` / `+5511999990000` | The administrator created at startup when no user has that e-mail ([README §4](../../README.md#4-database-schema-and-the-administrator)). The values must pass the user rules, or the API does not start. Replace the password outside development. |
| `Serilog` section | Levels, console output, `/health` filter | EF Core and ASP.NET Core log at Warning, so SQL commands do not flood the log. |

The API container overrides `DefaultConnection`, `LogStorage`, `MessageBus`, and `ReadModel` in `docker-compose.yml` so they point at the service names instead of `localhost`.

**MongoDB is required at startup.** The Rebus transport creates its queue index when the bus starts, so the whole API, synchronous endpoints included, does not start while MongoDB is unreachable.

**Changing a port.** If you publish PostgreSQL or MongoDB on another host port, change the matching connection strings in `appsettings.json` too (they are used by `dotnet run`, `dotnet ef`, and the integration tests). The container connection strings use the internal service ports and do not change.

**Example: a bigger pool and more consumers for one run of the host API.**

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=developer_evaluation;Username=developer;Password=ev@luAt10n;Maximum Pool Size=200" \
Rebus__MaxParallelism=50 \
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi --launch-profile http
```
