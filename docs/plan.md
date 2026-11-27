# Plan: Labs 1–9

Each lab is one branch and one pull request, built on the previous one. Every
lab ends with: all tests green, Windows and Android builds green, its step
document written, decisions added to the [decision log](decision-log.md).

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

---

# Phase 2 — Labs 5–9 (PDFs: `docks/Лабораторна_робота_{5..9}_*.pdf`)

The labs' running example is a railway ERP; ours stays the warehouse. Lab 9's
variant 8 ("Spare-parts warehouse: items, receipts, issues; roles storekeeper /
administrator; camera + accelerometer shake") is exactly StockRoom, so the final
lab needs no new domain, only completion and defence material.

Every lab follows the **iteration cycle** in `AGENTS.md` §2b and the
cross-cutting rules in §2a. Stacking: `plan-labs-5-9` → `catalog` → `lab5` →
`lab6` → `lab7` → `lab8` → `lab9`.

| Branch | Theme | Migration | New packages |
|--------|-------|-----------|--------------|
| `plan-labs-5-9` | rules + this plan (docs only) | — | — |
| `catalog` | real open barcode data, QR scanning, Open Food Facts lookup | edit `0002_seed.sql` (pre-release DB reset) | — |
| `lab5` | Serilog, global exception handling, log viewer, crash reporter | — | Serilog.Extensions.Logging, Serilog.Sinks.File, Serilog.Sinks.Console, Sentry.Maui |
| `lab6` | login + password hash, token, session, roles, biometrics, cert pinning | `0003_auth.sql` | Plugin.Maui.Biometric |
| `lab7` | permissions, product photo, operation location, map, shake, network indicator | `0004_device.sql` | Microsoft.Maui.Controls.Maps |
| `lab8` | `StockRoom.Core` library, Moq tests, CI, AutomationId, UI tests | — | Moq, FluentAssertions 6.x, Appium client |
| `lab9` | integration, search, README + access matrix + ER, APK, `v1.0` | — | — |

## Catalog (`catalog` → `plan-labs-5-9`) — real data and fast scanning

Not a lab, but you asked for a fast scan-and-record app on open barcode data,
and every later lab uses this data.

1. **Seed**: replace the invented `4820001…` products in `0002_seed.sql` with
   about 12 real products from Open Food Facts (name, brand, quantity, category →
   characteristics `Brand`, `Category`, `Weight (kg)` / `Volume (l)`). Every
   barcode passes `Ean13`. The app is pre-release, so the migration is edited in
   place and a dev database is simply deleted (decision). Add
   `docs/domain/open-data.md`: source, licence (ODbL), attribution text.
2. **QR**: `CameraScannerView` reads `BarcodeFormats.OneDimensional | QrCode`.
   A pure `ScanPayloadParser` turns a scan into a barcode: plain EAN-13, UPC-A
   (padded to EAN-13), or a `stockroom://product/{barcode}` deep link (reuses
   `DeepLinkParser`). Anything else → "not a StockRoom code".
3. **Lookup**: `IProductLookup` (Services) → `OpenFoodFactsLookup` (HTTP, one
   `HttpClient` from DI, 5 s timeout, `User-Agent` without personal data).
   Returns `LookupResult` (name, brand, quantity text, category), "not found" or
   "offline". Used by `ScannerViewModel` when a barcode is unknown: the Manager
   gets "Create from Open Food Facts" → product form prefilled; others see the
   existing "unknown barcode" message.
4. **Quick flow**: after a successful scan the In/Out page opens with the
   quantity field focused. No other UI work.
5. **Tests**: `ScanPayloadParser` (theory), lookup parsing from canned JSON via a
   fake `HttpMessageHandler`, scanner ViewModel with a fake lookup, seed test
   (all barcodes valid and unique).

## Lab 5 — Logging, errors, analytics (`lab5` → `catalog`)

Lab asks: Serilog (console + rolling file); log key events with right levels;
global handlers for `AppDomain` / `TaskScheduler`; simulate and verify;
crash-reporting service; in-app log viewer with level filter; answers to 6 questions.

| Task | Where | What |
|------|-------|------|
| Serilog | `MauiProgram` only | `builder.Logging.AddSerilog(...)`: Console/Debug sink + file `logs/stockroom-.log`, daily rolling, 7 files kept; fixed output template `{Timestamp:o} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}`. Folder via `ILogPathProvider` (Services) so tests use a temp dir |
| Key events | ViewModels, `SqliteRepository`, `SyncService`, `StockService`, `ProductService` | `ILogger<T>` injected (`AddLogging()` is already in the core registration): load, save ok/fail, sync pushed/failed/offline, role denied (Warning). Rules in `AGENTS.md` §2a |
| Global handlers | `Services/UnhandledExceptionLogger` + wiring in `MauiProgram` / `Platforms/*` | `AppDomain.UnhandledException` → Critical; `TaskScheduler.UnobservedTaskException` → Error + `SetObserved`; Android `UnhandledExceptionRaiser`; Windows `Application.UnhandledException`. The class takes `ILogger` and is unit-tested by calling its handlers |
| Simulate | Settings, **Manager only**, "Diagnostics" section | "Throw in background task" (faulted unobserved Task + `GC.Collect`), "Write test log entries", "Send test crash" (enabled only when a DSN is set) |
| Crash service | `ICrashReporter` + `SentryCrashReporter` / `NullCrashReporter` | Sentry: App Center was retired in 2025 and Crashlytics has no official MAUI binding. DSN from an untracked `appsettings.local.json` or env var; without it the null reporter is used. `SendDefaultPii = false`, no personal data in events |
| Log viewer | `ILogReader` + `LogsViewModel` + `LogsPage` | reads the newest files, parses lines, last N (default 200), minimum-level filter, newest first, shows exception text; Manager only; route `logs`, linked from Settings |
| Report | step doc | log excerpt, viewer screenshot, Sentry screenshot = **owner actions** |

Tests: `Services/` (line parser, reader with temp files, handlers), `ViewModels/`
(LogsViewModel filter), a capturing `ILogger` fake for log assertions in a few
VM/service tests, EndToEnd (real Serilog to a temp dir → read back).
Questions 7–12 answered in `docs/steps/05-logging.md`.

## Lab 6 — Authentication, roles, security (`lab6` → `lab5`)

Lab asks: login screen + token in `SecureStorage`; session service with the
role; role-based UI binding; role check in a critical command with an
explanation; biometrics; certificate pinning.
**Scope (your instruction):** local accounts only; passwords are never stored,
only hashes.

| Task | Where | What |
|------|-------|------|
| Schema | `Data/Migrations/0003_auth.sql` | `Users` gets `Login` (unique, NOCASE) and `PasswordHash`; seeded users get PBKDF2 hashes of documented demo passwords (README lists the demo logins and passwords; the DB holds hashes only). `User` gets the two properties; no password field exists in memory beyond a method argument |
| Hashing | `IPasswordHasher` / `Pbkdf2PasswordHasher` | rules in `AGENTS.md` §2a; `Hash`, `Verify` (constant time), `NeedsRehash` |
| Auth | `IAuthService` / `AuthService` | `LoginAsync(login, password)` → `Result<User>`; same message for unknown login and wrong password; 5 failures → 30 s lockout (in memory, injected `IClock`); Warning log without the password |
| Token | `ITokenService` / `HmacTokenService` | token = `base64url(userId.expiry).HMAC` with a key kept in `ISecretStore`; 30-day expiry; `Validate` → userId or null. Stored through `ISecretStore`; sign-out removes it |
| Session | extend `ICurrentUserService` (not a second service): `CurrentRole`, `IsInRole`; `RestoreAsync` validates the stored token instead of trusting a saved user id | the lab's `ISessionService` = our `ICurrentUserService` (decision: no second source of truth) |
| UI | `LoginPage` (login + password, error label, biometric button) | replaces "pick a user"; existing `CanManage…` bindings stay, `IsManager` added where missing |
| Command check | `ProductService.DeleteAsync`, `CharacteristicService`, `StockService` already return `Result` | ViewModels show the refusal reason through `IAlertService` if a forbidden command is invoked; the button is also hidden. Both layers tested |
| Biometrics | `IBiometricAuthenticator` / `MauiBiometricAuthenticator` (Plugin.Maui.Biometric; Windows Hello on Windows) | offered only if a valid token exists, "Biometric unlock" is on (`IPreferencesStore`) and hardware is available; success restores the session from the token; failure falls back to the password form |
| Pinning | `ICertificatePinValidator` (pure: SHA-256 of the public key vs configured pins) + `PinnedHandlerFactory` + `HttpSyncApi : ISyncApi` | `StubSyncApi` stays the default; `HttpSyncApi` is used when `Sync:BaseUrl` is configured and is the only client that pins |

Tests: hasher (known vector, wrong password, rehash), token (tamper, expiry),
auth (lockout with a fake clock), role matrix as a `[Theory]` over
`RolePermissions`, ViewModel refusal messages, pin validator with generated
certificates, EndToEnd: login → token stored → restart → restored → sign-out
clears. Questions 7–12 in `docs/steps/06-auth.md`. The step doc states plainly
that client-side role checks are not a security boundary without a server
(question 12).

## Lab 7 — Device capabilities (`lab7` → `lab6`)

Lab asks: permissions in the manifest and at run time; an entity with a photo
and GPS saved through a repository; handle denial / GPS off / no camera /
cancel with messages and logs; a map with pins (+ Windows alternative); a shake
gesture with sensor lifecycle; a connectivity indicator with auto-sync.
The lab's `WagonInspection` maps onto the warehouse like this (written decision):

| Lab idea | StockRoom |
|----------|-----------|
| Photo of the defect | `Product.PhotoPath`: photo of the item (camera or gallery), file in `AppDataDirectory/photos/<barcode>_<timestamp>.jpg`, DB stores the relative path, thumbnail on detail and form |
| GPS of the inspection | optional `Operation.Latitude/Longitude`: "attach my location" on the In/Out page, mainly a driver's drop-off point; shown on the history item |
| Map of inspections | `MapPage` with pins for operations that have a location (`ItemsSource` bound); on Windows "Open in maps" through `IMapLauncher` (`Map.Default.OpenAsync`) |
| Shake → new incident | shake on the product list or scanner opens the scanner ("quick scan"); sensor started in `OnAppearing`, stopped in `OnDisappearing` |
| Connectivity indicator | offline banner bound to the existing `IConnectivityMonitor` / `IOfflineSimulator` plus a "syncing…" state; auto-sync on reconnect exists since Lab 4, now visible and tested |

| Layer | Add |
|-------|-----|
| Migration | `0004_device.sql`: `Products.PhotoPath TEXT NULL`, `Operations.Latitude/Longitude REAL NULL` with `CHECK` ranges |
| Models and rows | `Product.PhotoPath`, `Operation.Location` (`GeoPoint?`), row mapping; the sync payload carries them (photo bytes are **not** synced, path only; documented) |
| Services | `IPermissionService` (rationale text), `IPhotoService` (capture / pick / copy → relative path), `ILocationService` (fresh with timeout → last known), `IShakeDetector`, `IMapLauncher`, `IMainThread`; MAUI implementations over `IMediaPicker` / `IGeolocation` / `IAccelerometer` registered in `MauiProgram` |
| ViewModels | `ProductFormViewModel` / `ProductDetailViewModel`: `TakePhotoCommand`, `PickPhotoCommand`; `OperationViewModel`: `AttachLocationCommand`; `MapViewModel`; shake in `ProductListViewModel` / `ScannerViewModel` |
| Platform | `AndroidManifest.xml` CAMERA, FINE and COARSE location (`uses-feature required=false`), `Info.plist` strings, Google Maps key placeholder (owner action) |
| Errors | each case → `IAlertService` message + log (matrix in the step doc) |

Tests: fakes for every new service; denial / GPS off / no camera / cancel for
each command; shake start and stop with appearing and disappearing; migration
and row mapping; EndToEnd with a fake `ILocationService`. Questions 1–6 in
`docs/steps/07-device.md`.

## Lab 8 — Testing and CI (`lab8` → `lab7`)

Lab asks: platform-free Core library + xUnit / Moq / FluentAssertions project;
abstract the static MAUI APIs; at least 8 ViewModel unit tests including a
`[Theory]`; repository integration tests (CRUD + `SyncQueue`); GitHub Actions
with coverage (≥ 60 % ViewModels) and an Android build; `AutomationId` + 2 UI tests.

| Task | What |
|------|------|
| Core split | new `StockRoom.Core` (`net10.0`): `Models/`, `Services/` (interfaces + pure logic), `Data/` (+ embedded migrations), `ViewModels/`, `ServiceCollectionExtensions`. The app keeps `Views/`, `Converters/`, `Platforms/`, `Maui*` / `Shell*` / `Camera*` implementations, `MauiProgram`. Namespaces are **kept** (`Mobile_apps_L1_Model_view.*`) to avoid touching 150+ files: only projects and folders move (decision). Anything in Core still using `Microsoft.Maui.*` gets an interface first |
| Test project | retargets to plain `net10.0` and references Core only; tests of MAUI-only classes (converters, `Maui*`) go to a small app-side test project or are dropped when they only test framework types |
| Moq / FluentAssertions | added for the new lab tests: list load, `[Theory]` `CanExecute`, repository called on save, Driver delete denied, navigation parameter, logged repository error (≥ 8). Existing fake-based tests stay (no rewrite of 800 tests) |
| Integration | CRUD + `SyncQueue` row on a temp SQLite file exist from Lab 4; add the missing named cases |
| CI | `.github/workflows/ci.yml`: job `test` on ubuntu-latest (`dotnet test --collect:"XPlat Code Coverage"`, ReportGenerator HTML + summary, fail if ViewModel coverage < 60 %, upload artifact); job `build-android` on windows-latest (`dotnet workload install maui-android`, build `-f net10.0-android`), `needs: test` |
| AutomationId | login entries and button, product list, product-form fields and Save, operation form, scanner entry |
| UI tests | `StockRoom.UiTests` (Appium, `[Trait("Category","UI")]`, not in CI): login → list visible; create product → appears in list. Needs an emulator / Windows driver → run by the owner |

Order inside the lab: Core split first (own commit, build and tests green),
then leftover abstractions, then tests, then CI. Questions 1–6 in
`docs/steps/08-testing-ci.md`. Owner actions: push, enable Actions, screenshot
of a green run, run Appium.

## Lab 9 — Final module (`lab9` → `lab8`, tag `v1.0`)

Status: **done** (see `docs/steps/09-final.md`; the tag `v1.0` is the owner's).

Variant 8, **Spare-parts warehouse** (Part, Receipt, Issue → our Product and
Operation In / Out; roles storekeeper / administrator → our Storekeeper and
Manager, plus Driver; camera + accelerometer; we also have scanner and location).

Requirement table (lab §1.2) → where it is satisfied:

| Lab | Requirement | Where / status |
|-----|-------------|----------------|
| 1 | clean Models, ObservableObject, binding, state indicator | `Models/`, `ViewModels/`, `QuantityToColorConverter` — done |
| 2 | commands, Shell routes, list → detail → edit, `IQueryAttributable`, result back | done (Lab 2) |
| 3 | singleton session service, draft survives, theme, deep link | done (Lab 3), session from Lab 6 |
| 4 | ≥ 3 related entities, repositories, versioned schema, `SyncQueue`, one encrypted field | done (Lab 4) |
| 5 | Serilog console + file, levels, global handler, log viewer | Lab 5 |
| 6 | login + token in SecureStorage, ≥ 2 roles, UI + command checks | Lab 6 |
| 7 | ≥ 2 hardware features, permissions + denial handling | Lab 7 |
| 8 | Core library, ≥ 12 unit + ≥ 2 integration tests, CI + Android build | Lab 8 (far above the minimum) |

What Lab 9 itself adds:

1. **Gap fixes** found in the table: list **search / filter** by name or barcode
   (lab §2.1 item 3, missing today): pure `ProductSearch` helper + `SearchText`
   on `ProductListViewModel`, combined with the existing sort.
2. **Design material** (task 1): `docs/steps/09-final.md` with the ER diagram
   (from `docs/domain`), screen / route map, **role × screen × action matrix**
   (extends the table in `domain-model.md` with Logs, Settings, Map,
   Diagnostics), layer diagram, DI lifetimes table (answers question 3).
3. **README** for the module: demo accounts, run instructions, test commands,
   CI badge, feature list.
4. **Release**: CI uploads a debug APK artifact; tag `v1.0`; demo script
   (5–7 min); offline scenario script (change offline → server changed →
   sync → last-write-wins) for question 4.
5. **Defence prep**: answers to the 8 control questions grounded in our classes;
   where each exception type is caught (question 7) with sample log lines; what
   is not covered by tests and why (question 8).
6. Full regression: all tests, Windows + Android builds, manual smoke checklist.

Owner actions: screenshots per role on Windows and Android, short video,
repository link, title page, GitHub Actions screenshot, Sentry and Google Maps keys.
