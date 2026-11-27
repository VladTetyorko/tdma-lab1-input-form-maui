# Rules and workflow

**Rules:** [`AGENTS.md`](../AGENTS.md) (scope, SOLID/DRY/KISS, layers, tests,
workflow). **Plan:** [`docs/plan.md`](plan.md).

The lab PDFs (kept locally in `Mobile_apps_L1_Model_view/docks/`, not in git)
describe *general* techniques, not exact steps for this app. Each lab is
handled like this:

1. **Read the lab** and list what it asks for.
2. **Compare with the current app**: what already exists, what is missing.
3. **Break it down** into small tasks that fit the warehouse domain, plus any
   related work the step needs (tests, refactoring, UI).
4. **One pull request per lab.** No direct commits to `master`.
5. **Write a step document** in [`docs/steps/`](steps) for each PR:
   - what the app had before;
   - what the lab asked for;
   - how we read it for the warehouse domain;
   - which decisions we made and why;
   - the PR link and a short summary of the changes.

Template: [`docs/steps/_template.md`](steps/_template.md).

### Branches and PRs

- One branch per lab, stacked: `lab1` → `master`, `lab2` → `lab1`,
  `lab3` → `lab2`, `lab4` → `lab3` (the course asks for branches named `lab3`, `lab4`).
- Commit and PR titles: `[Lab N] <what changed>`.
- Every PR that changes behaviour updates or adds tests in
  `Mobile_apps_L1_Model_view.Tests`.
