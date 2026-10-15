# Step 00 — Project vision and domain model

- **Lab:** none (setup step)
- **Branch:** `docs/00-vision-and-roles`
- **PR:** _to be added_

## 1. Before

Lab 1 is done on a **Student** model: an MVVM input form (`StudentViewModel`)
with validation of the full name, group (`FIT 2-4`) and average score, an
`ObservableCollection<Student>` list, `AddStudentCommand`, a
`HighScoreToColorConverter`, and unit tests. No DI registrations, no
database, one Shell page. The README held only a layer diagram.

## 2. What the lab asks for

Nothing. The labs use sample domains (students, a railway ERP). This step
picks our own domain so every later lab has a real job to do.

## 3. Our interpretation

A **simple warehouse app (StockRoom)**: one warehouse, three users with three
roles, about ten products, products described by characteristics, a sortable
list, In/Out operations, a barcode scanner. See
[`../domain/domain-model.md`](../domain/domain-model.md).

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| Keep it small: list, sort, add product/characteristic, scan In/Out | Dashboards, approvals, stock-out analytics | The goal is learning the lab techniques, not building an ERP |
| Characteristics as rows (`Characteristics` + `ProductCharacteristics`) | One column per characteristic on `Products` | New characteristics are added at runtime without a schema change |
| One `Operations` table for all In/Out | Separate deliveries and issues tables | Same columns; quantity on hand is one `SUM` |
| Quantity, first delivery date are computed | Stored on `Products` | Stored copies break 3NF and can drift |
| UUID keys, `CreatedAt`/`UpdatedAt`, `CreatedByUserId` everywhere | Integer autoincrement keys | Rows can be created offline / on several devices without clashes; every row records who and when |
| `Role`, `Direction`, `ValueType` as text enums | Lookup tables | Fixed sets with no attributes; still 3NF; readable export |
| Design the schema now, add SQLite in Lab 4 | SQLite from the start | Follows the course order; entities already match the future tables, so Lab 4 only adds storage |
| Demo login (pick one of three users) | Real authentication | Not a lab topic |
| Docs and code in English | Ukrainian | Project owner's choice |

## 5. Changes in the PR

- `README.md`: project vision, lab-to-feature mapping, workflow, step log.
- `docs/domain/domain-model.md`: roles, permissions, schema, computed values, scan flow.
- `docs/domain/entities.md`: entity catalogue with class diagram, fields, relations, permissions, scaling.
- `docs/steps/_template.md`, `docs/steps/00-vision-and-roles.md`.
- No code changes.

## 6. Next steps

1. **Step 01 — migrate Student → warehouse entities, ViewModels and UI** (`feat/01-warehouse-entities`).
2. Lab 2 (navigation).
3. Lab 4: SQLite, repositories, migrations.
