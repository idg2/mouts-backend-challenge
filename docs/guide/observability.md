# Logs and observability

Where the API writes its logs and how to query them. Back to the [README](../../README.md).

| Where | How |
|---|---|
| Container console | `docker compose logs -f ambev.developerevaluation.webapi` |
| Host console | the `dotnet run` terminal |
| MongoDB | database `developer_evaluation_logs`, collection `logs` (expires after `LogStorage:ExpireAfter`) |
| MongoDB read model | database `developer_evaluation_read`, collection `sales` (one document per sale, see [architecture.md](architecture.md#asynchronous-sale-intake)) |

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
