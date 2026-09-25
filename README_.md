# DeveloperStore Sales API — Step-by-Step Guide

This guide takes you from a fresh clone to a running platform, then through configuration and the load test that compares the synchronous and asynchronous ways of creating sales. The original challenge statement is in [README.md](README.md), and the API conventions are in [`.doc/`](.doc/).

**Contents**

1. [What runs where](#1-what-runs-where)
2. [Prerequisites](#2-prerequisites)
3. [Start the containers with make](#3-start-the-containers-with-make)
4. [Create the database schema](#4-create-the-database-schema)
5. [Choose how to run the API](#5-choose-how-to-run-the-api)
6. [First requests, end to end](#6-first-requests-end-to-end)
7. [Configuration reference](#7-configuration-reference)
8. [Asynchronous sale intake](#8-asynchronous-sale-intake)
9. [Load test: synchronous vs asynchronous](#9-load-test-synchronous-vs-asynchronous)
10. [Logs and observability](#10-logs-and-observability)
11. [Automated tests](#11-automated-tests)
12. [Stop, restart, and reset](#12-stop-restart-and-reset)
13. [Troubleshooting](#13-troubleshooting)
14. [Repository layout](#14-repository-layout)
15. [Branching and pull requests](#15-branching-and-pull-requests)

---

## 1. What runs where

| Component | Technology | Role | Host port |
|---|---|---|---|
| API | ASP.NET Core 8 (`backend/src/Ambev.DeveloperEvaluation.WebApi`) | REST API, Swagger, JWT auth | 8080 (container) or 5119 (`dotnet run`) |
| PostgreSQL 13 | compose service `ambev.developerevaluation.database` | Users, customers, branches, products, sales | 5432 |
| MongoDB 8 | compose service `ambev.developerevaluation.nosql` | Application logs (`developer_evaluation_logs`) and the Rebus sale queue (`developer_evaluation_bus`) | 27017 |
| Redis 7 | compose service `ambev.developerevaluation.cache` | Started by the stack; not used by the code yet | 6379 |
| Load simulator | console app (`backend/tools/Ambev.DeveloperEvaluation.LoadSimulator`) | Generates concurrent sale traffic and reports latency and throughput | — |

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

The stack publishes the default ports 5432, 27017, 6379, and 8080. Stop anything else that listens on them, or change the published ports in `backend/docker-compose.yml` together with the matching connection strings (see [§7](#7-configuration-reference)).

## 3. Start the containers with make

All `make` targets run from the **repository root**.

**Step 1: build the API image.** `make dev-up` builds the image only when none exists. After you pull new code, rebuild it explicitly:

```bash
cd backend
docker compose build ambev.developerevaluation.webapi
cd ..
```

**Step 2: start the stack.**

```bash
make dev-up
```

This starts PostgreSQL, MongoDB, Redis, and the API container, then prints where each one listens:

```
Postgres  0.0.0.0:5432
MongoDB   0.0.0.0:27017
Redis     0.0.0.0:6379
API       http://0.0.0.0:8080/swagger
```

**Step 3: check the containers.**

```bash
cd backend && docker compose ps
```

All four services should be `running`. If the API container is `exited`, see [§13](#13-troubleshooting): the API does not start while MongoDB is still initializing.

To start only the databases, for example when you run the API with `dotnet run`, use:

```bash
cd backend
docker compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql
```

## 4. Create the database schema

The API does not apply migrations on startup. Apply them once after the first `make dev-up`, and again whenever new migrations arrive or the `postgres-data` volume is recreated:

```bash
cd backend
dotnet ef database update \
  --project src/Ambev.DeveloperEvaluation.ORM \
  --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

The command reads `ConnectionStrings:DefaultConnection` from `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, which already points at the compose PostgreSQL on `localhost:5432`. Nothing needs to be restarted afterwards.

## 5. Choose how to run the API

Run **one** API instance at a time. Every instance consumes the same MongoDB queue (`sales-intake`), so two instances split the queued sales between them and distort any measurement. Each instance also runs an outbox relay ([§8](#8-asynchronous-sale-intake)), which assumes it is the only one: two relays can send the same sale events twice and out of order.

### Option A — API in the container (port 8080)

`make dev-up` already started it. Open **http://localhost:8080/swagger**.

Its configuration comes from `appsettings.json` plus the `environment` block of the API service in `backend/docker-compose.yml`, which points the connection strings at the other containers.

### Option B — API on the host with `dotnet run` (port 5119)

Stop the API container first, then run the API from the host:

```bash
cd backend
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

**Step 1: create a user.** `POST /api/users` is anonymous. `role`: 1 = Customer, 2 = Manager, 3 = Admin. `status`: 1 = Active. Write endpoints require the Admin or Manager role.

```bash
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' -d '{
  "username": "manager", "password": "Str0ng@Pass", "phone": "+5511999998888",
  "email": "manager@example.com", "status": 1, "role": 2 }'
```

The password needs at least 8 characters, with an uppercase letter, a lowercase letter, a digit, and one of `! ? * . @ # $ % ^ & + =`.

**Step 2: log in and keep the token.**

```bash
TOKEN=$(curl -s -X POST $BASE/api/auth -H 'Content-Type: application/json' \
  -d '{"email":"manager@example.com","password":"Str0ng@Pass"}' \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['token'])")
AUTH="Authorization: Bearer $TOKEN"
```

Swagger does not have a JWT security scheme configured, so it cannot send the token. Use curl, as below, or any HTTP client that sends the `Authorization: Bearer <token>` header. Swagger is still useful to browse the routes and schemas.

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

**Step 4: create a sale synchronously.** The response is `201` with the stored sale.

```bash
SALE="{\"customerId\":\"$CUSTOMER\",\"branchId\":\"$BRANCH\",\"totalAmount\":10,
  \"items\":[{\"productId\":\"$PRODUCT\",\"quantity\":2,\"discountPercentage\":0,\"discountAmount\":0,\"totalAmount\":10}]}"
curl -s -X POST $BASE/api/sales -H "$AUTH" -H 'Content-Type: application/json' -d "$SALE"
```

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

Configuration comes from `backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, overridden by environment variables. An environment variable replaces `:` with `__`; for example, `ConnectionStrings:MessageBus` becomes `ConnectionStrings__MessageBus`. Every key below is **required**. A missing or invalid key stops the API at startup with a message that names the key; there are no silent defaults.

| Key | Value in `appsettings.json` | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL on `localhost:5432` | Npgsql format. Append `;Maximum Pool Size=N` to change the pool size (default 100). |
| `Jwt:SecretKey` | Development key | At least 32 bytes. Replace it outside development. |
| `ConnectionStrings:LogStorage` | MongoDB on `localhost:27017` | MongoDB URL for the log sink. Percent-encode `@` in passwords as `%40`. |
| `LogStorage:Database` / `Collection` | `developer_evaluation_logs` / `logs` | Where Serilog stores events. |
| `LogStorage:ExpireAfter` | `1.00:00:00` | Log TTL, as a .NET time span. |
| `ConnectionStrings:MessageBus` | `mongodb://…/developer_evaluation_bus?authSource=admin` | MongoDB URL of the Rebus queue. The path **must** name a database, and it cannot be the `LogStorage` database. |
| `Rebus:InputQueue` | `sales-intake` | Queue the API sends to and consumes from. |
| `Rebus:Workers` | `1` | Rebus worker threads. |
| `Rebus:MaxParallelism` | `20` | Queued messages processed at the same time. Keep it **below** the Npgsql pool size. |
| `Outbox:PollingInterval` | `00:00:05` | Wait between outbox relay cycles; after a full batch the next cycle starts at once. A time span (hh:mm:ss) from `00:00:00.1` to `01:00:00`; a bare number is rejected, since .NET reads it as days. |
| `Outbox:BatchSize` | `50` | Outbox rows one relay cycle sends. |
| `Serilog` section | Levels, console output, `/health` filter | EF Core and ASP.NET Core log at Warning, so SQL commands do not flood the log. |

The API container overrides `DefaultConnection`, `LogStorage`, and `MessageBus` in `backend/docker-compose.yml` so they point at the service names instead of `localhost`.

**MongoDB is required at startup.** The Rebus transport creates its queue index when the bus starts, so the whole API, synchronous endpoints included, does not start while MongoDB is unreachable.

**Changing a port.** If you publish PostgreSQL or MongoDB on another host port, change the matching connection strings in `appsettings.json` too (they are used by `dotnet run`, `dotnet ef`, and the integration tests). The container connection strings use the internal service ports and do not change.

**Example: a bigger pool and more consumers for one run of the host API.**

```bash
cd backend
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n;Maximum Pool Size=200" \
Rebus__MaxParallelism=50 \
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi --launch-profile http
```

## 8. Asynchronous sale intake

The internals (queue, transactional outbox, relay, and consumers, with diagrams and step keys) are documented in [backend/docs/sales.md](backend/docs/sales.md).

`POST /api/sales` has two modes:

| Request | What happens | Response |
|---|---|---|
| No `Prefer` header, or a header without `respond-async` | The sale is validated and stored in the request's own transaction | `201` with the sale, or `400` |
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

**Known limitations:**

- Retries have no backoff.
- The MongoDB transport claims a message at most 5 times. Claims that Rebus does not count as errors, such as a restart mid-processing, still use up those 5 claims, so a message can get stuck, which the last query above finds.
- Update and delete are synchronous only.

## 9. Load test: synchronous vs asynchronous

### What it measures

Under thousands of concurrent inserts, the synchronous endpoint holds one PostgreSQL connection per request for the whole transaction. Past the Npgsql pool size (100 by default), requests wait, and after 15 s they fail with `500`.

The asynchronous mode turns the spike into a queue: the client gets `202` in milliseconds, and the consumers store sales at the pace the database sustains. The asynchronous mode does **not** insert faster. The database is the same, so write throughput is similar. What changes is behavior above capacity.

### The simulator

`backend/tools/Ambev.DeveloperEvaluation.LoadSimulator` is a console app that talks to the API over HTTP only. Each run:

1. Creates its own Manager user (random password) and logs in **once**.
2. Registers one customer, one branch, and three products for the run.
3. Starts every loop of every profile at the same time. Each loop is a `while` that posts a three-item sale, times it, records the status code, and waits `PauseMilliseconds`. Concurrency equals the total number of loops.
4. Prints, per profile and in total:
   - latency p50/p95/p99;
   - the outcome of each request (status code, or the exception name when no response came back);
   - throughput over the whole run.
5. In async mode, also polls the sale count until every `202` is stored and reports the drain time, plus how many requests got no response.

### Simulator settings

The settings are in `backend/tools/Ambev.DeveloperEvaluation.LoadSimulator/appsettings.json`. Any setting can be overridden on the command line with `--Simulator:<Key>=<value>`, and every setting is required.

| Key | Default | Meaning |
|---|---|---|
| `Simulator:BaseUrl` | `http://localhost:5119` | API URL. Use `http://localhost:8080` for the container. |
| `Simulator:Mode` | `sync` | `sync` or `async` (async sends `Prefer: respond-async`). |
| `Simulator:RequestsPerLoop` | `20` | Requests each loop sends. |
| `Simulator:Profiles` | fast 500 loops / 0 ms, medium 300 / 50 ms, slow 200 / 200 ms | Loop groups: `Name`, `Loops`, `PauseMilliseconds`. |
| `Simulator:DrainTimeout` | `00:10:00` | How long async mode waits for the queue to drain. |
| `Simulator:DrainPollInterval` | `00:00:01` | How often it counts stored sales. |
| `Simulator:RequestTimeout` | `00:01:40` | Client timeout per request; a timed-out request counts as unanswered. |

With the defaults, a run has 1,000 concurrent loops × 20 requests, which is 20,000 sales.

### Step by step

**Step 1: bring the platform up and apply the schema** ([§3](#3-start-the-containers-with-make) and [§4](#4-create-the-database-schema)).

**Step 2: run exactly one API instance** ([§5](#5-choose-how-to-run-the-api)). For the most direct comparison, use Option B (`dotnet run`, port 5119, the simulator default) with the API container stopped.

**Step 3: decide the capacity settings.** Leave the defaults (pool 100, `MaxParallelism` 20) for the first run; change them later as shown in [§7](#7-configuration-reference). Keep `Rebus:MaxParallelism` below the pool size, otherwise the consumers can exhaust the pool themselves.

**Step 4: warm up with a small run** to check the setup:

```bash
cd backend
dotnet run --project tools/Ambev.DeveloperEvaluation.LoadSimulator -- \
  --Simulator:Mode=sync --Simulator:RequestsPerLoop=3 \
  --Simulator:Profiles:0:Loops=5 --Simulator:Profiles:1:Loops=3 --Simulator:Profiles:2:Loops=2
```

Expected: `TOTAL: 30 requests, … outcomes: 201=30`.

**Step 5: run the synchronous scenario.**

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.LoadSimulator -- --Simulator:Mode=sync
```

**Step 6: run the asynchronous scenario.**

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.LoadSimulator -- --Simulator:Mode=async
```

Add `--Simulator:BaseUrl=http://localhost:8080` to both commands when the target is the API container.

**Step 7: raise the load until the synchronous mode breaks.** Increase `Loops` or `RequestsPerLoop` until the sync run shows `500` outcomes or unanswered requests, then repeat the same settings in async mode.

```bash
dotnet run --project tools/Ambev.DeveloperEvaluation.LoadSimulator -- --Simulator:Mode=sync \
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
- **Runs accumulate data.** Every run adds its user, catalog entries, and sales. Reset the data between series if you need clean numbers ([§12](#12-stop-restart-and-reset)).
- **The outbox has a cost.** Each sale write also stores its events, and the queue also carries them, so numbers are not comparable with runs made before the outbox existed. That difference is the outbox cost per sale.

## 10. Logs and observability

| Where | How |
|---|---|
| Container console | `cd backend && docker compose logs -f ambev.developerevaluation.webapi` |
| Host console | the `dotnet run` terminal |
| MongoDB | database `developer_evaluation_logs`, collection `logs` (expires after `LogStorage:ExpireAfter`) |

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

## 11. Automated tests

Run these from `backend/`:

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit          # no infrastructure needed
dotnet test tests/Ambev.DeveloperEvaluation.Integration   # needs the compose PostgreSQL and MongoDB
./coverage-report.sh                                      # coverage report in TestResults/CoverageReport/index.html
```

The integration tests create a throwaway database for each test class, apply the migrations, and drop it afterwards, so they never touch `developer_evaluation`.

## 12. Stop, restart, and reset

Run these from `backend/`:

| Goal | Command | Data |
|---|---|---|
| Stop everything | `docker compose stop` | Kept |
| Start again | `make dev-up` from the repository root | Kept |
| Remove the containers | `docker compose down` | Kept (named volumes) |
| **Delete all data** | `docker compose down -v` | **PostgreSQL and MongoDB data erased.** Apply the schema again ([§4](#4-create-the-database-schema)). |
| Rebuild the API image after a code change | `docker compose build ambev.developerevaluation.webapi && docker compose up -d ambev.developerevaluation.webapi` | Kept |

## 13. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| API container `exited` right after `make dev-up` | MongoDB was still initializing when the API started; the Rebus transport needs MongoDB at startup | `cd backend && docker compose up -d ambev.developerevaluation.webapi` |
| API exits naming a key, for example `ConnectionStrings:MessageBus is not configured` | Missing or invalid configuration | Set the key named in the message ([§7](#7-configuration-reference)) |
| Requests fail with `relation "Sales" does not exist` | Schema not applied, or volume recreated | Apply the schema ([§4](#4-create-the-database-schema)) |
| `address already in use` on 5432, 27017, 6379, or 8080 | Another service uses the port | Stop it, or change the port and the matching connection strings ([§7](#7-configuration-reference)) |
| `dotnet run` fails because 5119 is in use | Another API instance on the host | Stop it; run one API instance at a time |
| Queued sales never appear | The consumer is failing, or two API instances share the queue | Check the error queue and the logs ([§8](#8-asynchronous-sale-intake), [§10](#10-logs-and-observability)) |
| `401` on every endpoint | Missing or expired token | Log in again ([§6](#6-first-requests-end-to-end), step 2) |
| `403` on POST, PUT, or DELETE | The user's role is Customer | Use a Manager or Admin user |
| Simulator fails at setup with a validation message | API validation rejected the setup data | Read the message; it names the endpoint and the rule |
| Sale events never appear in the log | The relay is failing, or rows wait behind a failing one | Run the pending-rows query ([§8](#8-asynchronous-sale-intake)) and look for Error logs from `OutboxRelay` |

## 14. Repository layout

```
.
├── Makefile                      # make dev-up: the local stack
├── README.md                     # challenge statement
├── README_.md                    # this guide
├── .doc/                         # API conventions and reference docs
└── backend/
    ├── Ambev.DeveloperEvaluation.sln
    ├── docker-compose.yml        # PostgreSQL, MongoDB, Redis, API; named volumes
    ├── docs/                     # API documentation: INDEX.md, TEMPLATE.md, one file per API
    ├── src/
    │   ├── Ambev.DeveloperEvaluation.Domain        # entities, rules, repository contracts
    │   ├── Ambev.DeveloperEvaluation.Application   # MediatR commands and handlers
    │   ├── Ambev.DeveloperEvaluation.ORM           # EF Core context, mappings, migrations, repositories
    │   ├── Ambev.DeveloperEvaluation.IoC           # dependency registration
    │   ├── Ambev.DeveloperEvaluation.Common        # validation, security, logging, health checks
    │   └── Ambev.DeveloperEvaluation.WebApi        # controllers, Rebus messaging, Program.cs
    ├── tests/
    │   ├── Ambev.DeveloperEvaluation.Unit
    │   └── Ambev.DeveloperEvaluation.Integration
    └── tools/
        └── Ambev.DeveloperEvaluation.LoadSimulator # load test console app
```

## 15. Branching and pull requests

Up to pull request #11 each change was committed on `dev` and released to `main` through its own pull request (#3 to #11). From pull request #12 on, every change starts on a `feature/<ITEM-ID>` or `bugfix/<ITEM-ID>` branch, where `<ITEM-ID>` is the work item from `work-items.json`, and reaches `dev` through a pull request. `dev` goes to `main` only as a release.

Commit messages use semantic prefixes (`feat:`, `fix:`, `test:`, `chore:`, `docs:`).
