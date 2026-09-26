# DeveloperStore Sales API — Step-by-Step Guide

This guide takes you from a fresh clone to a running platform, then through configuration, the trace console, and the load test that compares the synchronous and asynchronous ways of creating sales. The original challenge statement is in [CHALLENGE.md](CHALLENGE.md), and the API conventions are in [`.doc/`](.doc/).

**Contents**

1. [What runs where](#1-what-runs-where)
2. [Prerequisites](#2-prerequisites)
3. [Start the containers with make](#3-start-the-containers-with-make)
4. [Database schema and the administrator](#4-database-schema-and-the-administrator)
5. [Choose how to run the API](#5-choose-how-to-run-the-api)
6. [First requests, end to end](#6-first-requests-end-to-end)
7. [Configuration reference](#7-configuration-reference)
8. [Asynchronous sale intake](#8-asynchronous-sale-intake)
9. [Trace console](#9-trace-console)
10. [Load test: synchronous vs asynchronous](#10-load-test-synchronous-vs-asynchronous)
11. [Logs and observability](#11-logs-and-observability)
12. [Automated tests](#12-automated-tests)
13. [Stop, restart, and reset](#13-stop-restart-and-reset)
14. [Troubleshooting](#14-troubleshooting)
15. [Repository layout](#15-repository-layout)
16. [Branching and pull requests](#16-branching-and-pull-requests)

---

## 1. What runs where

| Component | Technology | Role | Host port |
|---|---|---|---|
| API | ASP.NET Core 8 (`src/Ambev.DeveloperEvaluation.WebApi`) | REST API, Swagger, JWT auth | 8080 (container) or 5119 (`dotnet run`) |
| PostgreSQL 13 | compose service `ambev.developerevaluation.database` | Users, customers, branches, products, sales | 5433 (5432 inside the container) |
| MongoDB 8 | compose service `ambev.developerevaluation.nosql` | Application logs (`developer_evaluation_logs`), the Rebus sale queue (`developer_evaluation_bus`), and the sales read model (`developer_evaluation_read`) | 27017 |
| Redis 7 | compose service `ambev.developerevaluation.cache` | Started by the stack; not used by the code yet | 6380 (6379 inside the container) |
| Developer console | console app (`tools/Ambev.DeveloperEvaluation.DevConsole`) | Traced scenarios against the API hosted in process, and the load simulator | — |

PostgreSQL keeps its data in the named volume `postgres-data` and MongoDB in `mongo-data`, so both survive `docker compose down`.

## 2. Prerequisites

| Tool | Version | Check |
|---|---|---|
| Docker with Compose v2 | Docker Desktop, OrbStack, or Docker Engine | `docker compose version` |
| .NET SDK | 8.0 (newer SDKs also work) | `dotnet --list-sdks` |
| EF Core CLI | any recent `dotnet-ef` | `dotnet ef --version` |
| make | GNU make (preinstalled on macOS and most Linux distributions) | `make --version` |
| curl and python3 | used by the examples in this guide | `curl --version` |

Install the EF Core CLI if it is missing:

```bash
dotnet tool install --global dotnet-ef
```

The stack publishes PostgreSQL on 5433 and Redis on 6380 (not the defaults 5432 and 6379, to avoid colliding with other local instances), and the defaults 27017 and 8080. Stop anything else that listens on them, or change the published ports in `docker-compose.yml` together with the matching connection strings (see [§7](#7-configuration-reference)).

## 3. Start the containers with make

All `make` targets run from the **repository root**.

**Step 1: build the API image.** `make dev-up` builds the image only when none exists. After you pull new code, rebuild it explicitly:

```bash
docker compose build ambev.developerevaluation.webapi
```

**Step 2: start the stack.**

```bash
make dev-up
```

This starts PostgreSQL, MongoDB, Redis, and the API container, then prints where each one listens:

```
Postgres  0.0.0.0:5433
MongoDB   0.0.0.0:27017
Redis     0.0.0.0:6380
API       http://0.0.0.0:8080/swagger
```

**Step 3: check the containers.**

```bash
docker compose ps
```

All four services should be `running`. If the API container is `exited`, see [§14](#14-troubleshooting): the API does not start while MongoDB is still initializing.

To start only the databases, for example when you run the API with `dotnet run`, use:

```bash
docker compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql
```

## 4. Database schema and the administrator

Nothing to run by hand. Every time the API starts, before it listens, it applies the pending migrations and then creates the administrator from `Seed:Admin` ([§7](#7-configuration-reference)) unless a user already has that e-mail. After new migrations arrive or the `postgres-data` volume is recreated, start the API again. The API container waits for the PostgreSQL healthcheck before it starts.

To apply the migrations without starting the API:

```bash
dotnet ef database update \
  --project src/Ambev.DeveloperEvaluation.ORM \
  --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

The command reads `ConnectionStrings:DefaultConnection` from `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, which already points at the compose PostgreSQL on `localhost:5433`.

## 5. Choose how to run the API

Run **one** API instance at a time. Every instance consumes the same MongoDB queue (`sales-intake`), so two instances split the queued sales between them and distort any measurement. Each instance also runs an outbox relay ([§8](#8-asynchronous-sale-intake)), which assumes it is the only one: two relays can send the same sale events twice and out of order.

### Option A — API in the container (port 8080)

`make dev-up` already started it. Open **http://localhost:8080/swagger**.

Its configuration comes from `appsettings.json` plus the `environment` block of the API service in `docker-compose.yml`, which points the connection strings at the other containers.

### Option B — API on the host with `dotnet run` (port 5119)

Stop the API container first, then run the API from the host:

```bash
docker compose stop ambev.developerevaluation.webapi
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi --launch-profile http
```

Open **http://localhost:5119/swagger**. This option picks up code changes without rebuilding an image.

### Health checks

| Route | Meaning |
|---|---|
| `GET /health/live` | The process is up |
| `GET /health/ready` | Readiness |
| `GET /health` | Overall health |

## 6. First requests, end to end

The examples use the container API. For Option B, set `BASE=http://localhost:5119`.

```bash
BASE=http://localhost:8080
```

**Step 1: log in as the administrator and keep the token.** The API created it at startup from `Seed:Admin` ([§4](#4-database-schema-and-the-administrator)); the credentials below are the development values in `appsettings.json`.

```bash
TOKEN=$(curl -s -X POST $BASE/api/auth -H 'Content-Type: application/json' \
  -d '{"email":"admin@example.com","password":"Adm1n@Pass"}' \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['token'])")
AUTH="Authorization: Bearer $TOKEN"
```

**Step 2 (optional): create more users.** There is no anonymous sign-up: every `/api/users` call needs an Admin or Manager token. `role`: 1 = Customer, 2 = Manager, 3 = Admin. `status`: 1 = Active. Write endpoints require the Admin or Manager role; a Customer can only read.

```bash
curl -s -X POST $BASE/api/users -H "$AUTH" -H 'Content-Type: application/json' -d '{
  "username": "manager", "password": "Str0ng@Pass", "phone": "+5511999998888",
  "email": "manager@example.com", "status": 1, "role": 2 }'
```

The password needs at least 8 characters, with an uppercase letter, a lowercase letter, a digit, and one of `! ? * . @ # $ % ^ & + =`.

**No control over user roles.** Admin and Manager have the same powers everywhere. Either one can create a user with any role, Admin included, and delete any user, Admin included; nothing stops a Manager from promoting someone to Admin.

To use Swagger, copy `data.token` from the login response, click **Authorize** at the top of the Swagger page, and paste the token without the `Bearer ` prefix; every request from Swagger then carries it. The steps below use curl, which sends the `Authorization: Bearer <token>` header itself.

**Step 3: register a customer, a branch, and a product.** Customers need a valid CPF or CNPJ, and it must be unique. Product codes are unique too.

```bash
id() { python3 -c "import sys,json;print(json.load(sys.stdin)['data']['id'])"; }
CUSTOMER=$(curl -s -X POST $BASE/api/customers -H "$AUTH" -H 'Content-Type: application/json' \
  -d '{"name":"Acme Market","document":"52998224725"}' | id)
BRANCH=$(curl -s -X POST $BASE/api/branches -H "$AUTH" -H 'Content-Type: application/json' \
  -d '{"name":"Downtown"}' | id)
PRODUCT=$(curl -s -X POST $BASE/api/products -H "$AUTH" -H 'Content-Type: application/json' \
  -d '{"code":"BEER-350","description":"Beer 350ml","unitPrice":5}' | id)
```

**Step 4: create a sale synchronously.** The response is `201` with the stored sale. The server prices the items from the discount policies (the seeded default policy is the challenge rule: 4 to 9 units of one product get 10%, 10 to 20 get 20%, more than 20 is refused), so the body carries no amounts; send `discountPercentage` on an item only to ask for less than the ceiling.

```bash
SALE="{\"customerId\":\"$CUSTOMER\",\"branchId\":\"$BRANCH\",
  \"items\":[{\"productId\":\"$PRODUCT\",\"quantity\":4}]}"
curl -s -X POST $BASE/api/sales -H "$AUTH" -H 'Content-Type: application/json' -d "$SALE"
```

Every error, from any endpoint, has the body of [`.doc/general-api.md`](.doc/general-api.md): `type` is the category, `error` the code of the first failure, and `detail` a JSON array of messages serialized as a string, each prefixed with the field or line it belongs to. Asking for 50% on four units, above the 10% ceiling, gives:

```bash
curl -s -X POST $BASE/api/sales -H "$AUTH" -H 'Content-Type: application/json' -d "{\"customerId\":\"$CUSTOMER\",\"branchId\":\"$BRANCH\",
  \"items\":[{\"productId\":\"$PRODUCT\",\"quantity\":4,\"discountPercentage\":50}]}"
```

```
{"type":"ValidationError","error":"DiscountAboveAllowed","detail":"[\"Items[0].DiscountPercentage: Requested discount 50% exceeds 10% allowed for 4 units\"]"}
```

A request without a token answers `401` with `type` `AuthenticationError`; a `Customer` token on a write answers `403` with `AuthorizationError`.

**Discount rules per product and branch.** The challenge rules are the seeded default policy, which covers every product and every branch. A policy can also be scoped to a product, a branch, or both, with its own maximum and tiers. Each sale is priced, per product, by the most specific policy in effect at its sale date: product and branch, then product, then branch, then the default. A policy cannot start in the past, so this one starts ten seconds from now and gives the product of step 3 up to 50 units, with 30% from 12 units:

```bash
FROM=$(date -u -v+10S +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u -d '+10 seconds' +%Y-%m-%dT%H:%M:%SZ)   # macOS, then Linux
curl -s -X POST $BASE/api/discount-policies -H "$AUTH" -H 'Content-Type: application/json' -d "{\"productId\":\"$PRODUCT\",\"branchId\":null,
  \"validFrom\":\"$FROM\",\"maxQuantityPerProduct\":50,\"tiers\":[{\"minQuantity\":12,\"maxQuantity\":null,\"percentage\":30}]}"
sleep 10
curl -s -X POST $BASE/api/sales -H "$AUTH" -H 'Content-Type: application/json' -d "{\"customerId\":\"$CUSTOMER\",\"branchId\":\"$BRANCH\",
  \"items\":[{\"productId\":\"$PRODUCT\",\"quantity\":30}]}"
```

The sale is accepted with 30 units at 30%, and its item carries the new policy's id in `discountPolicyId`; every other product keeps the challenge rules. A policy is never edited: a newer one of the same scope wins once it starts, and `POST /api/discount-policies/disable` ends one from that moment on, while sales dated before keep being priced by it. The rules are in [docs/discount-policies.md](docs/discount-policies.md).

**Step 5: create a sale asynchronously.** Add `Prefer: respond-async`. The response is `202 Accepted` with the id the sale will be stored under:

```bash
curl -s -i -X POST $BASE/api/sales -H "$AUTH" -H 'Content-Type: application/json' \
  -H 'Prefer: respond-async' -d "$SALE"
```

```
HTTP/1.1 202 Accepted
Location: http://localhost:8080/api/Sales/<id>
Preference-Applied: respond-async

{"data":{"id":"<id>"},"success":true,"message":"Sale sent for processing","errors":[]}
```

**Step 6: follow the queued sale.** Poll the `Location` URL. It answers `404` while the sale is queued and `200` once it is stored:

```bash
curl -s -o /dev/null -w '%{http_code}\n' -H "$AUTH" $BASE/api/sales/<id>
```

`GET /api/sales/{id}` and the sale list read from the MongoDB read model, which the sale events fill within `Outbox:PollingInterval` (half a second) of the write; a `GET` fired in that window answers 404, and `saleDate` comes back with millisecond precision (BSON dates), so its last digits can differ from the POST response.

**Step 7: list and filter.** List endpoints follow [`.doc/general-api.md`](.doc/general-api.md):

| Parameter | Meaning |
|---|---|
| `_page`, `_size` | Paging; `_size` goes from 1 to 100 |
| `_order` | Sort order, for example `saleNumber desc` |
| Any response field | Filter; `*` works at the start or end of text |
| `_min<Field>`, `_max<Field>` | Ranges on numeric and date fields |

```bash
curl -s -H "$AUTH" "$BASE/api/sales?_page=1&_size=5&_order=saleNumber%20desc"
curl -s -H "$AUTH" "$BASE/api/customers?name=Acme*"
```

## 7. Configuration reference

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
| `Seed:Admin:Username` / `Email` / `Password` / `Phone` | `admin` / `admin@example.com` / `Adm1n@Pass` / `+5511999990000` | The administrator created at startup when no user has that e-mail ([§4](#4-database-schema-and-the-administrator)). The values must pass the user rules, or the API does not start. Replace the password outside development. |
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

## 8. Asynchronous sale intake

The internals (queue, transactional outbox, relay, and consumers, with diagrams and step keys) are documented in [docs/sales.md](docs/sales.md).

`POST /api/sales` has two modes:

| Request | What happens | Response |
|---|---|---|
| No `Prefer` header, or a header without `respond-async` | The sale is validated and stored in the request's own transaction | `201` with the sale and a `Location` pointing at it, or `400` |
| `Prefer: respond-async` | The body is validated, the id is generated, and the command is queued in MongoDB | `202` with `{ id }`, `Location`, and `Preference-Applied`, or `400` |

How a queued sale is processed:

- A Rebus consumer inside the API runs the command through the same pipeline as the synchronous endpoint, with at most `Rebus:MaxParallelism` sales at a time.
- **Idempotent.** If the same message is delivered twice, the second delivery finds the sale already stored and writes nothing.
- **Fail fast on bad references.** An unknown customer, branch, or product goes straight to the Rebus **error queue** after a single attempt. Other failures, such as a database outage, get up to 5 delivery attempts in total and then move to the error queue.
- **What the client sees.** It follows the sale with `GET /api/sales/{id}`: `404` means still queued or rejected, `200` means stored. Rejected sales are visible only in the error queue and the log.

### Sale events

Every sale create, update, and delete records its events (`SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled`, `SaleDeleted`) in the `OutboxMessages` table, inside the same transaction as the write:

- A write that rolls back leaves no event, and a committed write always leaves its events.
- A relay inside the API sends pending rows, oldest first, to the same Rebus queue.
- `SaleEventLogHandler` writes one line per event to the log: `Sale event <Type> <MessageId> for sale <SaleId>`.
- `SaleProjectionHandler` projects `SaleCreated`, `SaleModified`, and `SaleDeleted` into the MongoDB read model that `GET /api/sales` reads, ordered by the outbox sequence so a late or repeated event never overwrites a newer state.
- **Delivery is at least once.** The message id is the outbox row id, so a re-sent event keeps its id.
- **Consumers may process events in any order**, because they run in parallel. The order is guaranteed only for how events leave the outbox.
- A queued sale that is delivered again after it was stored produces no second `SaleCreated`.

See which events are still waiting in the outbox:

```bash
docker exec ambev_developer_evaluation_database psql -U developer -d developer_evaluation \
  -c 'select "Type", "OccurredAt" from "OutboxMessages" where "ProcessedAt" is null order by "Sequence";'
```

**Inspect the queue and the error queue:**

```bash
docker exec -it ambev_developer_evaluation_nosql mongosh -u developer -p 'ev@luAt10n' \
  --authenticationDatabase admin developer_evaluation_bus
```

```javascript
db.messages.countDocuments({ q: "sales-intake" })          // waiting: queued sales and sale events together
db.messages.countDocuments({ q: "error" })                 // failed
db.messages.find({ q: "sales-intake", n: { $gte: 5 } })    // claimed 5 times and never moved to "error"
```

**Inspect the read model** (`use developer_evaluation_read` in the same shell):

```javascript
db.sales.find({}, { _id: 1, SaleNumber: 1, Version: 1, IsDeleted: 1 }).sort({ SaleNumber: -1 }).limit(20)
```

**Known limitations:**

- Retries have no backoff.
- The MongoDB transport claims a message at most 5 times. Claims that Rebus does not count as errors, such as a restart mid-processing, still use up those 5 claims, so a message can get stuck, which the last query above finds.
- Update and delete are synchronous only.
- The read model is not rebuilt from PostgreSQL. Sales stored before this version, or while MongoDB was down for the projection, are missing from `GET /api/sales`; `docker compose down -v` and a fresh start recreate everything.

## 9. Trace console

The `t` command of `tools/Ambev.DeveloperEvaluation.DevConsole` runs a documented flow ([`docs/`](docs/INDEX.md)) with every step printed. It hosts the API inside its own process, so the request, the Rebus worker, the outbox relay, and the event consumer print into one window. Each line carries the time, the thread, the step key (a shared line carries both keys), the step title, the values that decided the path, and the source file and line:

```
17:48:57.538726  T022  SAL-CRT-04 CMN-PIP-10  Validate the command  presetId=null valid=True errors=0  CreateSaleHandler.cs:74
```

**It wipes the development data.** Before hosting the API it drops the PostgreSQL database named in `ConnectionStrings:DefaultConnection` (the API then recreates the schema and reseeds the administrator, [§4](#4-database-schema-and-the-administrator)) the MongoDB queue database named in `ConnectionStrings:MessageBus`, and the MongoDB read model database named in `ReadModel:Database`. The log database is untouched. It asks for confirmation unless `--yes` is given. The drop is a plain `DROP DATABASE` over a connection to the maintenance database `Trace:MaintenanceDatabase` and is never forced: while another session holds the database, the console prints the command that stops the API container and exits 1. Stop the API container and any `dotnet run` of the WebApi first.

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
- `sale-discount` walks the same sales rules as the functional tests ([§12](#12-automated-tests)) and prints, for each case, whether the discount or the status matched; a mismatch is a `!!!` line.
- `failures` provokes the failure and redelivery paths on purpose, so its error log lines are expected. It runs the seed again, writes to the outbox outside a transaction, sends a stored sale's command and its `SaleCreated` again, adds an outbox row of an unknown type and deletes it, sends an event that skipped the relay (it ends in the error queue after 5 deliveries), and arms two faults that exist only in the trace host: one failed relay cycle and one unhandled exception (500).

| Key | Default | Meaning |
|---|---|---|
| `Trace:AppLogMinimumLevel` | `Warning` | Level of the hosted API's own log lines in the same window. |
| `Trace:WaitTimeout` | `00:00:30` | How long a scenario waits for an asynchronous step (worker, relay, consumer). |
| `Trace:MaintenanceDatabase` | `postgres` | PostgreSQL database the console connects to for the drop. |

The settings are in the console's `appsettings.json`; override them with `--Trace:<Key>=<value>`. Every setting is required.

The trace exists only in Debug builds: in Release the `StepTrace` calls are compiled out and the `t` command refuses to run.

## 10. Load test: synchronous vs asynchronous

### What it measures

Under thousands of concurrent inserts, the synchronous endpoint holds one PostgreSQL connection per request for the whole transaction. Past the Npgsql pool size (100 by default), requests wait, and after 15 s they fail with `500`.

The asynchronous mode turns the spike into a queue: the client gets `202` in milliseconds, and the consumers store sales at the pace the database sustains. The asynchronous mode does **not** insert faster. The database is the same, so write throughput is similar. What changes is behavior above capacity.

### The simulator

The `l` command of the developer console (`tools/Ambev.DeveloperEvaluation.DevConsole`) talks to the API over HTTP only. Each run:

1. Logs in **once** as the administrator the API seeds ([§4](#4-database-schema-and-the-administrator)).
2. Registers one customer, one branch, and three products for the run.
3. Starts every loop of every profile at the same time. Each loop is a `while` that posts a three-item sale, times it, records the status code, and waits `PauseMilliseconds`. Concurrency equals the total number of loops.
4. Prints, per profile and in total:
   - latency p50/p95/p99;
   - the outcome of each request (status code, or the exception name when no response came back);
   - throughput over the whole run.
5. In async mode, also polls the sale count until every `202` is stored and reports the drain time, plus how many requests got no response.

### Simulator settings

The settings are in `tools/Ambev.DeveloperEvaluation.DevConsole/appsettings.json`. Any setting can be overridden on the command line with `--Simulator:<Key>=<value>`, and every setting is required.

| Key | Default | Meaning |
|---|---|---|
| `Simulator:BaseUrl` | `http://localhost:5119` | API URL. Use `http://localhost:8080` for the container. |
| `Simulator:Mode` | `sync` | `sync` or `async` (async sends `Prefer: respond-async`). |
| `Simulator:RequestsPerLoop` | `20` | Requests each loop sends. |
| `Simulator:Profiles` | fast 500 loops / 0 ms, medium 300 / 50 ms, slow 200 / 200 ms | Loop groups: `Name`, `Loops`, `PauseMilliseconds`. |
| `Simulator:DrainTimeout` | `00:10:00` | How long async mode waits for the queue to drain. |
| `Simulator:DrainPollInterval` | `00:00:01` | How often it counts stored sales. |
| `Simulator:RequestTimeout` | `00:01:40` | Client timeout per request; a timed-out request counts as unanswered. |

The simulator also reads the API's configuration, as the trace console does ([§9](#9-trace-console)), and logs in with its `Seed:Admin:Email` and `Seed:Admin:Password`; override them with `--Seed:Admin:Email=<value>` and `--Seed:Admin:Password=<value>` when the API runs with other values.

With the defaults, a run has 1,000 concurrent loops × 20 requests, which is 20,000 sales.

### Step by step

**Step 1: bring the platform up** ([§3](#3-start-the-containers-with-make); the API applies the schema itself, [§4](#4-database-schema-and-the-administrator)).

**Step 2: run exactly one API instance** ([§5](#5-choose-how-to-run-the-api)). For the most direct comparison, use Option B (`dotnet run`, port 5119, the simulator default) with the API container stopped.

**Step 3: decide the capacity settings.** Leave the defaults (pool 100, `MaxParallelism` 20) for the first run; change them later as shown in [§7](#7-configuration-reference). Keep `Rebus:MaxParallelism` below the pool size, otherwise the consumers can exhaust the pool themselves.

**Step 4: warm up with a small run** to check the setup:

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole -- l \
  --Simulator:Mode=sync --Simulator:RequestsPerLoop=3 \
  --Simulator:Profiles:0:Loops=5 --Simulator:Profiles:1:Loops=3 --Simulator:Profiles:2:Loops=2
```

Expected: `TOTAL: 30 requests, … outcomes: 201=30`.

**Step 5: run the synchronous scenario.**

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole -- l --Simulator:Mode=sync
```

**Step 6: run the asynchronous scenario.**

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole -- l --Simulator:Mode=async
```

Add `--Simulator:BaseUrl=http://localhost:8080` to both commands when the target is the API container.

**Step 7: raise the load until the synchronous mode breaks.** Increase `Loops` or `RequestsPerLoop` until the sync run shows `500` outcomes or unanswered requests, then repeat the same settings in async mode.

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.DevConsole -- l --Simulator:Mode=sync \
  --Simulator:Profiles:0:Loops=1500 --Simulator:RequestsPerLoop=30
```

### Reading the report

```
Run 6aea083f: sync mode, 1000 concurrent loops x 20 requests against http://localhost:5119/, request timeout 00:01:40
fast: 10000 requests, p50=… ms, p95=… ms, p99=… ms, outcomes: 201=…, 500=…
medium: …
slow: …
TOTAL: 20000 requests, … req/s over the whole run, p50=… ms, p95=… ms, p99=… ms, outcomes: …
```

In async mode two more lines follow:

```
Unanswered requests (timeout or network error): 0. …
Drain: all 20000 accepted sales stored 42.3 s after the load ended (61.8 s after it started).
```

| Signal | Synchronous mode | Asynchronous mode |
|---|---|---|
| `outcomes` | `201`; `500` once the pool is exhausted | `202` |
| p95/p99 | Grows with queueing for connections, up to the 15 s pool timeout | Stays in milliseconds (enqueue only) |
| Sales stored | As they are answered | After the drain; compare the total time with the sync run |

While a run is in progress, you can watch PostgreSQL connections:

```bash
docker exec ambev_developer_evaluation_database psql -U developer -d developer_evaluation \
  -c "select state, count(*) from pg_stat_activity where datname = 'developer_evaluation' group by state;"
```

### Caveats

- **Anything else that creates sales during the run distorts the drain count.** Keep other clients quiet.
- **An unanswered request may still have been queued.** When the report shows unanswered requests, the drain target can be reached before every accepted sale is stored.
- **PostgreSQL and MongoDB share the host disk.** The named volumes separate data, not I/O: on Docker Desktop or OrbStack every volume lives on the same VM disk. Absolute numbers are pessimistic compared with a server.
- **MongoDB holds both the queue and the logs.** Log writes and queue writes compete during the run.
- **Runs accumulate data.** Every run adds its user, catalog entries, and sales. Reset the data between series if you need clean numbers ([§13](#13-stop-restart-and-reset)).
- **The outbox has a cost.** Each sale write also stores its events, and the queue also carries them, so numbers are not comparable with runs made before the outbox existed. That difference is the outbox cost per sale.

## 11. Logs and observability

| Where | How |
|---|---|
| Container console | `docker compose logs -f ambev.developerevaluation.webapi` |
| Host console | the `dotnet run` terminal |
| MongoDB | database `developer_evaluation_logs`, collection `logs` (expires after `LogStorage:ExpireAfter`) |
| MongoDB read model | database `developer_evaluation_read`, collection `sales` (one document per sale, see [§8](#8-asynchronous-sale-intake)) |

Each HTTP request and each MediatR request logs one Information line. Rejections log a Warning with the exception type, and failures log an Error. To query the stored logs:

```bash
docker exec -it ambev_developer_evaluation_nosql mongosh -u developer -p 'ev@luAt10n' \
  --authenticationDatabase admin developer_evaluation_logs
```

```javascript
db.logs.find({}, { _id: 0, Level: 1, UtcTimeStamp: 1, RenderedMessage: 1 }).sort({ UtcTimeStamp: -1 }).limit(20)
db.logs.find({ Level: { $in: ["Warning", "Error"] } }).sort({ UtcTimeStamp: -1 }).limit(20)
db.logs.find({ RenderedMessage: /^Sale event/ }).sort({ UtcTimeStamp: -1 }).limit(20)
```

## 12. Automated tests

Run these from the repository root:

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit          # no infrastructure needed
dotnet test tests/Ambev.DeveloperEvaluation.Integration   # needs the compose PostgreSQL and MongoDB
dotnet test tests/Ambev.DeveloperEvaluation.Functional    # the challenge sales rules over HTTP; same infrastructure
./coverage-report.sh                                      # coverage report in TestResults/CoverageReport/index.html
```

The integration tests create a throwaway database for each test class, apply the migrations, and drop it afterwards, so they never touch `developer_evaluation`.

The functional tests host the whole API in process against throwaway PostgreSQL and MongoDB databases, dropped afterwards, and prove the challenge sales rules through HTTP with the seeded default policy: 1 to 3 identical items get no discount, 4 to 9 get 10%, 10 to 20 get 20%, more than 20 (on one line or across lines) answer 400 `QuantityLimitExceeded`, a requested discount above the tier answers 400 `DiscountAboveAllowed`, each product is priced on its own total, names and prices come from the catalog, cancelling a line reprices the others, a policy scoped to one product applies its own cap and tiers while the other products keep the challenge rules, and disabling the default policy leaves older sales editable. They also check that the API refuses to start without `ConnectionStrings:DefaultConnection`, naming the key.

## 13. Stop, restart, and reset

Run these from the repository root:

| Goal | Command | Data |
|---|---|---|
| Stop everything | `docker compose stop` | Kept |
| Start again | `make dev-up` from the repository root | Kept |
| Remove the containers | `docker compose down` | Kept (named volumes) |
| **Delete all data** | `docker compose down -v` | **PostgreSQL and MongoDB data erased.** The API recreates the schema and the administrator when it starts again ([§4](#4-database-schema-and-the-administrator)). |
| Rebuild the API image after a code change | `docker compose build ambev.developerevaluation.webapi && docker compose up -d ambev.developerevaluation.webapi` | Kept |

## 14. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| API container `exited` right after `make dev-up` | MongoDB was still initializing when the API started; the Rebus transport needs MongoDB at startup | `docker compose up -d ambev.developerevaluation.webapi` |
| API exits naming a key, for example `ConnectionStrings:MessageBus is not configured` | Missing or invalid configuration | Set the key named in the message ([§7](#7-configuration-reference)) |
| API exits at startup with a PostgreSQL connection error | PostgreSQL is unreachable; the API applies the migrations before it listens ([§4](#4-database-schema-and-the-administrator)) | Start PostgreSQL, then the API |
| `address already in use` on 5433, 27017, 6380, or 8080 | Another service uses the port | Stop it, or change the port and the matching connection strings ([§7](#7-configuration-reference)) |
| `dotnet run` fails because 5119 is in use | Another API instance on the host | Stop it; run one API instance at a time |
| Queued sales never appear | The consumer is failing, or two API instances share the queue | Check the error queue and the logs ([§8](#8-asynchronous-sale-intake), [§11](#11-logs-and-observability)) |
| `401` on every endpoint | Missing or expired token | Log in again ([§6](#6-first-requests-end-to-end), step 1) |
| `403` on POST, PUT, or DELETE, or on any `/api/users` call | The user's role is Customer | Use a Manager or Admin user |
| Simulator fails at setup with a validation message | API validation rejected the setup data | Read the message; it names the endpoint and the rule |
| Sale events never appear in the log | The relay is failing, or rows wait behind a failing one | Run the pending-rows query ([§8](#8-asynchronous-sale-intake)) and look for Error logs from `OutboxRelay` |

## 15. Repository layout

```
.
├── Ambev.DeveloperEvaluation.sln
├── CHALLENGE.md                  # challenge statement
├── README.md                     # this guide
├── Makefile                      # make dev-up: the local stack
├── docker-compose.yml            # PostgreSQL, MongoDB, Redis, API; named volumes
├── .doc/                         # API conventions and reference docs
├── docs/                         # API documentation: INDEX.md, TEMPLATE.md, one file per API
├── src/
│   ├── Ambev.DeveloperEvaluation.Domain        # entities, rules, repository contracts
│   ├── Ambev.DeveloperEvaluation.Application   # MediatR commands and handlers
│   ├── Ambev.DeveloperEvaluation.ORM           # EF Core context, mappings, migrations, repositories
│   ├── Ambev.DeveloperEvaluation.IoC           # dependency registration
│   ├── Ambev.DeveloperEvaluation.Common        # validation, security, logging, health checks
│   └── Ambev.DeveloperEvaluation.WebApi        # controllers, Rebus messaging, Program.cs
├── tests/
│   ├── Ambev.DeveloperEvaluation.Unit
│   ├── Ambev.DeveloperEvaluation.Integration
│   └── Ambev.DeveloperEvaluation.Functional    # the challenge sales rules over HTTP
└── tools/
    └── Ambev.DeveloperEvaluation.DevConsole    # trace console and load simulator
```

## 16. Branching and pull requests

Up to pull request #11 each change was committed on `dev` and released to `main` through its own pull request (#3 to #11). From pull request #12 on, every change starts on a `feature/<ITEM-ID>` or `bugfix/<ITEM-ID>` branch, where `<ITEM-ID>` is the work item from `work-items.json`, and reaches `dev` through a pull request. `dev` goes to `main` only as a release.

Commit messages use semantic prefixes (`feat:`, `fix:`, `test:`, `chore:`, `docs:`).
