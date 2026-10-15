# Step 01 — Migrate Student → warehouse entities

- **Lab:** 1 (MVVM, data binding, validation), reapplied to the warehouse domain
- **Branch:** `lab1`
- **PR:** _to be added_

## 1. Before

Lab 1 on a `Student` model: `StudentViewModel` with validation, an
`ObservableCollection<Student>`, `AddStudentCommand`, a
`HighScoreToColorConverter`, one page, unit tests. No warehouse code.

## 2. What the lab asks for

Lab 1: a model, a ViewModel with `INotifyPropertyChanged`, two-way binding,
input validation, an `ICommand` that adds an item to an observable list.

## 3. Our interpretation

The same techniques, moved to the domain from
[`../domain/entities.md`](../domain/entities.md). Done as a series of commits:

| # | Commit | Status |
|---|--------|--------|
| 1 | Domain entities: `Entity`, `OwnedEntity`, `User`, `Product`, `Characteristic`, `ProductCharacteristic`, `Operation` and enums, with tests | done |
| 2 | Current-user service (ownership): pick one of three seeded users | planned |
| 3 | `ProductFormViewModel`: name required, EAN-13 barcode with check digit; replaces `StudentViewModel` | planned |
| 4 | UI: add-product form and product list; converter for out-of-stock | planned |
| 5 | Remove `Student`, move remaining tests | planned |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| `Entity` / `OwnedEntity` base classes | Repeat `Id`, timestamps, owner in every class | One place for the common rules; `User` is not owned |
| `Guid.CreateVersion7()` for IDs | `Guid.NewGuid()` (v4) | Time-ordered: better index locality in SQLite (Lab 4), IDs sort by creation |
| Timestamps set in the constructor, UTC | Set by a service / repository | No storage layer yet; UTC avoids time-zone bugs in export and sync |
| IDs only, no navigation properties | `Product.Characteristics` collections | Matches future tables one to one; joins belong to ViewModels / repositories |
| `Operation` and link IDs use `init` setters | Mutable properties | Operations are immutable by rule; links never move to another product |
| `Characteristic.ValueType` is `init` | Settable | Changing the type would invalidate existing values |
| Student code kept until commit 5 | Delete it in commit 1 | Every commit builds and passes tests |

## 5. Changes in the PR

- `Models/`: `Entity`, `OwnedEntity`, `User`, `Role`, `Product`,
  `Characteristic`, `CharacteristicValueType`, `ProductCharacteristic`,
  `Operation`, `OperationDirection`.
- `Tests/EntityTests.cs`: ID version and uniqueness, UTC timestamps, ownership, links.

## 6. Open questions / next steps

- Commits 2–5 above.
