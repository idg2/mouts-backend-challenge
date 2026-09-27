# Guided validation and Debug diagnostics

How to run the API in Debug inside the compose stack and read its diagnostics. Back to the [README](../../README.md).

## Start the Debug stack

```bash
make debug-up
```

It builds the API image with `BUILD_CONFIGURATION=Debug` under its own tag (`ambevdeveloperevaluationwebapi-debug`) and starts PostgreSQL, MongoDB, Redis, and that API. The API keeps the container name and port 8080 of `make dev-up`, so the two targets replace each other's API container; run `make dev-up` to go back to the Release API. `--build` rebuilds the Debug image from the current code every time.

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
- The trace buffer is in memory; it starts empty at every API start.
- The trace also holds the outbox relay's idle cycles: about seven events every `Outbox:PollingInterval` (500 ms), so the default capacity of 5000 keeps roughly the last six minutes of an idle API.
