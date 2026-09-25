# Conventions

What every API shares: authentication, the request path, transactions, responses and errors, list queries, and health checks.

> Work item: TD-023 · Key: CMN

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
- CMN-AUT-04: the sales, customers, branches, and products controllers require a token for every action; the auth and users controllers require none (users: BUG-012).
- CMN-AUT-06: writes (POST, PUT, DELETE) require Admin or Manager; reads accept any authenticated role, Customer included.

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

- CMN-PIP-03: a malformed body or a wrong content type never reaches the controller (CMN-RSP).
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

- CMN-TXN-01: the create and delete commands of users and the create, update, and delete commands of customers, branches, products, and sales implement it.
- CMN-TXN-04: repository saves inside the handler join this transaction, so a sale and its outbox rows commit together (SAL-OBW).
- CMN-TXN-07: the rollback ignores the request's cancellation, so an aborted request still releases the transaction.

## CMN-RSP — Responses and errors

Successful responses share one envelope; errors take one of three shapes depending on where the request stopped.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/BaseController.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/ApiResponse.cs`, `backend/src/Ambev.DeveloperEvaluation.WebApi/Common/PaginatedResponse.cs`, `backend/src/Ambev.DeveloperEvaluation.Common/Validation/ValidationErrorDetail.cs`

```mermaid
flowchart TD
  RSP01{"CMN-RSP-01 How did the rest of the pipeline return?"}
  RSP02["CMN-RSP-02 Envelope with success, message, and data"]
  RSP03["CMN-RSP-03 ProblemDetails 400 or 415"]
  RSP04["CMN-RSP-04 400 with the FluentValidation failure list"]
  RSP05{"CMN-RSP-05 Exception type?"}
  RSP06["CMN-RSP-06 400 envelope with error and detail per failure"]
  RSP07["CMN-RSP-07 404 envelope with the message"]
  RSP08["CMN-RSP-08 401 envelope with the message"]
  RSP09["CMN-RSP-09 409 envelope with the message"]
  E500["500 without envelope"]
  RSP01 -->|success| RSP02
  RSP01 -->|model binding| RSP03
  RSP01 -->|request validator| RSP04
  RSP01 -->|exception| RSP05
  RSP05 -->|ValidationException| RSP06
  RSP05 -->|KeyNotFoundException| RSP07
  RSP05 -->|UnauthorizedAccessException| RSP08
  RSP05 -->|DuplicateEntryException| RSP09
  RSP05 -->|other| E500
```

- CMN-RSP-02: lists add `currentPage`, `totalPages`, and `totalCount` to the envelope.
- CMN-RSP-03: a malformed JSON body gives 400 and a missing or wrong `Content-Type` gives 415, both as ProblemDetails.
- CMN-RSP-04: this body is the raw failure list (`propertyName`, `errorMessage`, and more), not the envelope.
- CMN-RSP-05: the 401 and 403 of CMN-AUT come from the authentication middleware with an empty body; the `{ type, error, detail }` body of `.doc/general-api.md` is not produced.

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
- CMN-LST-05: text fields match case-insensitively with `*` allowed only at the start or the end; other types match exactly; a day matches the whole day; a repeated key matches any of its values, up to 50.
- CMN-LST-07: a bad filter or order throws `ValidationException` and gets the envelope (CMN-RSP-06); a bad `_page` or `_size` fails in CMN-LST-08 and gets the raw failure list (CMN-RSP-04).
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
- Errors come in three shapes; the challenge's `{ type, error, detail }` body is not produced.
- Health checks do not probe PostgreSQL or MongoDB.
- An unexpected exception returns 500 without the envelope.

## See also

- [INDEX.md](INDEX.md)
- [README_.md](../../README_.md): running and configuring the API
- [general-api.md](../../.doc/general-api.md): the challenge's target conventions
