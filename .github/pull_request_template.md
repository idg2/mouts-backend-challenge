## Work items
## Work items

<!-- IDs from work-items.json. The PR title uses a semantic prefix (feat:, fix:, chore:, docs:, test:). -->
- Closes: FEAT-000 / TASK-000 / BUG-000 / TD-000

## Summary

<!-- What changes and why, in 2-5 bullets. -->

## API contract

<!-- Delete this section if no endpoint changes. -->
| Method | Route | Roles | Change |
|---|---|---|---|
| | | | new / changed / removed |

- Breaking changes for existing clients (response shape, status codes, routes): none / describe

## Persistence

<!-- Delete this section if the model is unchanged. -->
- Migrations: `<timestamp>_<Name>`, and what `Up` does
- [ ] `dotnet ef migrations has-pending-model-changes` reports no changes
- [ ] No new foreign key to an External Identity (customer, branch, product)

## Configuration

- [ ] No hardcoded host, port, URL, connection string, or credential; new values come from config or environment variables
- New config keys: none / `Section:Key` (where it is set)

## Tests

- Unit: <N> new, <total> passing (`dotnet test tests/Ambev.DeveloperEvaluation.Unit`)

- Unit: <N> new, <total> passing (`dotnet test tests/Ambev.DeveloperEvaluation.Unit`)
- Integration / functional: none / describe
- Manual verification: none / steps and result
- Test gaps accepted in this PR (tracked as TD): none / TD-000

## Checklist

- [ ] Base branch is `dev` (Git Flow)
- [ ] Every new type and every changed member has its `// Work item:` comment
- [ ] Follows the existing layer pattern (Request/Validator/Response/Profile in WebApi; Command/Handler/Validator/Result/Profile in Application)
- [ ] XML doc comments on public members; file-scoped namespaces
- [ ] Build has no new warnings

## Follow-ups

<!-- Items left for later, with their work-item IDs. -->
