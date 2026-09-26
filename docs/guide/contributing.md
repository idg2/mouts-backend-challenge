# Branching and pull requests

How changes reach `dev` and `main`. Back to the [README](../../README.md).

Up to pull request #11 each change was committed on `dev` and released to `main` through its own pull request (#3 to #11). From pull request #12 on, every change starts on a `feature/<ITEM-ID>` or `bugfix/<ITEM-ID>` branch, where `<ITEM-ID>` is the work item from `work-items.json`, and reaches `dev` through a pull request. `dev` goes to `main` only as a release.

Commit messages use semantic prefixes (`feat:`, `fix:`, `test:`, `chore:`, `docs:`).
