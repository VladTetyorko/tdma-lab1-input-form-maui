# Step 00 — Project vision and role model

- **Lab:** none (a setup step before Lab 2)
- **Branch:** `docs/00-vision-and-roles`
- **PR:** _to be added_

## 1. Before

Lab 1 is done on a **Student** model: an MVVM input form (`StudentViewModel`)
with validation of the full name, group (`FIT 2-4`) and average score, an
`ObservableCollection<Student>` list, `AddStudentCommand`, a
`HighScoreToColorConverter`, and unit tests. The README held only a layer diagram.

## 2. What the lab asks for

Nothing. The labs (2–4) use sample domains (students, a railway ERP). This
step picks our own domain so that every later lab has a real job to do.

## 3. Our interpretation

We build a **warehouse app (StockRoom)** with three roles: storekeeper,
supplier driver and warehouse manager. See [`../domain/roles.md`](../domain/roles.md).

| Upcoming lab | How the warehouse domain uses it |
|--------------|----------------------------------|
| 2 — navigation, parameters | Product list → details → edit and return |
| 3 — lifecycle, DI, state, theme, deep links | Session/role service, restore last screen, theme, `stockroom://product/{id}` |
| 4 — SQLite, repositories, sync, encryption | Local database, sync queue for offline drivers, encrypted credentials |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| Warehouse domain | Keep students; railway ERP from the labs | Gives real reasons for roles, dashboards, offline work and a database |
| Three roles: storekeeper, supplier driver, manager | Add an auditor / accountant role | Three roles cover every flow; a read-only role can be added later |
| One role per user | Users with several roles | Simpler permission checks and UI |
| Stock changes only through `StockMovement` | Edit `StockLevel.Quantity` directly | Full history, approvals, stock-out history and dashboards come out of the same data |
| One warehouse with locations | Several warehouses | Enough for the labs; can grow later |
| Demo login (choose an account) until Lab 4 | Real auth now | Auth is not a lab topic; it would slow down the steps that are |
| Docs and code in English | Ukrainian | Requested by the project owner |

## 5. Changes in the PR

- `README.md`: project vision, lab-to-feature mapping, development workflow.
- `docs/domain/roles.md`: roles, permission matrix, entities, rules, dashboards, flows.
- `docs/steps/_template.md`: template for step documents.
- `docs/steps/00-vision-and-roles.md`: this document.
- No code changes.

## 6. Open questions / next steps

- **Step 01:** move the Lab 1 form from `Student` to `Product` (model,
  ViewModel, validation, tests), keeping the Lab 1 techniques.
- Then Lab 2: product list → product details navigation.
- Should the lab PDFs folder `docks/` be renamed to `docs/labs/`?
