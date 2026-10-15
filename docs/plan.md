# Plan: Labs 1–4

Each lab is one branch and one pull request, built on the previous one. Every
lab ends with: all tests green, Windows and Android builds green, its step
document written, decisions added to the README decision log.

## Lab 1 — Warehouse domain, MVVM and validation (`lab1` → `master`)

Lab topic: model, ViewModel with `INotifyPropertyChanged`, binding, validation, `ICommand`.

1. Domain entities and enums *(done)*.
2. Adopt **CommunityToolkit.Mvvm** (`ObservableObject`, `[RelayCommand]`) to
   remove `INotifyPropertyChanged` boilerplate (DRY).
3. Services: `ICurrentUserService` (who is acting: ownership), `IClock`.
4. Data (in memory): `IUserRepository`, `IProductRepository`,
   `ICharacteristicRepository`, `IOperationRepository` with in-memory
   implementations and seed data (3 users, ~10 products, 3–4 characteristics,
   a few operations). `IStockService` computes quantity on hand and records
   operations (rejects Out > on hand).
5. ViewModels: `LoginViewModel` (pick a user), `ProductFormViewModel` (name
   required, EAN-13 with check digit, unique barcode), `ProductListViewModel`
   (products with quantity).
6. UI: login page, product list + add-product form. Converter: out of stock → red.
7. Remove `Student`, `StudentViewModel`, `HighScoreToColorConverter`; move tests.
   App title → StockRoom.

## Lab 2 — Shell navigation, commands, parameters (`lab2` → `lab1`)

Lab topic: routes, `GoToAsync` from ViewModels via `ICommand`, `[QueryProperty]`
/ `IQueryAttributable`, going back with changed data.

1. `INavigationService` wrapping `Shell.Current.GoToAsync` (testable).
2. Routes registered in `AppShell`: `products`, `productdetail`,
   `characteristics`, `operation`.
3. `OpenDetailsCommand` on the list → `ProductDetailPage` (product,
   characteristic values, quantity, operation history) receiving the product
   via `IQueryAttributable`.
4. Edit characteristic values on the detail page (Manager), `SaveCommand`
   returns the updated product to the list (`GoToAsync("..", parameters)`),
   the list refreshes. `GoBackCommand`.
5. **Characteristics**: page to add a characteristic (name, Text/Number), and
   **sort** the product list by any characteristic (numbers numerically).
6. Role-based visibility (Manager-only actions hidden for others).
7. **In/Out operation page**: choose product, direction, quantity,
   counterparty, reason (this is the target of the scanner in Lab 3).
8. Step doc with answers to the 6 control questions.

## Lab 3 — Lifecycle, DI, state, theme, deep links, scanner (`lab3` → `lab2`)

Lab topic: app/window lifecycle, page lifecycle, DI registration, saving
state, background work, light/dark theme, deep links.

1. `IAppStateService` (singleton): current user, last screen, draft of an
   unsaved form. Saved on `Window.Stopped`/`Destroying`, restored on
   `Resumed`/start (via an `IPreferences` abstraction).
2. Unsaved add-product / operation form drafts survive going to background.
3. `OnAppearing` → `LoadCommand` on list/detail ViewModels; `OnDisappearing`
   releases resources (stops the camera).
4. All pages and ViewModels resolved through DI.
5. **Theme** switch (System/Light/Dark) saved in preferences; `AppThemeBinding` in styles.
6. **Deep link** `stockroom://product/{barcode}` opens the product detail (Android intent filter + Shell route).
7. **Barcode scanner**: `IBarcodeScanner` abstraction; camera implementation on
   Android/iOS, manual entry fallback on Windows. Scan → product found → In/Out page
   with quantity of units; unknown barcode → Manager can create the product.
8. Step doc with answers to the control questions.

## Lab 4 — SQLite, repositories, sync, encryption (`lab4` → `lab3`)

Lab topic: SQLite schema, repository + DI, async load into
`ObservableCollection`, CRUD commands, migrations, offline sync queue, encryption.

1. Add `sqlite-net-pcl`. Migrations as numbered SQL scripts
   (`0001_initial.sql`, `0002_seed.sql`) applied by a `MigrationRunner` using
   `PRAGMA user_version`; schema exactly as in `docs/domain/domain-model.md`
   (UUID text keys, FKs, UNIQUE, CHECK).
2. SQLite repositories replace the in-memory ones (same interfaces, DI swap).
3. `LoadCommand` reads asynchronously; add / edit / delete product commands
   (delete only without operations).
4. **SyncQueue**: every write adds a `SyncQueueItem`; `SyncService` pushes the
   queue to a local stub API when `IConnectivity` reports a network;
   conflict strategy last-write-wins by `UpdatedAt`.
5. **Encryption**: `Operation.Counterparty` (personal data) encrypted with
   AES before saving, key in `SecureStorage`.
6. **Export**: "Share database" action.
7. End-to-end tests against a real temporary SQLite file.
8. Step doc with DB schema, sync description, conflict strategy, answers to
   the control questions.
