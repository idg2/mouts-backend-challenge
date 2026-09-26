# Architecture

What each mechanism around the sales CRUD does and why it exists, then how a sale travels through the API after the request is accepted. Back to the [README](../../README.md).

## Mechanisms and why they exist

The sales CRUD with the challenge rules is the core. Seven mechanisms sit around it; each one answers a point of the challenge or of `.doc/`, and each one has a cost, listed here so it reads as a choice rather than an accident.

### Transactional outbox

- **What:** every sale write stores its events (`SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled`, `SaleDeleted`) in the `OutboxMessages` table, in the same transaction; a relay inside the API sends them afterwards.
- **Why:** the events are the challenge's differential ([CHALLENGE.md](../../CHALLENGE.md)). The outbox makes them exact: a rolled-back write leaves no event, and a committed write never loses one.
- **Cost:** one row per event, a relay polling every `Outbox:PollingInterval`, at-least-once delivery, and one API instance at a time, since a second relay would send the same rows.

### Rebus over MongoDB

- **What:** the queue that carries the sale events and the queued sales, stored in MongoDB.
- **Why:** Rebus is in [`.doc/frameworks.md`](../../.doc/frameworks.md) and MongoDB in [`.doc/tech-stack.md`](../../.doc/tech-stack.md); the MongoDB transport needs no extra broker container.
- **Cost:** the API does not start without MongoDB (the compose API waits for its healthcheck); retries have no backoff.

### MongoDB read model

- **What:** `GET /api/sales/{id}` and `GET /api/sales` read one document per sale, projected from the sale events.
- **Why:** [`.doc/overview.md`](../../.doc/overview.md) asks for PostgreSQL and MongoDB, relational and non-relational, and query performance; the list reads one document per sale with no joins.
- **Cost:** reads are eventual: a sale shows up within `Outbox:PollingInterval` of its write, so a `GET` right after the `201` can answer `404`. The read model is not rebuilt from PostgreSQL.

### Asynchronous intake

- **What:** `POST /api/sales` with `Prefer: respond-async` answers `202` and stores the sale through the queue. Without the header, the default, the sale is stored in the request and answers `201`.
- **Why:** [`.doc/overview.md`](../../.doc/overview.md) lists asynchronous programming patterns and performance. Past the database connection pool, synchronous requests fail with `500`; queued ones wait their turn. The [load test](load-test.md) measures both.
- **Cost:** the client polls for the result, and a rejected queued sale is visible only in the error queue and the log.

### Step tracing

- **What:** every step documented in [`docs/`](../INDEX.md) has a `StepTrace.Step` call at its source line (326 in `src/`), and `StepKeyCoverageTests` fails when a documented step has no call or a call has no documented step.
- **Why:** a project choice, not a `.doc/` requirement: the API documentation cannot drift from the code, and one command shows the path a request took.
- **Cost:** the calls sit inside handlers and controllers. They exist only in Debug builds (`[Conditional("DEBUG")]`) and are compiled out in Release.

### Developer console

- **What:** `tools/Ambev.DeveloperEvaluation.DevConsole`, with the [trace console](trace-console.md) (`t`) and the [load simulator](load-test.md) (`l`).
- **Why:** the trace console runs every documented flow end to end with the steps printed; the simulator backs the asynchronous intake with numbers.
- **Cost:** a separate project to maintain, and the trace console wipes the development databases unless it is pointed at scratch ones.

### Discount policy engine

- **What:** the challenge rules are the seeded default policy (at most 20 identical items, 10% from 4, 20% from 10). Policies can also be scoped to a product, a branch, or both, start at a given date, and are never edited; each sale item keeps the id of the policy that priced it.
- **Why:** the [CHALLENGE.md](../../CHALLENGE.md) rules hold by default, and a price rule can change without a deploy while older sales keep the rule they were sold under. The functional tests prove the default policy over HTTP ([README §7](../../README.md#7-automated-tests)).
- **Cost:** the rules are data: a Manager can supersede the default policy with a newer one, and disabling it without a replacement makes new sales answer `400 NoDiscountPolicy`. The rules are in [docs/discount-policies.md](../discount-policies.md).

## Asynchronous sale intake

The internals (queue, transactional outbox, relay, and consumers, with diagrams and step keys) are documented in [docs/sales.md](../sales.md).

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

## Sale events

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
