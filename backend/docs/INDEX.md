# API Documentation

How each API of the DeveloperStore sales backend behaves, from the HTTP contract to the internal flows. Every process has a key that a future trace console can print.

## Documents

| Document | Key | Scope |
|---|---|---|
| [conventions.md](conventions.md) | CMN | Authentication, request pipeline, transactions, responses and errors, list queries, health checks |
| [auth.md](auth.md) | AUT | Login and token issue |
| [users.md](users.md) | USR | User sign-up, read, and delete |
| [customers.md](customers.md) | CUS | Customer registry |
| [branches.md](branches.md) | BRN | Branch registry |
| [products.md](products.md) | PRD | Product registry |
| [sales.md](sales.md) | SAL | Sales, the queue, the transactional outbox, and the event consumer |
| [TEMPLATE.md](TEMPLATE.md) | — | Structure and key rules every document follows |

## Topics

| Topic | Title | Document |
|---|---|---|
| [CMN-AUT](conventions.md#cmn-aut--authentication-and-roles) | Authentication and roles | conventions.md |
| [CMN-PIP](conventions.md#cmn-pip--request-pipeline) | Request pipeline | conventions.md |
| [CMN-TXN](conventions.md#cmn-txn--transactions) | Transactions | conventions.md |
| [CMN-RSP](conventions.md#cmn-rsp--responses-and-errors) | Responses and errors | conventions.md |
| [CMN-LST](conventions.md#cmn-lst--list-queries) | List queries | conventions.md |
| [CMN-HLT](conventions.md#cmn-hlt--health-checks) | Health checks | conventions.md |
| [AUT-LGN](auth.md#aut-lgn--log-in) | Log in | auth.md |
| [USR-CRT](users.md#usr-crt--create-a-user) | Create a user | users.md |
| [USR-GET](users.md#usr-get--get-a-user) | Get a user | users.md |
| [USR-DEL](users.md#usr-del--delete-a-user) | Delete a user | users.md |
| [CUS-CRT](customers.md#cus-crt--create-a-customer) | Create a customer | customers.md |
| [CUS-GET](customers.md#cus-get--get-a-customer) | Get a customer | customers.md |
| [CUS-LST](customers.md#cus-lst--list-customers) | List customers | customers.md |
| [CUS-UPD](customers.md#cus-upd--update-a-customer) | Update a customer | customers.md |
| [CUS-DEL](customers.md#cus-del--delete-a-customer) | Delete a customer | customers.md |
| [BRN-CRT](branches.md#brn-crt--create-a-branch) | Create a branch | branches.md |
| [BRN-GET](branches.md#brn-get--get-a-branch) | Get a branch | branches.md |
| [BRN-LST](branches.md#brn-lst--list-branches) | List branches | branches.md |
| [BRN-UPD](branches.md#brn-upd--update-a-branch) | Update a branch | branches.md |
| [BRN-DEL](branches.md#brn-del--delete-a-branch) | Delete a branch | branches.md |
| [PRD-CRT](products.md#prd-crt--create-a-product) | Create a product | products.md |
| [PRD-GET](products.md#prd-get--get-a-product) | Get a product | products.md |
| [PRD-LST](products.md#prd-lst--list-products) | List products | products.md |
| [PRD-UPD](products.md#prd-upd--update-a-product) | Update a product | products.md |
| [PRD-DEL](products.md#prd-del--delete-a-product) | Delete a product | products.md |
| [SAL-OVW](sales.md#sal-ovw--overview) | Overview | sales.md |
| [SAL-CRT](sales.md#sal-crt--create-a-sale) | Create a sale | sales.md |
| [SAL-ASY](sales.md#sal-asy--queue-a-sale) | Queue a sale | sales.md |
| [SAL-GET](sales.md#sal-get--get-a-sale) | Get a sale | sales.md |
| [SAL-LST](sales.md#sal-lst--list-sales) | List sales | sales.md |
| [SAL-UPD](sales.md#sal-upd--update-a-sale) | Update a sale | sales.md |
| [SAL-DEL](sales.md#sal-del--delete-a-sale) | Delete a sale | sales.md |
| [SAL-EVT](sales.md#sal-evt--sale-events) | Sale events | sales.md |
| [SAL-OBW](sales.md#sal-obw--outbox-write) | Outbox write | sales.md |
| [SAL-RLY](sales.md#sal-rly--relay-loop) | Relay loop | sales.md |
| [SAL-DSP](sales.md#sal-dsp--dispatch-cycle) | Dispatch cycle | sales.md |
| [SAL-BUS](sales.md#sal-bus--message-bus) | Message bus | sales.md |
| [SAL-CON](sales.md#sal-con--event-consumer) | Event consumer | sales.md |
| [SAL-ERR](sales.md#sal-err--failures-and-the-error-queue) | Failures and the error queue | sales.md |

## Keys

Keys follow the rules in [TEMPLATE.md](TEMPLATE.md#keys). A step key's number never changes. Extraction regex: `\b[A-Z]{3}-[A-Z]{3}(-\d{2})?\b`.

## Where else to look

- Swagger UI (`/swagger`, Development environment): the full request and response schemas.
- [README_.md](../../README_.md): running, configuring, and operating the API.
- [.doc/general-api.md](../../.doc/general-api.md): the challenge's target conventions, which differ from the implemented ones where CMN-RSP says so.
