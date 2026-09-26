# Trace console

Runs a documented flow with every step printed. Back to the [README](../../README.md).

The `t` command of `tools/Ambev.DeveloperEvaluation.DevConsole` runs a documented flow ([`docs/`](../INDEX.md)) with every step printed. It hosts the API inside its own process, so the request, the Rebus worker, the outbox relay, and the event consumer print into one window. Each line carries the time, the thread, the step key (a shared line carries both keys), the step title, the values that decided the path, and the source file and line:

```
17:48:57.538726  T022  SAL-CRT-04 CMN-PIP-10  Validate the command  presetId=null valid=True errors=0  CreateSaleHandler.cs:74
```

**It wipes the development data.** Before hosting the API it drops the PostgreSQL database named in `ConnectionStrings:DefaultConnection` (the API then recreates the schema and reseeds the administrator, [README §4](../../README.md#4-database-schema-and-the-administrator)) the MongoDB queue database named in `ConnectionStrings:MessageBus`, and the MongoDB read model database named in `ReadModel:Database`. The log database is untouched. It asks for confirmation unless `--yes` is given. The drop is a plain `DROP DATABASE` over a connection to the maintenance database `Trace:MaintenanceDatabase` and is never forced: while another session holds the database, the console prints the command that stops the API container and exits 1. Stop the API container and any `dotnet run` of the WebApi first.

```bash
docker compose stop ambev.developerevaluation.webapi
docker compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole            # asks t or l, the scenario, then Continue? (y/n)
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole -- t sale-async --yes
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole -- t all --yes > trace.txt
```

**To keep the development data**, point the run at scratch databases. The hosted API creates them, the next run drops them again, and the API container can keep running because it holds neither. The hosted API still writes its log lines (Warning and above, `Trace:AppLogMinimumLevel`) to the log database named in `ConnectionStrings:LogStorage`:

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole -- t all --yes \
  --ConnectionStrings:DefaultConnection="Host=localhost;Port=5433;Database=trace_scratch;Username=developer;Password=ev@luAt10n" \
  --ConnectionStrings:MessageBus="mongodb://developer:ev%40luAt10n@localhost:27017/trace_scratch_bus?authSource=admin" \
  --ReadModel:Database=trace_scratch_read \
  > trace.txt 2>&1; echo "exit $?"
tail -3 trace.txt          # a good run ends with ===== no unexpected misses
grep -n '^!!!' trace.txt   # a good run prints nothing: no scenario failed and no wait timed out
```

A good run shows `exit 0`, ends with `===== no unexpected misses`, and has no `!!!` line. The scratch databases stay after the run so you can inspect the data. Drop them, and delete `trace.txt`, when you no longer need them:

```bash
docker compose exec -T ambev.developerevaluation.database psql -U developer -d postgres -c 'DROP DATABASE IF EXISTS trace_scratch'
docker compose exec -T ambev.developerevaluation.nosql mongosh -u developer -p 'ev@luAt10n' --authenticationDatabase admin --quiet --eval 'db.getSiblingDB("trace_scratch_bus").dropDatabase()'
docker compose exec -T ambev.developerevaluation.nosql mongosh -u developer -p 'ev@luAt10n' --authenticationDatabase admin --quiet --eval 'db.getSiblingDB("trace_scratch_read").dropDatabase()'
rm trace.txt
```

The console reads the API's configuration in the API's order (the WebApi `appsettings.json`, `appsettings.Development.json`, the WebApi user secrets, environment variables), then its own `appsettings.json`, then the command line. It forwards the command-line pairs, the resolved connection strings, and the `Seed:Admin` credentials to the hosted API, so the wipe, the login, and the API always use the same databases and administrator.

Scenarios: `conventions`, `auth`, `users`, `customers`, `branches`, `products`, `discount-policy`, `sale-create`, `sale-async`, `sale-update`, `sale-delete`, `sale-list`, `sale-discount`, `failures`, and `all`.

- Each request and response is printed with `password`, `token`, and a rejected password's `attemptedValue` and `formattedMessagePlaceholderValues` masked as `***`; a body that looks like JSON but does not parse prints as `[unparsed body]`.
- A failing scenario prints `!!! scenario <name> failed: ...`; the run goes on with the next scenario and exits 1.
- The run ends with the distinct keys seen and, for `all`, the documented keys that were not exercised. None is expected: `===== no unexpected misses` means every documented step ran.
- `sale-discount` walks the same sales rules as the functional tests ([README §7](../../README.md#7-automated-tests)) and prints, for each case, whether the discount or the status matched; a mismatch is a `!!!` line.
- `failures` provokes the failure and redelivery paths on purpose, so its error log lines are expected. It runs the seed again, writes to the outbox outside a transaction, sends a stored sale's command and its `SaleCreated` again, adds an outbox row of an unknown type and deletes it, sends an event that skipped the relay (it ends in the error queue after 5 deliveries), and arms two faults that exist only in the trace host: one failed relay cycle and one unhandled exception (500).

| Key | Default | Meaning |
|---|---|---|
| `Trace:AppLogMinimumLevel` | `Warning` | Level of the hosted API's own log lines in the same window. |
| `Trace:WaitTimeout` | `00:00:30` | How long a scenario waits for an asynchronous step (worker, relay, consumer). |
| `Trace:MaintenanceDatabase` | `postgres` | PostgreSQL database the console connects to for the drop. |

The settings are in the console's `appsettings.json`; override them with `--Trace:<Key>=<value>`. Every setting is required.

The trace exists only in Debug builds: in Release the `StepTrace` calls are compiled out and the `t` command refuses to run.
