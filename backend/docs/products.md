# Products API

The product catalog. Sales reference products by id and copy their description and unit price.

> Work item: TD-028 · Key: PRD · Routes: `/api/products`

## Endpoints

| Topic | Method | Route | Roles | Success | Errors |
|---|---|---|---|---|---|
| PRD-CRT | POST | `/api/products` | Admin, Manager | 201 | 400, 401, 403, 409 |
| PRD-GET | GET | `/api/products/{id}` | any authenticated | 200 | 400, 401, 404 |
| PRD-LST | GET | `/api/products` | any authenticated | 200 | 400, 401 |
| PRD-UPD | PUT | `/api/products/{id}` | Admin, Manager | 200 | 400, 401, 403, 404, 409 |
| PRD-DEL | DELETE | `/api/products/{id}` | Admin, Manager | 200 | 400, 401, 403, 404 |

## Data model

- `Code`: at most 50 characters, stored trimmed and uppercase; unique index.
- `Description`: required, at most 200 characters.
- `UnitPrice`: greater than zero, at most two decimals (`numeric(18,2)`).

## PRD-CRT — Create a product

Stores a new product. The code is trimmed and uppercased first, so codes are unique regardless of case.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Products/CreateProduct/CreateProductHandler.cs`, `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs`

```mermaid
flowchart TD
  CRT01{"PRD-CRT-01 Validate the request"}
  CRT02{"PRD-CRT-02 Validate the command"}
  CRT03["PRD-CRT-03 Normalize the code"]
  CRT04{"PRD-CRT-04 Code already stored?"}
  CRT05["PRD-CRT-05 Insert the product"]
  CRT06["PRD-CRT-06 201 with id, code, description, and unit price"]
  E400["400"]
  E409["409"]
  CRT01 -->|pass| CRT02
  CRT01 -->|fail| E400
  CRT02 -->|pass| CRT03 --> CRT04
  CRT02 -->|fail| E400
  CRT04 -->|no| CRT05
  CRT04 -->|yes| E409
  CRT05 -->|inserted| CRT06
  CRT05 -->|unique violation| E409
```

- PRD-CRT-05: two concurrent requests with one code both pass PRD-CRT-04; the unique index turns the second into a 409.

## PRD-GET — Get a product

Returns one product by id.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Products/GetProduct/GetProductHandler.cs`

```mermaid
flowchart TD
  GET01{"PRD-GET-01 Validate the id"}
  GET02["PRD-GET-02 Load the product"]
  GET03{"PRD-GET-03 Product found?"}
  GET04["PRD-GET-04 200 with the product"]
  E400["400"]
  E404["404"]
  GET01 -->|pass| GET02 --> GET03
  GET01 -->|fail| E400
  GET03 -->|yes| GET04
  GET03 -->|no| E404
```

## PRD-LST — List products

Returns products one page at a time with the list conventions of CMN-LST.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Products/ListProducts/ListProductsHandler.cs`, `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs`

```mermaid
flowchart TD
  LST01{"PRD-LST-01 Parse filters and order, see CMN-LST"}
  LST02{"PRD-LST-02 Validate _page and _size"}
  LST03["PRD-LST-03 Query one page"]
  LST04["PRD-LST-04 200 with the page"]
  E400["400"]
  LST01 -->|pass| LST02
  LST01 -->|fail| E400
  LST02 -->|pass| LST03 --> LST04
  LST02 -->|fail| E400
```

- PRD-LST-01: the fields are `id`, `code`, `description`, and `unitPrice`; `unitPrice` also accepts `_minUnitPrice` and `_maxUnitPrice`.
- PRD-LST-03: the default order is description, then id.

## PRD-UPD — Update a product

Replaces a product's code, description, and unit price. Sales keep the description and price they copied.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Products/UpdateProduct/UpdateProductHandler.cs`, `backend/src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRepository.cs`

```mermaid
flowchart TD
  UPD01{"PRD-UPD-01 Validate the request"}
  UPD02{"PRD-UPD-02 Validate the command"}
  UPD03["PRD-UPD-03 Load the product"]
  UPD04{"PRD-UPD-04 Product found?"}
  UPD05["PRD-UPD-05 Normalize the code"]
  UPD06{"PRD-UPD-06 Code used by another product?"}
  UPD07["PRD-UPD-07 Save the product"]
  UPD08["PRD-UPD-08 200 with the product"]
  E400["400"]
  E404["404"]
  E409["409"]
  UPD01 -->|pass| UPD02
  UPD01 -->|fail| E400
  UPD02 -->|pass| UPD03 --> UPD04
  UPD02 -->|fail| E400
  UPD04 -->|yes| UPD05 --> UPD06
  UPD04 -->|no| E404
  UPD06 -->|no| UPD07
  UPD06 -->|yes| E409
  UPD07 -->|saved| UPD08
  UPD07 -->|unique violation| E409
```

- PRD-UPD-07: a new price applies only to sales and items written afterwards; existing items keep their copied price (SAL-UPD-07).

## PRD-DEL — Delete a product

Deletes one product by id. Sale items that reference it are untouched.

**Source:** `backend/src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs`, `backend/src/Ambev.DeveloperEvaluation.Application/Products/DeleteProduct/DeleteProductHandler.cs`

```mermaid
flowchart TD
  DEL01{"PRD-DEL-01 Validate the id"}
  DEL02["PRD-DEL-02 Delete the product"]
  DEL03{"PRD-DEL-03 Product existed?"}
  DEL04["PRD-DEL-04 200"]
  E400["400"]
  E404["404"]
  DEL01 -->|pass| DEL02 --> DEL03
  DEL01 -->|fail| E400
  DEL03 -->|yes| DEL04
  DEL03 -->|no| E404
```

- PRD-DEL-02: no foreign key points to products, so sale items never block the delete and keep the copied description and price.

## Known limitations

- Deleting a product leaves sale items pointing to an id that no longer exists.

## See also

- [conventions.md](conventions.md#cmn-lst--list-queries): filters, order, and pages
- [conventions.md](conventions.md#cmn-pip--request-pipeline): the path every request follows
- [INDEX.md](INDEX.md)
