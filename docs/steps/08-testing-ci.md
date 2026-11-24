# Step 08 — Testing and CI: Core library, Moq tests, integration tests, GitHub Actions, UI tests

- **Lab:** 8 (`docks/` lab 8 PDF: a platform-free Core library, xUnit + Moq + FluentAssertions, integration tests on real SQLite, GitHub Actions with coverage and an Android build, `AutomationId` and Appium UI tests)
- **Branch:** `lab8` (on top of `lab7`)
- **PR:** _to be added_

## 1. Before

One MAUI project held everything (`Models/`, `Data/`, `Services/`, `ViewModels/`, `Views/`, ...). The test project targeted `net10.0-windows10.0.19041.0` and referenced the app project, so tests could run only on Windows with the MAUI workload installed. 1206 tests (hand-written fakes, real SQLite files, 29 end-to-end flows) passed; there was no CI, no coverage number, no UI test. Lab 3-7 already put every platform static behind an interface (`INavigationService`, `IAlertService`, `IPreferencesStore`, `ISecretStore`, `ILocationService`, `IPhotoService`, `IMainThread`, ...), so the split was mostly moving files.

## 2. What the lab asks for

1. A platform-free library (`Core`, `net10.0`, no `Microsoft.Maui.*`) with the models, ViewModels, service interfaces and repositories; the MAUI project keeps views, platform implementations and `MauiProgram`.
2. Static MAUI APIs (`Shell.Current`, `Preferences`, `SecureStorage`, ...) hidden behind interfaces (`INavigationService`, `IDialogService`, `ISettingsService`, `ISecureStoreService`).
3. A test project (xUnit, Moq, FluentAssertions): at least 8 ViewModel tests with `Setup` / `Verify`, one `[Theory]` for `CanExecute`, a denied-role test, a navigation-parameter test.
4. Repository integration tests on a real SQLite file: CRUD and a `SyncQueue` row per write.
5. GitHub Actions: tests with coverage, an HTML report, a failing gate when ViewModel coverage is below 60 %, an Android build.
6. `AutomationId` on the key controls and two Appium UI tests (login; create a record and see it in the list), kept out of CI.
7. Report: solution tree, test screenshot, a green pipeline screenshot with a coverage fragment, answers to six questions.

## 3. Our interpretation

| Lab task | In StockRoom | Status |
|----------|--------------|--------|
| `Core` library | `StockRoom.Core` (`net10.0`): `Models/`, `Data/` (+ the embedded `Migrations/*.sql`), `Services/` (interfaces and pure logic), `ViewModels/`, `ServiceCollectionExtensions.cs` (`AddStockRoomCore`). Namespaces are **kept** (`Mobile_apps_L1_Model_view.*`, `RootNamespace` the same) | done |
| Static APIs behind interfaces | already true after Labs 3-7; checked with grep (section 5): no ViewModel or Core service names `Shell`, `Preferences`, `SecureStorage`, `MediaPicker`, `Geolocation`, sensors, `MainThread` or `Application.Current`. One leftover was found: the ViewModels implemented the Shell's own `IQueryAttributable`; replaced by `INavigationParameterReceiver` (section 4). Mapping of the lab's names in the table below | done |
| Test project | `Mobile_apps_L1_Model_view.Tests` is plain `net10.0` and references **only** Core; `Mobile_apps_L1_Model_view.AppTests` (Windows) holds the tests of classes that need MAUI types | done |
| Moq + FluentAssertions | new files `ProductListViewModelMoqTests`, `ProductFormViewModelMoqTests`, `LogsViewModelMoqTests`: 16 tests (the `[Theory]` counts 7 cases); the 1206 older fake-based tests are untouched | done |
| Integration tests | `Data/RepositoryCrudIntegrationTests` (5 tests) on top of the Lab 4 `SqliteRepositoryTests` / `SchemaConstraintTests` / `MigrationRunnerTests` | done |
| CI | `.github/workflows/ci.yml`, section 7 | written, **not run** (needs GitHub) |
| `AutomationId` + UI tests | section 8; `Mobile_apps_L1_Model_view.UiTests` | written, builds, **not run** (needs a device and an Appium server) |

The lab's service names and ours:

| Lab | StockRoom |
|-----|-----------|
| `INavigationService` | `INavigationService` (`ShellNavigationService`) |
| `IDialogService` | `IAlertService` (`ShellAlertService`) plus `IPermissionGuard` for refusals |
| `ISettingsService` (over `Preferences`) | `IPreferencesStore` (`MauiPreferencesStore`), `IAppDataPathProvider` |
| `ISecureStoreService` (over `SecureStorage`) | `ISecretStore` (`MauiSecretStore`) |
| `IMediaPicker`, `Geolocation`, sensors (Lab 7) | `IPhotoService`, `ILocationService`, `IShakeDetector`, `IMapLauncher`, `IPlatformPermissions`, `IMainThread` |
| `ISessionService` | `ICurrentUserService`, `ISessionStore` |

### Solution structure

```
Mobile_apps_L1_Model_view.slnx
StockRoom.Core/                          net10.0, no MAUI. The whole logic of the app:
    Models/                              entities and enums (Product, Operation, User, ...)
    Services/                            service interfaces + pure services (auth, stock, sync, validation, permissions, logging tools)
    Data/                                SQLite repositories, sync journal, migrations (+ embedded numbered .sql scripts)
    ViewModels/                          state + commands, depend on interfaces only
    ServiceCollectionExtensions.cs       AddStockRoomCore(): the DI container shared by the app and the tests
Mobile_apps_L1_Model_view/               the MAUI app (net10.0-android / -windows / -ios / -maccatalyst)
    Views/, Converters/, Platforms/      XAML pages, value converters, platform entry points and manifests
    Services/                            Maui*, Shell*, Camera*, Sentry* implementations of Core interfaces
    MauiProgram.cs                       composition root: registers the platform implementations, Serilog, Sentry
Mobile_apps_L1_Model_view.Tests/         net10.0, references Core. Unit, integration and end-to-end flow tests; runs on any OS (CI)
Mobile_apps_L1_Model_view.AppTests/      net10.0-windows. Tests of the classes that need MAUI types (Maui* services, converters)
Mobile_apps_L1_Model_view.UiTests/       net10.0, Appium. Two UI scenarios, [Category=UI], never run by default or in CI
.github/workflows/ci.yml                 the pipeline
.github/scripts/check_coverage.py        the coverage gate
```

## 4. Decisions

Also in the README decision log (75-81).

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| Keep the namespaces `Mobile_apps_L1_Model_view.*` in Core; `RootNamespace` is the same | Rename to `StockRoom.*` | A rename touches 150+ files, every XAML `xmlns` and every test `using` for no behaviour. Only projects and folders move (`git mv`, history kept). The embedded migration resource names (`Migrations/0001_initial.sql`, by `LogicalName`) did not change either |
| What stays in the app: `Maui*`, `Shell*`, `CameraBarcodeScanner`, `SentryCrashReporter`, `Views/`, `Converters/`, `Platforms/`, `MauiProgram` | Move `SentryCrashReporter` (it only needs the Sentry SDK, not MAUI) | Core has no reason to know a crash vendor; `ICrashReporter` and `SentryDsn` (pure JSON) are in Core, the SDK stays out. The `Maui*` classes use Essentials types; the Shell ones use `Shell.Current` |
| Packages move with the code: `CommunityToolkit.Mvvm`, `sqlite-net-pcl`, `SQLitePCLRaw.bundle_green`, `Microsoft.Extensions.DependencyInjection` / `Logging` go to Core. Serilog, Sentry, ZXing, the biometric plugin, Maps and the native sqlite packages (`SQLitePCLRaw.lib.e_sqlite3` 2.1.14, `.android`) stay in the app; the test project references `lib.e_sqlite3` 2.1.14 itself | Put the native library in Core | Core has one target and cannot choose between the desktop and the Android native library; the host that runs the code picks it (the app per platform, the tests the desktop one). The 2.1.14 pin keeps the fix for GHSA-2m69-gcr7-jv3q |
| `INavigationParameterReceiver` (Core) replaces `Microsoft.Maui.IQueryAttributable` on the four ViewModels that receive parameters; each page implements the Shell interface and forwards (`public void ApplyQueryAttributes(...) => _viewModel.ApplyQueryAttributes(...)`) | Keep MAUI in Core for one interface; a `Microsoft.Maui` reference-only package | The interface is the only Shell type the ViewModels used. The method keeps its name and signature, so no test changed. The Shell calls `ApplyQueryAttributes` on the page as well as on its `BindingContext`, so behaviour is the same. One four-line forwarder per page is the same trade-off `PageLifecycleBehavior` already makes |
| The tests of MAUI-bound classes go to `Mobile_apps_L1_Model_view.AppTests` (42 tests: `Maui*ServiceTests`, two converter tests, one `SentryCrashReporter` test, four fakes of Essentials interfaces); nothing was dropped | Delete them as "framework tests" | They assert real behaviour (error mapping of `MauiPhotoService`, `MauiLocationService`, `MauiMapLauncher`, `MauiShakeDetector`, colours of `QuantityToColorConverter`). The project is Windows-only and not in the CI test job. It reuses the fakes of the Core test project by project reference |
| Moq and FluentAssertions only for the **new** tests; the existing 1206 tests are not rewritten | Convert fakes to mocks | AGENTS 3: do not rewrite code that works. Fakes stay the default (they are also used by the end-to-end container); mocks are used where the lab wants an interaction verified (`Times.Once`, `Times.Never`) |
| FluentAssertions stays on 6.12.x | 7+ / 8 (commercial licence for some uses) | AGENTS 2a: Apache-2.0 version only |
| The UI tests reference Core (for the `Ean13` helper) but nothing references them | Copy the check-digit code | One rule in one place (DRY); the default test run builds only the Core test project |
| `Category!=UI` filter and a separate project instead of skipping tests at run time | `[Fact(Skip = ...)]` in the main project | The UI project needs Appium packages and a server; keeping it out of the default `dotnet test <project>` means a plain checkout never fails because no device exists |

## 5. Leftover abstractions (lab 1.3)

Check: `grep -rnE "Shell\.Current|DisplayAlert|Preferences\.|SecureStorage|MediaPicker\.|Geolocation\.|MainThread\.|Application\.Current|Microsoft\.Maui" StockRoom.Core --include=*.cs` finds only comments. The compiler is the stronger check: Core has no MAUI reference, so any use would not build. The only MAUI type the ViewModels still used was `IQueryAttributable` (section 4). In the app project the only static outside `Services/Maui*` / `Services/Shell*` is `MainThread.BeginInvokeOnMainThread` inside `CameraScannerView` (a view, not a ViewModel).

## 6. The test pyramid of this repository

| Level | Where | Tests | Runs in CI |
|-------|-------|-------|------------|
| Unit | `Tests/Models`, `Tests/Services`, `Tests/ViewModels` (fakes, plus the 16 Moq tests) | 1004 | yes |
| Integration | `Tests/Data`: real SQLite file in the temp folder, migrations, constraints, repositories, `SyncQueue` rows (includes in-memory repository fakes tests and the 5 new CRUD tests) | 152 | yes |
| End-to-end flow (no UI) | `Tests/EndToEnd`: real DI container, fake platform, whole flows through ViewModels | 29 | yes |
| MAUI-bound unit | `AppTests` | 42 | no (Windows only) |
| UI (Appium) | `UiTests` | 2 | no (needs a device) |

Before the lab: 1206 tests (the same ones). After: **1185 + 42 = 1227 runnable without a device** (1206 existing + 16 Moq + 5 CRUD, minus nothing), plus 2 UI tests. `dotnet test Mobile_apps_L1_Model_view.Tests` also passes on Linux (checked in `mcr.microsoft.com/dotnet/sdk:10.0` in Docker, see section 9).

### The Moq tests (lab 1.4)

| Test | Lab requirement |
|------|-----------------|
| `ProductListViewModelMoqTests.Load_FillsTheListFromTheCatalog` | list fills from the repository (catalog) mock, `Verify(..., Times.Once)` |
| `...OpenDetails_NavigatesToTheDetailRouteWithTheSelectedProduct` | navigation with the right parameter, `Verify` on `INavigationService` |
| `...Delete_ByDriver_IsDeniedWithAnAlertAndTheServiceIsNeverCalled` | role denied, `Times.Never` on the service, alert shown once |
| `...Delete_ByManager_AfterConfirmation_CallsTheServiceOnceAndRemovesTheRow` | service called once |
| `...Delete_WhenTheManagerDeclinesTheQuestion_TheServiceIsNeverCalled` | `Times.Never` |
| `ProductFormViewModelMoqTests.SaveCommand_CanExecute_DependsOnTheNameAndTheBarcode` | `[Theory]` with 7 `[InlineData]` rows (empty, blank, bad length, bad check digit, letters) |
| `...Save_WithValidInput_CallsTheServiceOnceAndNavigatesBack` | repository/service called on save |
| `...Save_WhenTheServiceRefuses_ShowsTheReasonAndStaysOnThePage` | `Times.Never` on navigation |
| `LogsViewModelMoqTests.Refresh_WhenTheReaderThrows_LogsAnErrorWithTheExceptionAndShowsAMessage` | logged error when the dependency throws (`Mock<ILogger<T>>.Verify`) |
| `...Refresh_WhenTheReaderWorks_LogsNothingAtErrorLevel` | the negative case |

The logged-error test uses `ILogReader` as the failing dependency: the repositories' own failure log (`Write to {Table} failed and was rolled back`) is tested in `RepositoryLoggingTests`, and the ViewModels do not catch repository exceptions (they are the global handler's job, Lab 5).

### The integration tests (lab 1.5)

`RepositoryCrudIntegrationTests`: `Product_FullCrud_Save_GetById_Update_Delete`; `Product_EveryWrite_AddsTheExpectedSyncQueueRow` (Insert, Update, Delete in that order, with payloads); `ProductCharacteristicValue_FullCrud_AndEveryWriteQueuesARow`; `Operation_IsSavedAndReadBack_AndQueuesOneInsert_ItHasNoUpdateOrDelete` (immutability checked through the interface); `FailedWrite_LeavesNoSyncQueueRow` (the unique-barcode violation rolls the queue row back). They use the real container with a temporary SQLite file (`AddFakePlatform` gives each test its own file).

## 7. CI

`.github/workflows/ci.yml`, triggers: `push` and `pull_request`.

- **`test`** (ubuntu-latest): checkout, setup .NET 10.0.x, `dotnet test Mobile_apps_L1_Model_view.Tests --filter "Category!=UI" --collect:"XPlat Code Coverage" --results-directory ./TestResults`, install `dotnet-reportgenerator-globaltool`, two reports (all of Core; `-classfilters:+Mobile_apps_L1_Model_view.ViewModels.*` for the ViewModels), a summary in the job page, then the **gate**: `python3 .github/scripts/check_coverage.py <cobertura> Mobile_apps_L1_Model_view.ViewModels. 60` fails the job under 60 % line coverage. The script counts distinct (file, line) pairs of every class whose name starts with the prefix, so async state machines and closures of one ViewModel are not counted twice. The HTML reports and the Cobertura file are uploaded as the artifact `coverage-report`.
- **`build-android`** (windows-latest, `needs: test`): checkout, setup .NET, setup Java 17 (Microsoft OpenJDK), `dotnet workload install maui-android`, `dotnet build Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-android -c Release`. The Android SDK is preinstalled on the runner image.
- A Windows build job was **not** added: it needs the Windows App SDK workload and runs 3-4 minutes more for little extra signal; the Android job already compiles all XAML and `MauiProgram`. Lab 9 can add it.

Numbers measured locally on the committed code (same commands, Windows host, ReportGenerator 5.6.1): 1185 tests passed; **ViewModels line coverage 97.4 %** (1129 of 1159 lines; branch 94.5 %); all of Core line coverage 96.2 % (3708 of 3854). The gate script printed `Line coverage of 'Mobile_apps_L1_Model_view.ViewModels.*': 97.4% (1129/1159 lines), minimum 60%` and exit code 0; with the minimum set to 99 it exits 1. No extra tests were needed to pass the gate.

What could **not** be run: GitHub Actions itself (no `gh`, no push). The YAML was checked for syntax by reading it and by running every `run:` step locally except `actions/*`, `dotnet workload install` and the Windows runner. The Linux test run was done in Docker with the same SDK image family.

## 8. AutomationId and UI tests

| Page | AutomationId |
|------|--------------|
| Login | `LoginEntry`, `PasswordEntry`, `LoginButton`, `ErrorLabel` (existed since Lab 6), `BiometricButton` |
| Product list | `ProductsCollection` (the `CollectionView`), `ProductNameLabel` (row), `EditProductButton`, `DeleteProductButton`, `SortPicker`, `AddProductButton`, `ScanButton`, `MapButton`, `SettingsButton`, `SignOutButton`, `StatusLabel` |
| Product form | `ProductNameEntry`, `BarcodeEntry`, `NameErrorLabel`, `BarcodeErrorLabel`, `StatusLabel`, `SaveProductButton`, `CancelProductButton` |
| Operation form | `DirectionPicker`, `QuantityEntry`, `CounterpartyEntry`, `ReasonEntry`, `ErrorLabel`, `SaveOperationButton`, `CancelOperationButton` |
| Scanner | `ManualBarcodeEntry`, `FindProductButton`, `AddThisProductButton`, `ScannerStatusLabel` |
| Settings | the Lab 5-7 ids (`OpenLogsButton`, `ShakeToScanSwitch`, ...). **Sign out is on the product list in this app (`SignOutButton`), not on Settings** |

`Mobile_apps_L1_Model_view.UiTests` (`net10.0`, `Appium.WebDriver` 5.0.0, xunit; `[Trait("Category", "UI")]` on the class):

1. `Login_WithTheDemoManager_ShowsTheProductList`: signs in as `olena` / `Manager#2026`, expects `ProductsCollection` and `AddProductButton`.
2. `CreatingAProduct_MakesItAppearInTheList`: signs in, taps `AddProductButton`, types a name `AAA UI test HHmmss` (sorts first, so no scrolling) and a valid EAN-13 built with `Ean13.WithCheckDigit`, taps `SaveProductButton`, expects the list and the new name as visible text.

`AppSession` picks the driver from environment variables, `AppScreen` finds controls by accessibility id (UIA `AutomationId` on Windows, content description on Android). If a saved session skips the login page, the test signs out first.

### How to run the UI tests (not run by the agent)

Common: `npm install -g appium`, then start the server in its own terminal: `appium` (listens on `http://127.0.0.1:4723`).

Windows:

```bash
appium driver install --source=npm appium-windows-driver     # once; needs Windows Developer Mode on
dotnet build Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-windows10.0.19041.0
export STOCKROOM_UI_PLATFORM=windows
export STOCKROOM_UI_APP="$PWD/Mobile_apps_L1_Model_view/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Mobile_apps_L1_Model_view.exe"
dotnet test Mobile_apps_L1_Model_view.UiTests --filter "Category=UI"
```

The Windows run uses the **real database** of the unpackaged app (`%LOCALAPPDATA%/.../Data/stockroom.db3`): the second test adds one product `AAA UI test ...`. Use a copy of the database or delete the file afterwards.

Android (emulator running, or a device with USB debugging):

```bash
appium driver install uiautomator2                            # once
dotnet build Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-android -c Release
export STOCKROOM_UI_PLATFORM=android
export STOCKROOM_UI_APP="<path to the built .apk>"           # e.g. .../bin/Release/net10.0-android/com.companyname.mobile_apps_l1_model_view-Signed.apk
export STOCKROOM_UI_DEVICE=emulator-5554
dotnet test Mobile_apps_L1_Model_view.UiTests --filter "Category=UI"
```

Honest status: the project builds; the two tests have **never been executed** (no emulator or Windows driver was available while writing them). Expect to adjust waits or the apk path on the first run. A locator risk: `Displayed` of a `CollectionView` on Android can be false while it animates; `Find` retries for 20 s.

## 9. How to run everything

```bash
dotnet test Mobile_apps_L1_Model_view.Tests                                  # 1185 tests, any OS
dotnet test Mobile_apps_L1_Model_view.AppTests                               # 42 tests, Windows
dotnet test Mobile_apps_L1_Model_view.Tests --filter "Category!=UI" --collect:"XPlat Code Coverage" --results-directory ./TestResults
python .github/scripts/check_coverage.py TestResults/<guid>/coverage.cobertura.xml Mobile_apps_L1_Model_view.ViewModels. 60
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:CoverageReport -reporttypes:Html
dotnet build Mobile_apps_L1_Model_view.slnx                                  # all projects (needs the MAUI workloads)
```

Linux check without a Linux machine: copy `StockRoom.Core`, `Mobile_apps_L1_Model_view.Tests` into a folder and run `docker run --rm -v <folder>:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test Mobile_apps_L1_Model_view.Tests --filter "Category!=UI"`.

## 10. Owner actions

1. Push `lab8`, open the PR (`lab8` -> `lab7`), enable **Actions** for the repository (Settings, Actions, Allow).
2. Take the screenshot of a green run (both jobs) and of the coverage summary; open the artifact `coverage-report`, `CoverageReport-ViewModels/index.html`, and screenshot the ViewModels table (this is the "HTML fragment" of the report). The badge in the README turns green after the first run.
3. Screenshot of `dotnet test Mobile_apps_L1_Model_view.Tests` (1185 passed) and of `...AppTests` (42).
4. Run the UI tests: Appium on Windows and on an Android emulator (section 8); fix locators if the first run shows a mismatch; record the result.
5. If the Android job fails on the runner for an environment reason (SDK platform, workload version), the log names it; the usual fix is a pinned `dotnet-version` or an extra `dotnet workload restore`.
6. Add the PR link to this file.

## 11. Answers to the control questions

1. **What is the test pyramid and why are there most unit tests?** Three levels: many unit tests of single classes (ViewModels, services, validators), fewer integration tests of several parts (a repository on a real SQLite file), very few UI tests that drive the whole app on a device. The lower the level the faster, cheaper and less fragile the test, and the more precisely a failure points at the cause. Here: 1004 unit, 152 data/integration, 29 flow, 42 MAUI-bound, 2 UI. MVVM makes the wide base possible: all behaviour is in ViewModels and services, so it runs without any UI.
2. **Why can an xUnit project not reference the MAUI project, and how does Core solve it?** The MAUI app targets `net10.0-android/-windows10.0.19041.0/-ios/...`, a platform TFM; a plain `net10.0` test project cannot reference an assembly built for a platform TFM, and a test project that targets Windows would need the MAUI workloads and could run only there. `StockRoom.Core` targets plain `net10.0`, has no `Microsoft.Maui.*` reference (the compiler enforces it), and both the app and the test project reference it. The tests then run on any OS, including the CI Linux runner. Platform behaviour is reached only through Core interfaces that the app implements and the tests fake.
3. **Mock versus stub; `Setup` and `Verify`.** A stub returns canned answers so the code under test can run (our hand-written fakes are stubs and fakes with some memory). A mock is also a spy: the test asserts on how it was used. In Moq both are `Mock<T>`; `Setup` defines the behaviour (`_catalog.Setup(c => c.GetSummariesAsync()).ReturnsAsync(...)`), `Verify` asserts an interaction (`_productService.Verify(s => s.DeleteAsync(It.IsAny<Guid>()), Times.Never)`). Use a stub when the result matters, a mock when the call itself is the behaviour (a refused role must not reach the service).
4. **What do integration tests find that mock-repository tests cannot?** Everything between the code and the database: wrong SQL or column mapping, a missing migration, `CHECK`, `UNIQUE` and foreign-key rules, rounding of dates and `double` values, enum storage, transaction rollback (our `FailedWrite_LeavesNoSyncQueueRow`), that a write leaves exactly one `SyncQueue` row, and encrypted fields round trips. A mock repository does what the test author assumed it does, so these faults pass unit tests unseen.
5. **Why `AutomationId`, and why is finding by text fragile?** `AutomationId` is a stable identifier that does not change with the visible text, language, theme or layout, and maps to the accessibility id of each platform (UIA `AutomationId` on Windows, content description on Android), so one locator works on both. Text changes with translation, wording or state ("Save" becomes "Saving..."), is not unique (several "Delete" buttons in a list) and is sometimes not exposed at all, so tests that find by text break for reasons unrelated to behaviour. (The one place we use text is the list row, where the product name is the data being tested.)
6. **What does code coverage show, and why does 100 % not mean no bugs?** It shows which lines (and branches) were executed during the tests, so it finds code no test touches. It does not show whether the assertions are meaningful: a test that calls a method and asserts nothing gives full coverage. It cannot see missing code (an unhandled case), wrong requirements, concurrency, platform differences, or the integration with real devices. Our 97 % ViewModel coverage says tests reach nearly all lines; the quality comes from the assertions (for example `Times.Never` on the service for a denied role) and from the integration and UI levels.
