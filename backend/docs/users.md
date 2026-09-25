# Users API

Signs up, reads, and deletes users. Any user can then log in through the Auth API.

> Work item: TD-025 · Key: USR · Routes: `/api/users`

## Endpoints

| Topic | Method | Route | Roles | Success | Errors |
|---|---|---|---|---|---|
| USR-CRT | POST | `/api/users` | anonymous | 201 | 400, 409 |
| USR-GET | GET | `/api/users/{id}` | anonymous (BUG-012) | 200 | 400, 404 |
| USR-DEL | DELETE | `/api/users/{id}` | anonymous (BUG-012) | 200 | 400, 404 |

## Data model

- `Email`: unique index; a second user with the same e-mail is a 409.
- `Password`: stored as a BCrypt hash and never returned.
- `Username` 3 to 50 characters, `Phone` in E.164 form, `Status` not Unknown, `Role` not None.

## USR-CRT — Create a user

Stores a new user with a hashed password. The caller chooses the status and the role.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Users/CreateUser/CreateUserHandler.cs`, `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/UserRepository.cs`

```mermaid
flowchart TD
  CRT01{"USR-CRT-01 Validate the request"}
  CRT02{"USR-CRT-02 Validate the command"}
  CRT03{"USR-CRT-03 E-mail already stored?"}
  CRT04["USR-CRT-04 Hash the password with BCrypt"]
  CRT05["USR-CRT-05 Insert the user"]
  CRT06["USR-CRT-06 201 with id, name, email, phone, role, and status"]
  E400["400"]
  E409["409"]
  CRT01 -->|pass| CRT02
  CRT01 -->|fail| E400
  CRT02 -->|pass| CRT03
  CRT02 -->|fail| E400
  CRT03 -->|no| CRT04 --> CRT05
  CRT03 -->|yes| E409
  CRT05 -->|inserted| CRT06
  CRT05 -->|unique violation| E409
```

- USR-CRT-01: the e-mail must be valid; the password needs at least 8 characters with an uppercase letter, a lowercase letter, a digit, and one of `! ? * . @ # $ % ^ & + =`.
- USR-CRT-05: two concurrent sign-ups with one e-mail both pass USR-CRT-03; the unique index turns the second into a 409.
- USR-CRT-06: the caller may choose any role, Admin included (BUG-012).

## USR-GET — Get a user

Returns one user by id, without the password.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Users/GetUser/GetUserHandler.cs`

```mermaid
flowchart TD
  GET01{"USR-GET-01 Validate the id"}
  GET02["USR-GET-02 Load the user"]
  GET03{"USR-GET-03 User found?"}
  GET04["USR-GET-04 200 with the user"]
  E400["400"]
  E404["404"]
  GET01 -->|pass| GET02 --> GET03
  GET01 -->|fail| E400
  GET03 -->|yes| GET04
  GET03 -->|no| E404
```

## USR-DEL — Delete a user

Deletes one user by id. Tokens already issued to it stay valid until they expire.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Users/UsersController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Users/DeleteUser/DeleteUserHandler.cs`

```mermaid
flowchart TD
  DEL01{"USR-DEL-01 Validate the id"}
  DEL02["USR-DEL-02 Delete the user"]
  DEL03{"USR-DEL-03 User existed?"}
  DEL04["USR-DEL-04 200"]
  E400["400"]
  E404["404"]
  DEL01 -->|pass| DEL02 --> DEL03
  DEL01 -->|fail| E400
  DEL03 -->|yes| DEL04
  DEL03 -->|no| E404
```

## Known limitations

- All three endpoints accept anonymous requests, and sign-up lets the caller pick any role (BUG-012).
- There is no list or update endpoint.
- Deleting a user does not revoke its tokens.

## See also

- [auth.md](auth.md)
- [conventions.md](conventions.md#cmn-pip--request-pipeline): the path every request follows
- [INDEX.md](INDEX.md)
