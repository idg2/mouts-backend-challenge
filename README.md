# DeveloperStore Sales API

A .NET 8 sales API for the DeveloperStore challenge: sales with the challenge discount rules, the customer, branch, and product registries, and JWT authentication. The challenge statement is in [CHALLENGE.md](CHALLENGE.md), and the API conventions are in [`.doc/`](.doc/).

This guide takes you from a fresh clone to a running API and a first sale. Configuration, architecture, the trace console, the load test, and operations are in the guides indexed in [§9](#9-guides).

**Contents**

1. [What runs where](#1-what-runs-where)
2. [Prerequisites](#2-prerequisites)
3. [Start the containers with make](#3-start-the-containers-with-make)
4. [Database schema and the administrator](#4-database-schema-and-the-administrator)
5. [Choose how to run the API](#5-choose-how-to-run-the-api)
6. [First sale, end to end](#6-first-sale-end-to-end)
7. [Automated tests](#7-automated-tests)
8. [Repository layout](#8-repository-layout)
9. [Guides](#9-guides)

---

## 1. What runs where

| Component | Technology | Role | Host port |
|---|---|---|---|
| API | ASP.NET Core 8 (`src/Ambev.DeveloperEvaluation.WebApi`) | REST API, Swagger, JWT auth | 8080 (container) or 5119 (`dotnet run`) |
| PostgreSQL 13 | compose service `ambev.developerevaluation.database` | Users, customers, branches, products, sales | 5433 (5432 inside the container) |
| MongoDB 8 | compose service `ambev.developerevaluation.nosql` | Application logs (`developer_evaluation_logs`), the Rebus sale queue (`developer_evaluation_bus`), and the sales read model (`developer_evaluation_read`) | 27017 |
| Redis 7 | compose service `ambev.developerevaluation.cache` | Started by the stack; not used by the code (see below) | 6380 (6379 inside the container) |
| Developer console | console app (`tools/Ambev.DeveloperEvaluation.DevConsole`) | Traced scenarios against the API hosted in process, and the load simulator | — |

PostgreSQL keeps its data in the named volume `postgres-data` and MongoDB in `mongo-data`, so both survive `docker compose down`.

Redis is deliberately not used. Every sale write resolves its discount policies with one indexed PostgreSQL query, and a stale cache entry would price a sale with the wrong policy. Where a cache would apply is the discount policy matrix: a decorator of `IDiscountPolicyRepository.GetApplicableAsync`, the single read behind `DiscountPolicyResolver`, so the resolver and the sale handlers stay unchanged. It would hold every policy, disabled ones included, because editing a sale reprices it at its original date, and creating or disabling a policy (DSC-CRT, DSC-DIS) would clear it. `IMemoryCache` serves one API instance; Redis fits once several instances share the policies (FEAT-008).

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

The stack publishes PostgreSQL on 5433 and Redis on 6380 (not the defaults 5432 and 6379, to avoid colliding with other local instances), and the defaults 27017 and 8080. Stop anything else that listens on them, or change the published ports in `docker-compose.yml` together with the matching connection strings (see [configuration guide](docs/guide/configuration.md)).

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

All four services should be `running`. The API container waits for the PostgreSQL and MongoDB healthchecks, so on a fresh clone it starts a few seconds after the databases.

To start only the databases, for example when you run the API with `dotnet run`, use:

```bash
docker compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql
```

To run the API built in Debug instead, with the diagnostics routes and the step trace buffer, plus the guided validation UI on port 4280, use `make debug-up` ([validation guide](docs/guide/validation-ui.md)). It replaces the API container of `make dev-up`, and `make dev-up` replaces it back.

## 4. Database schema and the administrator

Nothing to run by hand. Every time the API starts, before it listens, it applies the pending migrations and then creates the administrator from `Seed:Admin` ([configuration guide](docs/guide/configuration.md)) unless a user already has that e-mail. After new migrations arrive or the `postgres-data` volume is recreated, start the API again. The API container waits for the PostgreSQL healthcheck before it starts.

To apply the migrations without starting the API:

```bash
dotnet ef database update \
  --project src/Ambev.DeveloperEvaluation.ORM \
  --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

The command reads `ConnectionStrings:DefaultConnection` from `src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`, which already points at the compose PostgreSQL on `localhost:5433`.

## 5. Choose how to run the API

Run **one** API instance at a time. Every instance consumes the same MongoDB queue (`sales-intake`), so two instances split the queued sales between them and distort any measurement. Each instance also runs an outbox relay ([architecture guide](docs/guide/architecture.md#asynchronous-sale-intake)), which assumes it is the only one: two relays can send the same sale events twice and out of order.

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

## 6. First sale, end to end

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

To use Swagger, copy `data.token` from the login response, click **Authorize** at the top of the Swagger page, and paste the token without the `Bearer ` prefix; every request from Swagger then carries it. The steps below use curl, which sends the `Authorization: Bearer <token>` header itself.

**Step 2: register a customer, a branch, and a product.** Customers need a valid CPF or CNPJ, and it must be unique. Product codes are unique too.

```bash
id() { python3 -c "import sys,json;print(json.load(sys.stdin)['data']['id'])"; }
CUSTOMER=$(curl -s -X POST $BASE/api/customers -H "$AUTH" -H 'Content-Type: application/json' \
  -d '{"name":"Acme Market","document":"52998224725"}' | id)
BRANCH=$(curl -s -X POST $BASE/api/branches -H "$AUTH" -H 'Content-Type: application/json' \
  -d '{"name":"Downtown"}' | id)
PRODUCT=$(curl -s -X POST $BASE/api/products -H "$AUTH" -H 'Content-Type: application/json' \
  -d '{"code":"BEER-350","description":"Beer 350ml","unitPrice":5}' | id)
```

**Step 3: create a sale.** The response is `201` with the stored sale. The server prices the items from the discount policies (the seeded default policy is the challenge rule: 4 to 9 units of one product get 10%, 10 to 20 get 20%, more than 20 is refused), so the body carries no amounts; send `discountPercentage` on an item only to ask for less than the ceiling.

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

The [walkthrough](docs/guide/walkthrough.md) continues from here: more users and roles, discount rules per product and branch, the asynchronous `202` path, and list filters.

## 7. Automated tests

Run these from the repository root:

```bash
dotnet test tests/Ambev.DeveloperEvaluation.Unit          # no infrastructure needed
dotnet test tests/Ambev.DeveloperEvaluation.Integration   # needs the compose PostgreSQL and MongoDB
dotnet test tests/Ambev.DeveloperEvaluation.Functional    # the challenge sales rules over HTTP; same infrastructure
./coverage-report.sh                                      # coverage report in TestResults/CoverageReport/index.html
```

The integration tests create a throwaway database for each test class, apply the migrations, and drop it afterwards, so they never touch `developer_evaluation`.

The functional tests host the whole API in process against throwaway PostgreSQL and MongoDB databases, dropped afterwards, and prove the challenge sales rules through HTTP with the seeded default policy: 1 to 3 identical items get no discount, 4 to 9 get 10%, 10 to 20 get 20%, more than 20 (on one line or across lines) answer 400 `QuantityLimitExceeded`, a requested discount above the tier answers 400 `DiscountAboveAllowed`, each product is priced on its own total, names and prices come from the catalog, cancelling a line reprices the others, a policy scoped to one product applies its own cap and tiers while the other products keep the challenge rules, and disabling the default policy leaves older sales editable. They also check that the API refuses to start without `ConnectionStrings:DefaultConnection`, naming the key.

## 8. Repository layout

```
.
├── Ambev.DeveloperEvaluation.sln
├── CHALLENGE.md                  # challenge statement
├── README.md                     # this guide
├── Makefile                      # make dev-up / debug-up: the local stack
├── docker-compose.yml            # PostgreSQL, MongoDB, Redis, API; named volumes
├── docker-compose.debug.yml      # overlay: the API built in Debug
├── .doc/                         # API conventions and reference docs
├── docs/                         # API documentation: INDEX.md, TEMPLATE.md, one file per API
│   └── guide/                    # the guides indexed in §9
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
    ├── Ambev.DeveloperEvaluation.DevConsole    # trace console and load simulator
    └── validation-ui                          # Angular guided validation UI (make debug-up)
```

## 9. Guides

| Guide | What it covers |
|---|---|
| [walkthrough.md](docs/guide/walkthrough.md) | Users and roles, discount policies per product and branch, the asynchronous `202` path, list filters |
| [configuration.md](docs/guide/configuration.md) | Every configuration key, environment overrides, changing ports and pool sizes |
| [architecture.md](docs/guide/architecture.md) | Each mechanism around the sales CRUD and why it exists; the asynchronous sale intake, the sale events and the transactional outbox, the read model, and how to inspect the queue |
| [trace-console.md](docs/guide/trace-console.md) | Running a documented flow with every step printed |
| [validation-ui.md](docs/guide/validation-ui.md) | The guided validation UI (`make debug-up`, port 4280): 18 scenarios for the discount rules, the policies, and the outbox; the Debug diagnostics routes |
| [load-test.md](docs/guide/load-test.md) | The simulator that compares synchronous and asynchronous sale creation under load |
| [observability.md](docs/guide/observability.md) | Where the logs go and how to query them |
| [operations.md](docs/guide/operations.md) | Stop, restart, reset, and troubleshooting |
| [contributing.md](docs/guide/contributing.md) | Branching, pull requests, and commit messages |
| [docs/INDEX.md](docs/INDEX.md) | API documentation: every endpoint and internal flow, with step keys and diagrams |
