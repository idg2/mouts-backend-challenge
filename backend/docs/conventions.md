# Conventions

What every API shares: authentication, the request path, transactions, responses and errors, list queries, and health checks.

> Work item: TD-023, TASK-072 (FEAT-018) · Key: CMN

## CMN-AUT — Authentication and roles

Every request passes through JWT bearer authentication; controllers decide with `[Authorize]` whether a token is required and which roles may write.

**Source:** `backend/src/Ambev.DeveloperEvaluation.Common/Security/AuthenticationExtension.cs`, `backend/src/Ambev.DeveloperEvaluation.Common/Security/JwtTokenGenerator.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs`

```mermaid
flowchart TD
  AUT01["CMN-AUT-01 Read the bearer token"]
  AUT02{"CMN-AUT-02 Token valid?"}
  AUT03["CMN-AUT-03 Principal with nameid, unique_name, and role"]
  AUT04{"CMN-AUT-04 Endpoint has Authorize?"}
  AUT05{"CMN-AUT-05 Authenticated?"}
  AUT06{"CMN-AUT-06 Role allowed?"}
  AUT07["CMN-AUT-07 Controller action runs"]
  E401["401"]
  E403["403"]
  AUT01 --> AUT02
  AUT02 -->|yes| AUT03 --> AUT04
  AUT02 -->|no| AUT04
  AUT04 -->|yes| AUT05
  AUT04 -->|no| AUT07
  AUT05 -->|yes| AUT06
  AUT05 -->|no| E401
  AUT06 -->|yes| AUT07
  AUT06 -->|no| E403
```

- CMN-AUT-02: the signature (HMAC-SHA256 with `Jwt:SecretKey`) and the lifetime are validated with zero clock skew; issuer and audience are not.
- CMN-AUT-04: the sales, customers, branches, products, and users controllers require a token for every action; the auth controller requires none.
- CMN-AUT-06: writes (POST, PUT, DELETE) require Admin or Manager; reads accept any authenticated role, Customer included, except in the users API, where every call requires Admin or Manager.

## CMN-PIP — Request pipeline

Every endpoint follows the same path from middleware to repository. The API documents refer to this topic instead of redrawing it.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Program.cs`, `backend/src/Ambev.DeveloperEvaluation.Common/Logging/LoggingExtension.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Common/LoggingBehavior.cs`, `backend/src/Ambev.DeveloperEvaluation.Common/Validation/ValidationBehavior.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Common/TransactionBehavior.cs`

```mermaid
sequenceDiagram
  participant C as Client
  participant M as Middleware
  participant K as Controller
  participant B as MediatR
  participant H as Handler
  participant R as Repository
  C->>M: CMN-PIP-01 Request logging starts
  M->>M: CMN-PIP-02 Exception middleware wraps the rest
  M->>M: Authentication and authorization, see CMN-AUT
  M->>K: route to the controller action
  K->>K: CMN-PIP-03 Model binding reads route and JSON body
  K->>K: CMN-PIP-04 Request validator runs
  K->>K: CMN-PIP-05 AutoMapper maps the request to a command
  K->>B: CMN-PIP-06 Send the command
  B->>B: CMN-PIP-07 LoggingBehavior times the request
  B->>B: CMN-PIP-08 ValidationBehavior passes through
  B->>B: CMN-PIP-09 TransactionBehavior, see CMN-TXN
  B->>H: CMN-PIP-10 Handler validates the command and runs the use case
  H->>R: CMN-PIP-11 Repository reads or writes DefaultContext
  R-->>H: entities
  H-->>K: result
  K-->>M: CMN-PIP-12 Result mapped and wrapped, see CMN-RSP
  M->>M: CMN-PIP-13 Request log line with status and elapsed time
  M-->>C: response
```

- CMN-PIP-03: a malformed body or a wrong content type never reaches the controller (CMN-RSP-03 and CMN-RSP-11).
- CMN-PIP-08: no FluentValidation validator is registered in DI, so validation runs in CMN-PIP-04 and again in CMN-PIP-10 instead (TD-003).
- CMN-PIP-07: rejections (validation, not found, duplicate, unauthorized) are logged as warnings with the exception type only; any other failure is logged as an error.
- CMN-PIP-13: MongoDB stores the line as `HTTP "POST" "/api/sales" responded 201 in 12.3456 ms`, and the console prints it without the quotes; health checks answering 200 are filtered out by the Serilog filter in `appsettings.json`.

## CMN-TXN — Transactions

Every write command runs inside one database transaction, so all of its saves commit or roll back together. Reads and login run without one.

**Source:** `backend/src/Ambev.DeveloperEvaluation.Application/Common/TransactionBehavior.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Common/ITransactionalCommand.cs`, `backend/src/Ambev.DeveloperEvaluation.ORM/UnitOfWork.cs`

```mermaid
flowchart TD
  TXN01{"CMN-TXN-01 Command is an ITransactionalCommand?"}
  TXN02["CMN-TXN-02 Run the handler without a transaction"]
  TXN03["CMN-TXN-03 Begin the transaction"]
  TXN04["CMN-TXN-04 Run the handler"]
  TXN05{"CMN-TXN-05 Handler threw?"}
  TXN06["CMN-TXN-06 Commit"]
  TXN07["CMN-TXN-07 Roll back and rethrow"]
  TXN01 -->|no| TXN02
  TXN01 -->|yes| TXN03 --> TXN04 --> TXN05
  TXN05 -->|no| TXN06
  TXN05 -->|yes| TXN07
```

- CMN-TXN-01: the create and delete commands of users, the create, update, and delete commands of customers, branches, products, and sales, and the create command of discount policies implement it.
- CMN-TXN-04: repository saves inside the handler join this transaction, so a sale and its outbox rows commit together (SAL-OBW).
- CMN-TXN-07: the rollback ignores the request's cancellation, so an aborted request still releases the transaction.

## CMN-RSP — Responses and errors

Successful responses share one envelope; every error, wherever the request stopped, has the `{ type, error, detail }` body of [general-api.md](../../.doc/general-api.md): `type` is the category, `error` is the code of the first failure (the type itself when the category has no codes), and `detail` is a string holding a JSON array with one message per failure, never empty.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ErrorResponse.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ModelStateErrorResponse.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/StatusCodeErrorResponse.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ApiResponse.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/PaginatedResponse.cs`

```mermaid
flowchart TD
  RSP01{"CMN-RSP-01 How did the rest of the pipeline return?"}
  RSP02["CMN-RSP-02 Envelope with success, message, and data"]
  RSP03["CMN-RSP-03 400 error body from the model state"]
  RSP04["CMN-RSP-04 400 error body from the request validator"]
  RSP05{"CMN-RSP-05 Exception type?"}
  RSP06["CMN-RSP-06 400 ValidationError, one detail per failure"]
  RSP07["CMN-RSP-07 404 ResourceNotFound"]
  RSP08["CMN-RSP-08 401 AuthenticationError"]
  RSP09["CMN-RSP-09 409 DuplicateEntry"]
  RSP10["CMN-RSP-10 500 ServerError, exception logged"]
  RSP11["CMN-RSP-11 Bodiless status filled with the error body"]
  RSP01 -->|success| RSP02
  RSP01 -->|model binding| RSP03
  RSP01 -->|request validator| RSP04
  RSP01 -->|exception| RSP05
  RSP01 -->|status code| RSP11
  RSP05 -->|ValidationException| RSP06
  RSP05 -->|KeyNotFoundException| RSP07
  RSP05 -->|UnauthorizedAccessException| RSP08
  RSP05 -->|DuplicateEntryException| RSP09
  RSP05 -->|BadHttpRequestException| RSP11
  RSP05 -->|other| RSP10
```

| `type` | Status | Where it comes from |
|---|---|---|
| `ValidationError` | 400 | Request validators (CMN-RSP-04), `ValidationException` from handlers, list parsing, and the discount rules (CMN-RSP-06), model state (CMN-RSP-03, `error` = `InvalidBody`) |
| `ResourceNotFound` | 404 | `KeyNotFoundException` (CMN-RSP-07); a path that matches no endpoint (CMN-RSP-11) |
| `AuthenticationError` | 401 | Login failure (CMN-RSP-08); the JWT challenge of CMN-AUT (CMN-RSP-11) |
| `AuthorizationError` | 403 | The JWT forbidden result of CMN-AUT (CMN-RSP-11) |
| `MethodNotAllowed` | 405 | Routing (CMN-RSP-11) |
| `DuplicateEntry` | 409 | `DuplicateEntryException` (CMN-RSP-09) |
| `UnsupportedMediaType` | 415 | Missing or wrong `Content-Type` (CMN-RSP-11) |
| `ServerError` | 500 | Any other exception (CMN-RSP-10); fixed detail, no stack |
| `HttpError` | other | Any other bodiless status (CMN-RSP-11); detail is the reason phrase |

- CMN-RSP-02: lists add `currentPage`, `totalPages`, and `totalCount` to the envelope.
- CMN-RSP-03: a malformed JSON body or an unbindable query value; each element is `key: message`.
- CMN-RSP-04: `BaseController.BadRequest(ValidationResult)` builds the body and traces this key, so the request-validator outcome is traced before the action returns. Each element is `property: message` (`Items[1].Quantity: ...`) when the failure names a property, or the message alone.
- CMN-RSP-06: `error` is the first failure's `ErrorCode`; elements are `property: message` as in CMN-RSP-04; a `ValidationException` without failures gives one element with its message.
- CMN-RSP-10: the exception is logged at Error with the request method and path; the body never carries its message or type. When the response has already started, the exception propagates instead.
- CMN-RSP-11: `UseStatusCodePages` fills any 4xx or 5xx that reaches the client without a body. The 401 keeps its `WWW-Authenticate` header. A Kestrel `BadHttpRequestException` (413 for a body over the size limit, 400 for a bad chunked body) keeps its status and gets the same catalog body from the exception middleware, so its trace shows CMN-RSP-05 and then this key.

## CMN-LST — List queries

Every list endpoint turns its query string into filters, an order, and a page. Field names are the response's JSON names, case-insensitive.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ListQueryParser.cs`, `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ListQueryExtensions.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/PaginatedList.cs`

```mermaid
flowchart TD
  LST01["CMN-LST-01 Read each key except _page, _size, and _order"]
  LST02{"CMN-LST-02 Key starts with _min or _max?"}
  LST03["CMN-LST-03 Range filter on a number or date"]
  LST04{"CMN-LST-04 Known response field?"}
  LST05["CMN-LST-05 Match filter per value"]
  LST06["CMN-LST-06 Parse _order into fields with asc or desc"]
  LST07{"CMN-LST-07 Any failure?"}
  LST08["CMN-LST-08 Validate _page and _size"]
  LST09["CMN-LST-09 Filter, order with Id as tiebreak, count, then page"]
  E400["400"]
  LST01 --> LST02
  LST02 -->|yes| LST03 --> LST06
  LST02 -->|no| LST04
  LST04 -->|yes| LST05 --> LST06
  LST04 -->|no, failure recorded| LST06
  LST06 --> LST07
  LST07 -->|yes| E400
  LST07 -->|no| LST08
  LST08 -->|pass| LST09
  LST08 -->|fail| E400
```

- CMN-LST-03: each `_min` or `_max` key appears once; a date given as `yyyy-MM-dd` in `_max` includes the whole day.
- CMN-LST-05: text fields match case-insensitively with `*` allowed only at the start or the end; other types match exactly, and a nullable id field matches an id, never null; a day matches the whole day; a repeated key matches any of its values, up to 50.
- CMN-LST-07: a bad filter or order throws `ValidationException` and gets the error body (CMN-RSP-06); a bad `_page` or `_size` fails in CMN-LST-08 and gets the error body from the request validator (CMN-RSP-04), or from the model state (CMN-RSP-03) when the value does not bind.
- CMN-LST-08: `_page` is at least 1 and `_size` is from 1 to 100 (defaults 1 and 10).
- CMN-LST-09: a page past the end returns an empty list with the real total count.

## CMN-HLT — Health checks

Three anonymous endpoints report whether the process is up. They do not probe PostgreSQL or MongoDB.

**Source:** `backend/src/Ambev.DeveloperEvaluation.Common/HealthChecks/HealthChecksExtension.cs`

```mermaid
flowchart TD
  HLT01["CMN-HLT-01 GET /health/live runs the Liveness check"]
  HLT02["CMN-HLT-02 GET /health/ready runs the Readiness check"]
  HLT03["CMN-HLT-03 GET /health matches no check"]
  HLT04["CMN-HLT-04 JSON with status and checks"]
  HLT01 --> HLT04
  HLT02 --> HLT04
  HLT03 --> HLT04
```

- CMN-HLT-01 and CMN-HLT-02: both checks always report Healthy.
- CMN-HLT-03: `/health` reports Healthy with an empty check list.
- CMN-HLT-04: Healthy and Degraded give 200 and Unhealthy gives 503.

## Known limitations

- No validator is registered in DI, so `ValidationBehavior` does nothing (TD-003).
- Health checks do not probe PostgreSQL or MongoDB.

## See also

- [INDEX.md](INDEX.md)
- [README_.md](../../README_.md): running and configuring the API
- [general-api.md](../../.doc/general-api.md): the challenge's target conventions
