# Customers API

The customer registry. Sales reference customers by id and copy their name.

> Work item: TD-026 · Key: CUS · Routes: `/api/customers`

## Endpoints

| Topic | Method | Route | Roles | Success | Errors |
|---|---|---|---|---|---|
| CUS-CRT | POST | `/api/customers` | Admin, Manager | 201 | 400, 401, 403, 409 |
| CUS-GET | GET | `/api/customers/{id}` | any authenticated | 200 | 400, 401, 404 |
| CUS-LST | GET | `/api/customers` | any authenticated | 200 | 400, 401 |
| CUS-UPD | PUT | `/api/customers/{id}` | Admin, Manager | 200 | 400, 401, 403, 404, 409 |
| CUS-DEL | DELETE | `/api/customers/{id}` | Admin, Manager | 200 | 400, 401, 403, 404 |

## Data model

- `Name`: required, at most 100 characters.
- `Document`: stored normalized (dots, dashes, slashes, and spaces removed, uppercase); a CPF (11 characters) or a CNPJ (14) with valid check digits; unique index.

## CUS-CRT — Create a customer

Stores a new customer. The document is normalized first, so a formatted and an unformatted CPF are the same customer.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs`, `src/Ambev.DeveloperEvaluation.Application/Customers/CreateCustomer/CreateCustomerHandler.cs`, `src/Ambev.DeveloperEvaluation.Domain/Validation/DocumentNumber.cs`, `src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs`

```mermaid
flowchart TD
  CRT01{"CUS-CRT-01 Validate the request"}
  CRT02{"CUS-CRT-02 Validate the command and the CPF or CNPJ"}
  CRT03["CUS-CRT-03 Normalize the document"]
  CRT04{"CUS-CRT-04 Document already stored?"}
  CRT05["CUS-CRT-05 Insert the customer"]
  CRT06["CUS-CRT-06 201 with id, name, and document"]
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

- CUS-CRT-05: two concurrent requests with one document both pass CUS-CRT-04; the unique index turns the second into a 409.
- CUS-CRT-06: the response carries the normalized document.

## CUS-GET — Get a customer

Returns one customer by id.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs`, `src/Ambev.DeveloperEvaluation.Application/Customers/GetCustomer/GetCustomerHandler.cs`

```mermaid
flowchart TD
  GET01{"CUS-GET-01 Validate the id"}
  GET02["CUS-GET-02 Load the customer"]
  GET03{"CUS-GET-03 Customer found?"}
  GET04["CUS-GET-04 200 with the customer"]
  E400["400"]
  E404["404"]
  GET01 -->|pass| GET02 --> GET03
  GET01 -->|fail| E400
  GET03 -->|yes| GET04
  GET03 -->|no| E404
```

## CUS-LST — List customers

Returns customers one page at a time with the list conventions of CMN-LST.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs`, `src/Ambev.DeveloperEvaluation.Application/Customers/ListCustomers/ListCustomersHandler.cs`, `src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs`

```mermaid
flowchart TD
  LST01{"CUS-LST-01 Parse filters and order, see CMN-LST"}
  LST02{"CUS-LST-02 Validate _page and _size"}
  LST03["CUS-LST-03 Query one page"]
  LST04["CUS-LST-04 200 with the page"]
  E400["400"]
  LST01 -->|pass| LST02
  LST01 -->|fail| E400
  LST02 -->|pass| LST03 --> LST04
  LST02 -->|fail| E400
```

- CUS-LST-01: the fields are `id`, `name`, and `document`.
- CUS-LST-03: the default order is name, then id.

## CUS-UPD — Update a customer

Replaces a customer's name and document. Sales keep the name they copied when they were written.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs`, `src/Ambev.DeveloperEvaluation.Application/Customers/UpdateCustomer/UpdateCustomerHandler.cs`, `src/Ambev.DeveloperEvaluation.ORM/Repositories/CustomerRepository.cs`

```mermaid
flowchart TD
  UPD01{"CUS-UPD-01 Validate the request"}
  UPD02{"CUS-UPD-02 Validate the command"}
  UPD03["CUS-UPD-03 Load the customer"]
  UPD04{"CUS-UPD-04 Customer found?"}
  UPD05["CUS-UPD-05 Normalize the document"]
  UPD06{"CUS-UPD-06 Document used by another customer?"}
  UPD07["CUS-UPD-07 Save the customer"]
  UPD08["CUS-UPD-08 200 with the customer"]
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

- CUS-UPD-07: sales written earlier keep the old name; there is no foreign key to update them (SAL-CRT copies the name).

## CUS-DEL — Delete a customer

Deletes one customer by id. Sales that reference it are untouched.

**Source:** `src/Ambev.DeveloperEvaluation.WebApi/Features/Customers/CustomersController.cs`, `src/Ambev.DeveloperEvaluation.Application/Customers/DeleteCustomer/DeleteCustomerHandler.cs`

```mermaid
flowchart TD
  DEL01{"CUS-DEL-01 Validate the id"}
  DEL02["CUS-DEL-02 Delete the customer"]
  DEL03{"CUS-DEL-03 Customer existed?"}
  DEL04["CUS-DEL-04 200"]
  E400["400"]
  E404["404"]
  DEL01 -->|pass| DEL02 --> DEL03
  DEL01 -->|fail| E400
  DEL03 -->|yes| DEL04
  DEL03 -->|no| E404
```

- CUS-DEL-02: no foreign key points to customers, so sales never block the delete and keep the copied name.

## Known limitations

- Deleting a customer leaves sales pointing to an id that no longer exists.

## See also

- [conventions.md](conventions.md#cmn-lst--list-queries): filters, order, and pages
- [conventions.md](conventions.md#cmn-pip--request-pipeline): the path every request follows
- [INDEX.md](INDEX.md)
