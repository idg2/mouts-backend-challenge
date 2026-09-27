# Guided validation UI and Debug diagnostics

How to run the guided validation UI, and the API diagnostics it reads. Back to the [README](../../README.md).

## Start the Debug stack

```bash
make debug-up
```

It builds the API image with `BUILD_CONFIGURATION=Debug` under its own tag (`ambevdeveloperevaluationwebapi-debug`) and starts PostgreSQL, MongoDB, Redis, and that API, plus the guided validation UI on port 4280. The API keeps the container name and port 8080 of `make dev-up`, so the two targets replace each other's API container; run `make dev-up` to go back to the Release API. `--build` rebuilds the Debug image from the current code every time.

## Run the guided validation

With the Debug stack up, open the UI address `make debug-up` prints (`http://localhost:4280`). The badge shows `API: Debug · trace on`. Sign in: the form is prefilled with the seeded administrator of this stack. Click **▶ Run all (18)**, or **▶** on one scenario. Each scenario creates its own customer, branch, and product, so runs can repeat in any order.

For each scenario the page shows:

- **Flow:** its path, one box per step; ◆ boxes are decisions and show the answer the API gave. Green is proven by a response, amber is in progress, red diverged.
- **Steps:** expected versus actual; **view** opens the requests and responses.
- **Trace:** the step keys the API traced during the run ([docs/INDEX.md](../INDEX.md)), without the relay's idle cycles.

| Group | Scenarios | What they prove |
|---|---|---|
| Discount rules | D1–D8 | The default policy: no discount below 4, 10% from 4, 20% from 10, at most 20 units, tiers from a product's total across lines, a requested discount up to the ceiling; a rejected sale leaves no event |
| Discount policies | P1–P3 | A product policy beats the default, product and branch beats product, a disabled policy stops pricing new sales |
| Outbox and events | O1–O7 | Every sale write records its events in the outbox in the same transaction; the relay sends them in sequence order; the read model follows; a rejected asynchronous sale records nothing |

With a Release API the page says so and disables every scenario.

### Discount matrix

**Discount matrix** in the top bar registers discount policies (DSC-CRT) and checks them with real sales (SAL-CRT). On load the page creates its own customer, branch, and product; every policy it registers is scoped to that product, so the scenarios and other sales are untouched. The table lists the default policy and the policies registered on the page, numbered #1, #2, and so on.

1. Set the maximum per product and up to three tiers (from, to, discount %), then click **Register policy**. The policy starts 2 s after the API's clock, because a policy may not start in the past; a refused policy shows the API's error code, such as `InvalidTierSet`.
2. Type some quantities, such as `3, 5, 10, 31`, and click **Check**. The page waits until the newest policy has started, then posts one sale per quantity and shows the policy that priced it, the ceiling, the discount, and the total, or the error of a refused sale, such as `QuantityLimitExceeded`.

Registering a second policy and checking again shows that the newest policy of a scope wins. Reload the page to start over with new data. This page does not need the diagnostics routes, so it also works with a Release API.

### Working on the UI itself

```bash
cd tools/validation-ui
API_URL=http://localhost:8080 UI_LOGIN_EMAIL=admin@example.com UI_LOGIN_PASSWORD=Adm1n@Pass npm start   # http://localhost:4200
npm test -- --watch=false
```

`npm start` stops if a variable is missing. `API_URL` is the running API (the Debug stack, or `dotnet run`).

## Diagnostics routes (Debug builds only)

A Release build does not contain these routes; they answer 404 there.

| Route | Access | Returns |
|---|---|---|
| `GET /api/diagnostics` | anonymous | `{ traceEnabled }`; a 200 itself means a Debug build |
| `GET /api/diagnostics/outbox?after={sequence}` | Admin | `{ head, items }`: the highest outbox sequence and up to 500 rows after `after`, with `type`, `occurredAt`, `processedAt`, and the stored `payload` |
| `GET /api/diagnostics/trace?after={cursor}` | Admin | `{ head, items }`: the last trace cursor and the StepTrace events after `after` (key, title, values, file, line) |

Without `after`, both reads return only `head`. Record the head, do something, then read after it: the answer holds only what happened since. Requests to `/api/diagnostics` are never recorded in the trace. The trace buffer keeps the latest `Diagnostics:Trace:Capacity` events ([configuration](configuration.md)).

## Limits

- The cursor window is shared: another client using the API at the same time puts its outbox rows and trace events inside your window.
- Before each scenario the UI waits until the relay has sent the previous scenario's events, so their dispatch steps do not reach the next trace; a projection that finishes just after that can still show a few read-model steps of the previous sale.
- The trace buffer is in memory; it starts empty at every API start.
- O7 cannot tell "still queued" from "rejected" through the API (both answer 404); it proves only that no event appeared within 10 s. The trace shows the rejection.
- The relay's failure branch (a failed send that stops the batch) is not exercised; the trace console's failures scenario covers it.
- The trace also holds the outbox relay's idle cycles: about seven events every `Outbox:PollingInterval` (500 ms), so the default capacity of 5000 keeps roughly the last six minutes of an idle API.
