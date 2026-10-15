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
| 2 | Clock, current-user service (ownership), role permissions, `Result` | done |
| 3 | CommunityToolkit.Mvvm package | done |
| 4 | EAN-13 helper, `IProductValidator`, async repositories (in memory), `DataSeeder`, `IStockService`, `IProductService` | done |
| 5 | `LoginViewModel`, `ProductListViewModel`, `ProductFormViewModel`, `AddStockRoomCore` DI extension, `QuantityToColorConverter` | done |
| 6 | Login, products and add-product pages; `Student` code removed; app renamed to StockRoom | done |
| 7 | Tests for the new code (EndToEnd flows included) | planned (test agent) |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| `Entity` / `OwnedEntity` base classes | Repeat `Id`, timestamps, owner in every class | One place for the common rules; `User` is not owned |
| `Guid.CreateVersion7()` for IDs | `Guid.NewGuid()` (v4) | Time-ordered: better index locality in SQLite (Lab 4), IDs sort by creation |
| Timestamps set in the constructor, UTC | Set by a service / repository | No storage layer yet; UTC avoids time-zone bugs in export and sync |
| IDs only, no navigation properties | `Product.Characteristics` collections | Matches future tables one to one; joins belong to ViewModels / repositories |
| `Operation` and link IDs use `init` setters | Mutable properties | Operations are immutable by rule; links never move to another product |
| `Characteristic.ValueType` is `init` | Settable | Changing the type would invalidate existing values |
| Student code kept until the UI commit | Delete it in commit 1 | Every commit builds and passes tests |
| CommunityToolkit.Mvvm (`ObservableObject`, `[ObservableProperty]` as partial properties, `[RelayCommand]`) | Hand-written `INotifyPropertyChanged` and `Command` | DRY: no boilerplate; partial properties avoid the WinRT/AOT warning of field-based generation |
| Validation in `IProductValidator` (`Ean13` is a pure static helper) | Rules inside the ViewModel | Testable without UI; reused by `ProductService` and the form, so the rules exist once |
| Barcode format is validated on every keystroke, uniqueness only on Save | Async uniqueness check on each change | Uniqueness needs the repository (async in Lab 4); checking on Save avoids races and fire-and-forget code |
| `ProductService.AddAsync` re-validates and checks the role | Trust the ViewModel | The service is the authority; the ViewModel only mirrors the rules for friendly messages |
| Async repositories, one interface per aggregate; `IOperationRepository` has no update/delete | One generic `IRepository<T>` with Update/Delete | Operations are immutable; Lab 4 swaps implementations only. Shared storage lives in `InMemoryRepository<T>` |
| Quantity on hand computed in one private method of `StockService` | Stored column / calculation in ViewModels | 3NF, DRY; the same code serves one product and the whole list |
| `StockService.RecordAsync` returns `Result<Operation>` | Exceptions | Expected failures (role, quantity, stock) are user messages, not errors |
| Entities get their timestamps from `IClock` in services | `DateTime.UtcNow` only in the entity constructor | Time is controllable in tests |
| Minimal `INavigationService` (`GoToAsync(route)`) and `Routes` now | Navigate from code-behind / wait for Lab 2 | Login, list and form need navigation; ViewModels must not touch `Shell`. Lab 2 extends it |
| Add-product form is a separate page (route `productform`); the list reloads in `OnAppearing` | Form and list on one page | One BindingContext per page; keeps each ViewModel small |
| `AddStockRoomCore` extension; pages and `INavigationService` registered in `MauiProgram` | Everything in `MauiProgram` | Tests build the same container with a fake navigation service |
| Seeding runs once at startup, synchronously | Seed inside `LoginViewModel` | In-memory seeding is instant; ViewModels stay unaware of it. Lab 4 uses migrations |
| `QuantityToColorConverter` returns `UnsetValue` for non-zero quantities | Always return a color | Keeps the theme colors from the style |

## 5. Changes in the PR

- `Models/`: `Entity`, `OwnedEntity`, `User`, `Role`, `Product`,
  `Characteristic`, `CharacteristicValueType`, `ProductCharacteristic`,
  `Operation`, `OperationDirection`.
- `Services/`: `IClock`/`SystemClock`, `ICurrentUserService`/`CurrentUserService`,
  `RolePermissions`, `Result<T>`, `Ean13`, `IProductValidator`/`ProductValidator`,
  `IProductService`/`ProductService`, `IStockService`/`StockService`,
  `INavigationService`/`ShellNavigationService`, `Routes`.
- `Data/`: five repository interfaces, in-memory implementations
  (`InMemoryRepository<T>` base), `IDataSeeder`/`DataSeeder`.
- `ViewModels/`: `LoginViewModel`, `ProductListViewModel` (+ `ProductListItem`),
  `ProductFormViewModel`.
- `Views/`: `LoginPage`, `ProductsPage`, `ProductFormPage`; `AppShell` with routes.
- `Converters/`: `QuantityToColorConverter` (0 units is red).
- `ServiceCollectionExtensions.AddStockRoomCore`, `MauiProgram` registrations.
- Removed: `Student`, `StudentViewModel`, `HighScoreToColorConverter`, `MainPage`, their tests.
- Shared styles: `ErrorLabel`, `CaptionLabel`, `QuantityLabel`, color `Danger`.
- Tests: models and services so far; the rest is added in a separate commit.

## 6. Open questions / next steps

- Commit 7 above (repositories, services, ViewModels and end-to-end flow tests).
- Lab 2 extends `INavigationService` with parameters and going back with data.
