# Auth API

Exchanges an e-mail and password for a JWT that the other APIs require.

> Work item: TD-024 · Key: AUT · Routes: `/api/auth`

## Endpoints

| Topic | Method | Route | Roles | Success | Errors |
|---|---|---|---|---|---|
| AUT-LGN | POST | `/api/auth` | anonymous | 200 | 400, 401 |

## Data model

Reads `Users` (see [users.md](users.md#data-model)): `Email`, the BCrypt `Password` hash, `Status`, and `Role`.

## AUT-LGN — Log in

Checks the credentials and the user's status, then issues a token valid for 8 hours. Every failure answers 401 without saying which check failed.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Auth/AuthController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Auth/AuthenticateUser/AuthenticateUserHandler.cs`, `backend/src/Ambev.DeveloperEvaluation.Common/Security/JwtTokenGenerator.cs`

```mermaid
flowchart TD
  LGN01{"AUT-LGN-01 Validate e-mail format and password presence"}
  LGN02["AUT-LGN-02 Send the command, no transaction"]
  LGN03["AUT-LGN-03 Find the user by e-mail"]
  LGN04{"AUT-LGN-04 User found?"}
  LGN05{"AUT-LGN-05 Password matches the BCrypt hash?"}
  LGN06{"AUT-LGN-06 User is Active?"}
  LGN07["AUT-LGN-07 Generate the JWT"]
  LGN08["AUT-LGN-08 200 with token, email, name, and role"]
  E400["400"]
  E401A["401 Invalid credentials"]
  E401B["401 User is not active"]
  LGN01 -->|pass| LGN02 --> LGN03 --> LGN04
  LGN01 -->|fail| E400
  LGN04 -->|yes| LGN05
  LGN04 -->|no| E401A
  LGN05 -->|yes| LGN06
  LGN05 -->|no| E401A
  LGN06 -->|yes| LGN07 --> LGN08
  LGN06 -->|no| E401B
```

```mermaid
sequenceDiagram
  participant C as Client
  participant A as AuthController
  participant H as AuthenticateUserHandler
  participant U as UserRepository
  participant P as PasswordHasher
  participant J as JwtTokenGenerator
  C->>A: POST /api/auth
  A->>A: AUT-LGN-01 Validate e-mail format and password presence
  A->>H: AUT-LGN-02 Send the command, no transaction
  H->>U: AUT-LGN-03 Find the user by e-mail
  U-->>H: user or null
  alt no user
    H-->>A: AUT-LGN-04 401 Invalid credentials
  else user found
    H->>P: AUT-LGN-05 Verify the password against the BCrypt hash
    P-->>H: match or not, 401 Invalid credentials when not
    H->>H: AUT-LGN-06 Check the user is Active, 401 User is not active when not
    H->>J: AUT-LGN-07 Generate the JWT
    J-->>H: token
    H-->>A: result
    A-->>C: AUT-LGN-08 200 with token, email, name, and role
  end
```

- AUT-LGN-04 and AUT-LGN-05: both failures return the same message, so the response does not reveal whether the e-mail exists; the warning log carries the user id, never the e-mail.
- AUT-LGN-06: Inactive and Suspended users get 401 even with the right password.
- AUT-LGN-07: claims `nameid` (user id), `unique_name` (username), and `role`; signed with HMAC-SHA256 and `Jwt:SecretKey`; valid for 8 hours; there is no refresh token.

## Known limitations

- No refresh token and no logout.
- A token stays valid for its 8 hours after the user is deleted or deactivated.

## See also

- [conventions.md](conventions.md#cmn-aut--authentication-and-roles): how the token is validated
- [users.md](users.md)
- [INDEX.md](INDEX.md)
