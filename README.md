# StockRoom — a warehouse app built lab by lab

StockRoom is a .NET MAUI (C#, MVVM) mobile app for running a small warehouse.
It is also a **teaching project**: it is built one lab at a time for the
*Mobile Development (.NET MAUI)* course, and the repository records every step
of building mobile software, from a single MVVM form to an offline-first app
backed by a database.

## What we are building

A deliberately **simple** warehouse app: one warehouse, three people, three
roles, about ten products. It does four things:

- shows the **product list** with characteristics and quantity on hand;
- **sorts** the list by any characteristic;
- lets the manager **add products and characteristics**;
- **scans a barcode**, then the user chooses **In** or **Out** and enters the
  **number of units**. A barcode identifies a product (a kind of unit), so
  one scan can record many units.

Around these it behaves like a real mobile app: it **keeps unsaved forms and the
signed-in user** when minimised or killed, has a **System/Light/Dark theme**,
opens a product from a **deep link** (`stockroom://product/{barcode}`), and
scans with the **camera** on Android and iOS (manual entry on Windows).

Entities use UUID keys, `CreatedAt`/`UpdatedAt` and an owner (`CreatedByUserId`),
and are designed in third normal form. Since Lab 4 data lives in **SQLite**, a
single file (`stockroom.db3`) with versioned migrations, that the app can
share from Settings. It works **offline first**: every change is saved locally
and queued, and the queue is sent to a (stub) server when a network is
available. The counterparty of an operation (personal data) is stored
**encrypted**; products can be **edited and deleted** (deleted only while they
have no operations).

Details: [`docs/domain/domain-model.md`](docs/domain/domain-model.md) (overview, roles)
and [`docs/domain/entities.md`](docs/domain/entities.md) (every entity, its fields and relations).

## Why a warehouse

The course's sample domains (a student list, a railway ERP) only show the
techniques. A small warehouse gives every lab a real job to do without
growing into a big system:

| Lab | Course topic | What it becomes in StockRoom |
|-----|--------------|------------------------------|
| 1 | MVVM, data binding, `INotifyPropertyChanged`, validation | Login, product list with quantities, add-product form with validation (name, EAN-13, unique barcode) |
| 2 | Shell navigation, `ICommand`, routes, passing parameters | Product list → details → edit characteristics and return; add characteristics; sort; In/Out page |
| 3 | App and page lifecycle, DI, saving state, theme, deep links | App state service, drafts survive backgrounding, reload on `OnAppearing`, theme switch, `stockroom://product/{barcode}`, **barcode scanner** (camera stops on `OnDisappearing`) |
| 4 | SQLite, repository pattern, CRUD, migrations, sync, encryption | Entities mapped to SQLite tables, repositories, schema migrations via `PRAGMA user_version`, database export |


## How the project is developed

**Rules:** [`AGENTS.md`](AGENTS.md) (scope, SOLID/DRY/KISS, layers, tests,
workflow). **Plan:** [`docs/plan.md`](docs/plan.md).

The lab PDFs (kept locally in `Mobile_apps_L1_Model_view/docks/`, not in git)
describe *general* techniques, not exact steps for this app. Each lab is
handled like this:

1. **Read the lab** and list what it asks for.
2. **Compare with the current app**: what already exists, what is missing.
3. **Break it down** into small tasks that fit the warehouse domain, plus any
   related work the step needs (tests, refactoring, UI).
4. **One pull request per lab.** No direct commits to `master`.
5. **Write a step document** in [`docs/steps/`](docs/steps) for each PR:
   - what the app had before;
   - what the lab asked for;
   - how we read it for the warehouse domain;
   - which decisions we made and why;
   - the PR link and a short summary of the changes.

Template: [`docs/steps/_template.md`](docs/steps/_template.md).

### Branches and PRs

- One branch per lab, stacked: `lab1` → `master`, `lab2` → `lab1`,
  `lab3` → `lab2`, `lab4` → `lab3` (the course asks for branches named `lab3`, `lab4`).
- Commit and PR titles: `[Lab N] <what changed>`.
- Every PR that changes behaviour updates or adds tests in
  `Mobile_apps_L1_Model_view.Tests`.

## Step log

| Step | Lab | Document | Status |
|------|-----|----------|--------|
| 00 | — | [Project vision and domain model](docs/steps/00-vision-and-roles.md) | in review |
| 01 | 1 | [Warehouse domain, MVVM, validation](docs/steps/01-warehouse-entities.md) | done (145 tests) |
| 02 | 2 | [Shell navigation, parameters, characteristics, sorting, In/Out page](docs/steps/02-navigation.md) | done (310 tests) |
| 03 | 3 | [App and page lifecycle, saved state, theme, deep links, barcode scanner](docs/steps/03-lifecycle.md) | done (469 tests) |
| 04 | 4 | [SQLite, repositories, CRUD, migrations, sync queue, encryption](docs/steps/04-sqlite.md) | in progress |

## Decision log

Every architectural decision, newest last. Details are in the step documents.

| # | Lab | Decision | Why |
|---|-----|----------|-----|
| 1 | — | Warehouse domain, kept small (3 users, ~10 products) | Real reasons for every lab technique without building an ERP |
| 2 | — | Characteristics as rows (`Characteristics` + `ProductCharacteristics`) | Add characteristics at runtime without schema changes |
| 3 | — | One `Operations` table; quantity on hand is computed | 3NF; one source of truth for stock |
| 4 | — | UUID keys, `CreatedAt`/`UpdatedAt`, `CreatedByUserId` on every entity | Offline/sync-safe IDs; every row records who and when |
| 5 | — | Schema designed now, SQLite added in Lab 4 | Follows the course order; Lab 4 only adds storage |
| 6 | 1 | `Entity`/`OwnedEntity` base classes, UUID v7 | DRY; time-ordered IDs index well |
| 7 | 1 | Relations as IDs only, no navigation properties | Entities match tables; joins live in services |
| 8 | — | Stacked branches `lab1`…`lab4`, one PR per lab | Each lab reviewable alone; matches course branch names |
| 9 | — | Code, docs and UI in English | Project owner's choice |
| 10 | 1 | CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`) | Removes `INotifyPropertyChanged`/`Command` boilerplate (DRY) |
| 11 | 1 | Validation in `IProductValidator` (+ `Ean13`), not in ViewModels | Testable without UI; one set of rules for form and service |
| 12 | 1 | Async repository interfaces, in-memory implementations | Lab 4 swaps implementations without touching callers |
| 13 | 1 | `IStockService` computes quantity on hand and records operations, returns `Result` | One place for stock rules (role, quantity above 0, Out not above on hand) |
| 14 | 1 | `AddStockRoomCore` DI extension; pages and navigation registered in `MauiProgram` | Tests build the same container with fake platform services |
| 15 | 1 | Minimal `INavigationService` and `Routes` introduced in Lab 1 | ViewModels must not use `Shell`; Lab 2 extends it |
| 16 | 2 | `INavigationService.GoToAsync(route, parameters)`; objects travel in a dictionary and arrive through `IQueryAttributable` | Rich objects without serialisation; navigation stays fakeable |
| 17 | 2 | Data returns with `GoToAsync("..", parameters)` handled by the list's `IQueryAttributable`, not a messenger | Neighbouring pages; the link is visible; no hidden subscriptions |
| 18 | 2 | The list loads once per signed-in user and is updated by returned parameters; back arrow is bound to `GoBackCommand` | No reload workaround; every back path returns the data |
| 19 | 2 | `ProductSummary` (Services) replaces `ProductListItem`; `IProductCatalog` is the read side | Sorter and catalog must not depend on ViewModels; one join for list and detail |
| 20 | 2 | Characteristic rules in `ICharacteristicService` / `ICharacteristicValidator`; numbers parsed with the invariant culture in one helper | Testable; one definition of a valid number |
| 21 | 2 | Sorting in the pure `ProductSorter`; missing values always last | Testable; useful rows stay on top when descending |
| 22 | 2 | `OperationPage` takes the product from navigation parameters | Lab 3's scanner opens it the same way |
| 23 | 3 | `App` only forwards the six window events to `IAppLifecycleService`; state is saved on `Stopped`/`Destroying` and restored once at start | Testable without `Application`; warm resume needs no restore because memory is intact |
| 24 | 3 | `IAppStateService` (singleton) over `IPreferencesStore`: user id, last route, one JSON draft per draft type | ViewModels never touch `Preferences`; no key constants |
| 25 | 3 | Drafts for the product form, operation form and detail edits; cleared by save/cancel/back/sign-out, not by `OnDisappearing`; products in drafts are named by barcode; seeded users have fixed ids | `OnDisappearing` also fires on minimise; ids of in-memory data change on every start, barcodes do not |
| 26 | 3 | `IPageLifecycleAware` + `PageLifecycleBehavior` attached in XAML instead of `OnAppearing` overrides | DRY; code-behind free of lifecycle code |
| 27 | 3 | `IThemeService` (preferences) + `IThemeApplier` (`UserAppTheme`); applied before the first page | Testable logic, one class touches `Application` |
| 28 | 3 | Deep link `stockroom://product/{barcode}`: pure `DeepLinkParser`, `IDeepLinkHandler` (`HandleAsync`/`Defer`/`HandlePendingAsync`); Android intent filter, Windows command line, "Open a link" box in Settings | Works before Shell exists and before sign-in; testable on every platform |
| 29 | 3 | `IBarcodeScanner` = camera capability + permission; the camera is a `CameraScannerView` control (ZXing.Net.Maui.Controls, android/ios TFMs only), Windows uses manual entry | The library is control-based; Windows stays fully usable and free of the camera package |
| 30 | 3 | No background work in Lab 3 (documented); Lab 4's sync decides on WorkManager | Nothing to refresh in an in-memory single-writer app |
| 31 | 4 | `sqlite-net-pcl` + raw SQL migrations (embedded `NNNN_*.sql`, `PRAGMA user_version`, one transaction per script) | Constraints (`CHECK`, composite `UNIQUE`, `COLLATE NOCASE`, FKs) cannot be expressed by `CreateTable`; the SQL is the schema's source of truth |
| 32 | 4 | Storage rows (`ProductRow`, ...) with text ids/enums/ISO timestamps; repositories map entity to row | Entities stay free of storage attributes; the file is readable in any SQLite viewer |
| 33 | 4 | `SqliteRepository<TEntity, TRow>` base writes the change and its `SyncQueue` row in one transaction (`SyncJournal`) | DRY and atomic: no decorator per interface, no change without a queue entry |
| 34 | 4 | `IDatabase` opens, configures (foreign keys, WAL) and migrates lazily; the path comes from `IDatabasePathProvider` | A repository can never meet a missing schema; tests use a temporary file |
| 35 | 4 | Seed data is migration `0002_seed.sql` with the fixed ids | Runs once, needs no service, and is not queued for sync |
| 36 | 4 | Startup is async: `StartupPage` while `RestoreAsync` opens the database, then the shell | Removes the sync-over-async in `MauiProgram`/`App` |
| 37 | 4 | Delete product only without operations (button disabled with a note; service and FK enforce it); values removed with the product, each queued | History must stay complete; operations are immutable |
| 38 | 4 | Offline sync: `SyncQueue` outbox, `SyncService` push (oldest first, stop on failure, retry), `ISyncApi` + in-memory `StubSyncApi`, `IConnectivityMonitor`, "Simulate offline" switch | Lab asks for a queue and a stub; the switch makes the demo work on Windows |
| 39 | 4 | Conflicts: last write wins by change time, delete tombstones, push is idempotent | Simple, deterministic, no UI; conflicts are rare in this domain |
| 40 | 4 | Sync triggers: connectivity change, app start/resume, manual; no background service | Foreground warehouse app; WorkManager is listed as a next step (decision 30) |
| 41 | 4 | `Operation.Counterparty` encrypted with AES-256-CBC + HMAC-SHA256 (`IFieldEncryptor`), key in `SecureStorage` (`ISecretStore`); undecryptable value shows a placeholder | Personal data protected, the rest of the export stays readable; CBC+HMAC runs on every target |
| 42 | 4 | "Share database": checkpoint WAL, copy the file, `Share.RequestAsync` behind `IFileSharer` | A complete file the user can open on a PC |
| 43 | 4 | In-memory repositories moved to the test project | Dead code in the app otherwise; still fast test doubles |

## Architecture

```mermaid
graph TD
    subgraph Views
        AppShell
        LoginPage
        ProductsPage
        ProductFormPage
        ProductDetailPage
        CharacteristicsPage
        OperationPage
        SettingsPage
        ScannerPage
        CameraScannerView
        PageLifecycleBehavior
    end
    subgraph ViewModels
        LoginViewModel
        ProductListViewModel
        ProductFormViewModel
        ProductDetailViewModel
        CharacteristicsViewModel
        OperationViewModel
        SettingsViewModel
        ScannerViewModel
        IPageLifecycleAware
    end
    subgraph Converters
        QuantityToColorConverter
    end
    subgraph Services
        IStockService
        IProductService
        IProductValidator
        IProductCatalog
        ICharacteristicService
        ICharacteristicValidator
        ProductSorter
        ICurrentUserService
        INavigationService
        IClock
        RolePermissions
        Ean13
        IAppStateService
        IPreferencesStore
        IAppLifecycleService
        IThemeService
        IThemeApplier
        IDeepLinkHandler
        DeepLinkParser
        IStartupNavigator
        IBarcodeScanner
        IAlertService
        ISyncService
        ISyncApi
        IConnectivityMonitor
        IOfflineSimulator
        IFieldEncryptor
        ISecretStore
        IDatabaseExporter
        IFileSharer
    end
    subgraph Data
        IUserRepository
        IProductRepository
        IOperationRepository
        ICharacteristicRepository
        IProductCharacteristicRepository
        Sqlite["Sqlite*Repository (SqliteRepository base)"]
        IDatabase
        MigrationRunner
        SyncJournal
        ISyncQueue
        Migrations["Migrations/*.sql"]
    end
    subgraph Models
        Entities["User, Product, Characteristic, ProductCharacteristic, Operation"]
    end

    LoginPage --> LoginViewModel
    ProductsPage --> ProductListViewModel
    ProductsPage --> QuantityToColorConverter
    ProductFormPage --> ProductFormViewModel
    LoginViewModel --> IUserRepository
    LoginViewModel --> ICurrentUserService
    LoginViewModel --> INavigationService
    ProductListViewModel --> IProductCatalog
    ProductListViewModel --> ICharacteristicService
    ProductListViewModel --> ProductSorter
    ProductListViewModel --> INavigationService
    ProductDetailPage --> ProductDetailViewModel
    CharacteristicsPage --> CharacteristicsViewModel
    OperationPage --> OperationViewModel
    ProductDetailViewModel --> IProductCatalog
    ProductDetailViewModel --> ICharacteristicService
    ProductDetailViewModel --> ICharacteristicValidator
    ProductDetailViewModel --> INavigationService
    CharacteristicsViewModel --> ICharacteristicService
    CharacteristicsViewModel --> INavigationService
    OperationViewModel --> IStockService
    OperationViewModel --> INavigationService
    IProductCatalog --> IStockService
    IProductCatalog --> IProductRepository
    ICharacteristicService --> ICharacteristicValidator
    ICharacteristicService --> IProductCharacteristicRepository
    ProductFormViewModel --> IProductValidator
    ProductFormViewModel --> IProductService
    IProductService --> IProductValidator
    IProductService --> IProductRepository
    IStockService --> IOperationRepository
    IStockService --> RolePermissions
    IProductValidator --> Ean13
    IProductValidator --> IProductRepository
    Sqlite -.implements.-> IProductRepository
    Sqlite --> IDatabase
    Sqlite --> SyncJournal
    SyncJournal --> ISyncQueue
    IDatabase --> MigrationRunner
    MigrationRunner --> Migrations
    Sqlite --> IFieldEncryptor
    IFieldEncryptor --> ISecretStore
    ISyncService --> ISyncQueue
    ISyncService --> ISyncApi
    ISyncService --> IConnectivityMonitor
    ISyncService --> IOfflineSimulator
    IAppLifecycleService --> ISyncService
    SettingsViewModel --> ISyncService
    SettingsViewModel --> IDatabaseExporter
    IDatabaseExporter --> IDatabase
    IDatabaseExporter --> IFileSharer
    ProductListViewModel --> IProductService
    IProductService --> IOperationRepository
    IProductRepository --> Entities
    SettingsPage --> SettingsViewModel
    ScannerPage --> ScannerViewModel
    ScannerPage --> CameraScannerView
    ScannerPage --> PageLifecycleBehavior
    PageLifecycleBehavior -.calls.-> IPageLifecycleAware
    ProductListViewModel -.implements.-> IPageLifecycleAware
    ScannerViewModel -.implements.-> IPageLifecycleAware
    CameraScannerView -- "DetectedCommand" --> ScannerViewModel
    SettingsViewModel --> IThemeService
    SettingsViewModel --> IDeepLinkHandler
    ScannerViewModel --> IBarcodeScanner
    ScannerViewModel --> IProductCatalog
    ScannerViewModel --> INavigationService
    ProductListViewModel --> IStartupNavigator
    ProductListViewModel --> IAppStateService
    ProductFormViewModel --> IAppStateService
    OperationViewModel --> IAppStateService
    ProductDetailViewModel --> IAppStateService
    App["App (window events)"] --> IAppLifecycleService
    IAppLifecycleService --> IAppStateService
    IAppLifecycleService --> IThemeService
    IAppLifecycleService --> ICurrentUserService
    IAppStateService --> IPreferencesStore
    IThemeService --> IPreferencesStore
    IThemeService --> IThemeApplier
    IStartupNavigator --> IDeepLinkHandler
    IStartupNavigator --> IAppStateService
    IDeepLinkHandler --> DeepLinkParser
    IDeepLinkHandler --> IProductCatalog
    IDeepLinkHandler --> IAlertService
    IDeepLinkHandler --> INavigationService
```

## Repository layout

```
Mobile_apps_L1_Model_view/        MAUI app (Models, Services, Data, ViewModels, Views, Converters)
Mobile_apps_L1_Model_view/Data/Migrations/  Numbered SQL schema scripts (embedded resources)
Mobile_apps_L1_Model_view/docks/  Lab assignments (PDF, Ukrainian)
Mobile_apps_L1_Model_view.Tests/  Unit and end-to-end tests (real SQLite file, fake platform services)
docs/domain/                      Domain model, roles, entity catalogue
docs/steps/                       One document per step / PR
```

## Build and run

Requires the .NET 10 SDK with the MAUI workload.

```bash
dotnet build Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-windows10.0.19041.0
dotnet test Mobile_apps_L1_Model_view.Tests
```

Android: add `-f net10.0-android -p:AndroidSdkDirectory="$LOCALAPPDATA/Android/Sdk"`.

**Database.** The first start creates and seeds `stockroom.db3` in the app data
folder (Windows, unpackaged: `%LOCALAPPDATA%/User Name/com.companyname.mobile_apps_l1_model_view/Data`).
To start over, close the app and delete that file. The Settings page shows the
sync queue, has the **Simulate offline** switch and **Share database**.
Tests never touch it: each test container uses its own temporary file.
