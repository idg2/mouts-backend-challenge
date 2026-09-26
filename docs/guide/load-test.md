# Load test: synchronous vs asynchronous

Compares the two ways of creating sales under load. Back to the [README](../../README.md).

## What it measures

Under thousands of concurrent inserts, the synchronous endpoint holds one PostgreSQL connection per request for the whole transaction. Past the Npgsql pool size (100 by default), requests wait, and after 15 s they fail with `500`.

The asynchronous mode turns the spike into a queue: the client gets `202` in milliseconds, and the consumers store sales at the pace the database sustains. The asynchronous mode does **not** insert faster. The database is the same, so write throughput is similar. What changes is behavior above capacity.

## The simulator

The `l` command of the developer console (`tools/Ambev.DeveloperEvaluation.DevConsole`) talks to the API over HTTP only. Each run:

1. Logs in **once** as the administrator the API seeds ([README §4](../../README.md#4-database-schema-and-the-administrator)).
2. Registers one customer, one branch, and three products for the run.
3. Starts every loop of every profile at the same time. Each loop is a `while` that posts a three-item sale, times it, records the status code, and waits `PauseMilliseconds`. Concurrency equals the total number of loops.
4. Prints, per profile and in total:
   - latency p50/p95/p99;
   - the outcome of each request (status code, or the exception name when no response came back);
   - throughput over the whole run.
5. In async mode, also polls the sale count until every `202` is stored and reports the drain time, plus how many requests got no response.

## Simulator settings

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

The simulator also reads the API's configuration, as the trace console does ([trace-console.md](trace-console.md)), and logs in with its `Seed:Admin:Email` and `Seed:Admin:Password`; override them with `--Seed:Admin:Email=<value>` and `--Seed:Admin:Password=<value>` when the API runs with other values.

With the defaults, a run has 1,000 concurrent loops × 20 requests, which is 20,000 sales.

## Step by step

**Step 1: bring the platform up** ([README §3](../../README.md#3-start-the-containers-with-make); the API applies the schema itself, [README §4](../../README.md#4-database-schema-and-the-administrator)).

**Step 2: run exactly one API instance** ([README §5](../../README.md#5-choose-how-to-run-the-api)). For the most direct comparison, use Option B (`dotnet run`, port 5119, the simulator default) with the API container stopped.

**Step 3: decide the capacity settings.** Leave the defaults (pool 100, `MaxParallelism` 20) for the first run; change them later as shown in [configuration.md](configuration.md). Keep `Rebus:MaxParallelism` below the pool size, otherwise the consumers can exhaust the pool themselves.

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

## Reading the report

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

## Caveats

- **Anything else that creates sales during the run distorts the drain count.** Keep other clients quiet.
- **An unanswered request may still have been queued.** When the report shows unanswered requests, the drain target can be reached before every accepted sale is stored.
- **PostgreSQL and MongoDB share the host disk.** The named volumes separate data, not I/O: on Docker Desktop or OrbStack every volume lives on the same VM disk. Absolute numbers are pessimistic compared with a server.
- **MongoDB holds both the queue and the logs.** Log writes and queue writes compete during the run.
- **Runs accumulate data.** Every run adds its user, catalog entries, and sales. Reset the data between series if you need clean numbers ([operations.md](operations.md#stop-restart-and-reset)).
- **The outbox has a cost.** Each sale write also stores its events, and the queue also carries them, so numbers are not comparable with runs made before the outbox existed. That difference is the outbox cost per sale.
