# Branches API

The branch registry. Sales reference branches by id and copy their name.

> Work item: TD-027 · Key: BRN · Routes: `/api/branches`

## Endpoints

| Topic | Method | Route | Roles | Success | Errors |
|---|---|---|---|---|---|
| BRN-CRT | POST | `/api/branches` | Admin, Manager | 201 | 400, 401, 403 |
| BRN-GET | GET | `/api/branches/{id}` | any authenticated | 200 | 400, 401, 404 |
| BRN-LST | GET | `/api/branches` | any authenticated | 200 | 400, 401 |
| BRN-UPD | PUT | `/api/branches/{id}` | Admin, Manager | 200 | 400, 401, 403, 404 |
| BRN-DEL | DELETE | `/api/branches/{id}` | Admin, Manager | 200 | 400, 401, 403, 404 |

## Data model

- `Name`: required, at most 100 characters, not unique.

## BRN-CRT — Create a branch

Stores a new branch. Names are not unique, so the same request twice creates two branches.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs`, `src/Ambev.DeveloperEvaluation.Application/Branches/CreateBranch/CreateBranchHandler.cs`

```mermaid
flowchart TD
  CRT01{"BRN-CRT-01 Validate the request"}
  CRT02{"BRN-CRT-02 Validate the command"}
  CRT03["BRN-CRT-03 Insert the branch"]
  CRT04["BRN-CRT-04 201 with id and name"]
  E400["400"]
  CRT01 -->|pass| CRT02
  CRT01 -->|fail| E400
  CRT02 -->|pass| CRT03 --> CRT04
  CRT02 -->|fail| E400
```

## BRN-GET — Get a branch

Returns one branch by id.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs`, `src/Ambev.DeveloperEvaluation.Application/Branches/GetBranch/GetBranchHandler.cs`

```mermaid
flowchart TD
  GET01{"BRN-GET-01 Validate the id"}
  GET02["BRN-GET-02 Load the branch"]
  GET03{"BRN-GET-03 Branch found?"}
  GET04["BRN-GET-04 200 with the branch"]
  E400["400"]
  E404["404"]
  GET01 -->|pass| GET02 --> GET03
  GET01 -->|fail| E400
  GET03 -->|yes| GET04
  GET03 -->|no| E404
```

## BRN-LST — List branches

Returns branches one page at a time with the list conventions of CMN-LST.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs`, `src/Ambev.DeveloperEvaluation.Application/Branches/ListBranches/ListBranchesHandler.cs`, `src/Ambev.DeveloperEvaluation.ORM/Repositories/BranchRepository.cs`

```mermaid
flowchart TD
  LST01{"BRN-LST-01 Parse filters and order, see CMN-LST"}
  LST02{"BRN-LST-02 Validate _page and _size"}
  LST03["BRN-LST-03 Query one page"]
  LST04["BRN-LST-04 200 with the page"]
  E400["400"]
  LST01 -->|pass| LST02
  LST01 -->|fail| E400
  LST02 -->|pass| LST03 --> LST04
  LST02 -->|fail| E400
```

- BRN-LST-01: the fields are `id` and `name`.
- BRN-LST-03: the default order is name, then id.

## BRN-UPD — Update a branch

Renames a branch. Sales keep the name they copied when they were written.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs`, `src/Ambev.DeveloperEvaluation.Application/Branches/UpdateBranch/UpdateBranchHandler.cs`

```mermaid
flowchart TD
  UPD01{"BRN-UPD-01 Validate the request"}
  UPD02{"BRN-UPD-02 Validate the command"}
  UPD03["BRN-UPD-03 Load the branch"]
  UPD04{"BRN-UPD-04 Branch found?"}
  UPD05["BRN-UPD-05 Save the branch"]
  UPD06["BRN-UPD-06 200 with the branch"]
  E400["400"]
  E404["404"]
  UPD01 -->|pass| UPD02
  UPD01 -->|fail| E400
  UPD02 -->|pass| UPD03 --> UPD04
  UPD02 -->|fail| E400
  UPD04 -->|yes| UPD05 --> UPD06
  UPD04 -->|no| E404
```

## BRN-DEL — Delete a branch

Deletes one branch by id. Sales that reference it are untouched.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Branches/BranchesController.cs`, `src/Ambev.DeveloperEvaluation.Application/Branches/DeleteBranch/DeleteBranchHandler.cs`

```mermaid
flowchart TD
  DEL01{"BRN-DEL-01 Validate the id"}
  DEL02["BRN-DEL-02 Delete the branch"]
  DEL03{"BRN-DEL-03 Branch existed?"}
  DEL04["BRN-DEL-04 200"]
  E400["400"]
  E404["404"]
  DEL01 -->|pass| DEL02 --> DEL03
  DEL01 -->|fail| E400
  DEL03 -->|yes| DEL04
  DEL03 -->|no| E404
```

- BRN-DEL-02: no foreign key points to branches, so sales never block the delete and keep the copied name.

## Known limitations

- Branch names are not unique.
- Deleting a branch leaves sales pointing to an id that no longer exists.

## See also

- [conventions.md](conventions.md#cmn-lst--list-queries): filters, order, and pages
- [conventions.md](conventions.md#cmn-pip--request-pipeline): the path every request follows
- [INDEX.md](INDEX.md)
