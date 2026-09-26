# Operations

Stopping, resetting, and fixing the local stack. Back to the [README](../../README.md).

## Stop, restart, and reset

Run these from the repository root:

| Goal | Command | Data |
|---|---|---|
| Stop everything | `docker compose stop` | Kept |
| Start again | `make dev-up` from the repository root | Kept |
| Remove the containers | `docker compose down` | Kept (named volumes) |
| **Delete all data** | `docker compose down -v` | **PostgreSQL and MongoDB data erased.** The API recreates the schema and the administrator when it starts again ([README §4](../../README.md#4-database-schema-and-the-administrator)). |
| Rebuild the API image after a code change | `docker compose build ambev.developerevaluation.webapi && docker compose up -d ambev.developerevaluation.webapi` | Kept |


## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| API exits naming a key, for example `ConnectionStrings:MessageBus is not configured` | Missing or invalid configuration | Set the key named in the message ([configuration.md](configuration.md)) |
| API exits at startup with a PostgreSQL connection error | PostgreSQL is unreachable; the API applies the migrations before it listens ([README §4](../../README.md#4-database-schema-and-the-administrator)) | Start PostgreSQL, then the API |
| `address already in use` on 5433, 27017, 6380, or 8080 | Another service uses the port | Stop it, or change the port and the matching connection strings ([configuration.md](configuration.md)) |
| `dotnet run` fails because 5119 is in use | Another API instance on the host | Stop it; run one API instance at a time |
| Queued sales never appear | The consumer is failing, or two API instances share the queue | Check the error queue and the logs ([architecture.md](architecture.md#asynchronous-sale-intake), [observability.md](observability.md)) |
| `401` on every endpoint | Missing or expired token | Log in again ([README §6](../../README.md#6-first-sale-end-to-end), step 1) |
| `403` on POST, PUT, or DELETE, or on any `/api/users` call | The user's role is Customer | Use a Manager or Admin user |
| Simulator fails at setup with a validation message | API validation rejected the setup data | Read the message; it names the endpoint and the rule |
| Sale events never appear in the log | The relay is failing, or rows wait behind a failing one | Run the pending-rows query ([architecture.md](architecture.md#asynchronous-sale-intake)) and look for Error logs from `OutboxRelay` |
