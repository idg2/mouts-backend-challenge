# API walkthrough

Continues [README §6](../../README.md#6-first-sale-end-to-end): the commands reuse `BASE`, `AUTH`, `CUSTOMER`, `BRANCH`, `PRODUCT`, and `SALE` from there. Back to the [README](../../README.md).

## Create more users

There is no anonymous sign-up: every `/api/users` call needs an Admin or Manager token. `role`: 1 = Customer, 2 = Manager, 3 = Admin. `status`: 1 = Active. Write endpoints require the Admin or Manager role; a Customer can only read.

```bash
curl -s -X POST $BASE/api/users -H "$AUTH" -H 'Content-Type: application/json' -d '{
  "username": "manager", "password": "Str0ng@Pass", "phone": "+5511999998888",
  "email": "manager@example.com", "status": 1, "role": 2 }'
```

The password needs at least 8 characters, with an uppercase letter, a lowercase letter, a digit, and one of `! ? * . @ # $ % ^ & + =`.

**No control over user roles.** Admin and Manager have the same powers everywhere. Either one can create a user with any role, Admin included, and delete any user, Admin included; nothing stops a Manager from promoting someone to Admin.

## Discount rules per product and branch

The challenge rules are the seeded default policy, which covers every product and every branch. A policy can also be scoped to a product, a branch, or both, with its own maximum and tiers. Each sale is priced, per product, by the most specific policy in effect at its sale date: product and branch, then product, then branch, then the default. A policy cannot start in the past, so this one starts ten seconds from now and gives the product of README §6 up to 50 units, with 30% from 12 units:

```bash
FROM=$(date -u -v+10S +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u -d '+10 seconds' +%Y-%m-%dT%H:%M:%SZ)   # macOS, then Linux
curl -s -X POST $BASE/api/discount-policies -H "$AUTH" -H 'Content-Type: application/json' -d "{\"productId\":\"$PRODUCT\",\"branchId\":null,
  \"validFrom\":\"$FROM\",\"maxQuantityPerProduct\":50,\"tiers\":[{\"minQuantity\":12,\"maxQuantity\":null,\"percentage\":30}]}"
sleep 10
curl -s -X POST $BASE/api/sales -H "$AUTH" -H 'Content-Type: application/json' -d "{\"customerId\":\"$CUSTOMER\",\"branchId\":\"$BRANCH\",
  \"items\":[{\"productId\":\"$PRODUCT\",\"quantity\":30}]}"
```

The sale is accepted with 30 units at 30%, and its item carries the new policy's id in `discountPolicyId`; every other product keeps the challenge rules. A policy is never edited: a newer one of the same scope wins once it starts, and `POST /api/discount-policies/disable` ends one from that moment on, while sales dated before keep being priced by it. The rules are in [docs/discount-policies.md](../discount-policies.md).

## Create a sale asynchronously

Add `Prefer: respond-async`. The response is `202 Accepted` with the id the sale will be stored under:

```bash
curl -s -i -X POST $BASE/api/sales -H "$AUTH" -H 'Content-Type: application/json' \
  -H 'Prefer: respond-async' -d "$SALE"
```

```
HTTP/1.1 202 Accepted
Location: http://localhost:8080/api/Sales/<id>
Preference-Applied: respond-async

{"data":{"id":"<id>"},"success":true,"message":"Sale sent for processing","errors":[]}
```

## Follow the queued sale

Poll the `Location` URL. It answers `404` while the sale is queued and `200` once it is stored:

```bash
curl -s -o /dev/null -w '%{http_code}\n' -H "$AUTH" $BASE/api/sales/<id>
```

`GET /api/sales/{id}` and the sale list read from the MongoDB read model, which the sale events fill within `Outbox:PollingInterval` (half a second) of the write; a `GET` fired in that window answers 404, and `saleDate` comes back with millisecond precision (BSON dates), so its last digits can differ from the POST response.

## List and filter

List endpoints follow [`.doc/general-api.md`](../../.doc/general-api.md):

| Parameter | Meaning |
|---|---|
| `_page`, `_size` | Paging; `_size` goes from 1 to 100 |
| `_order` | Sort order, for example `saleNumber desc` |
| Any response field | Filter; `*` works at the start or end of text |
| `_min<Field>`, `_max<Field>` | Ranges on numeric and date fields |

```bash
curl -s -H "$AUTH" "$BASE/api/sales?_page=1&_size=5&_order=saleNumber%20desc"
curl -s -H "$AUTH" "$BASE/api/customers?name=Acme*"
```
