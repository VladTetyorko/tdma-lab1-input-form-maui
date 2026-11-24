# Step 05 — Logging, global error handling, log viewer, crash reporting

- **Lab:** 5 (`docks/` lab 5 PDF: Serilog, global exception handlers, crash reporting, analytics)
- **Branch:** `lab5` (on top of `catalog`)
- **PR:** _to be added_

## 1. Before

The app already used `ILogger<T>` in four places (`OpenFoodFactsLookup`,
`SyncService`, `SqliteOperationRepository`, `OperationDraftStore`) and
registered `AddLogging()`, but nothing wrote the messages anywhere except the
debug output in a DEBUG build. There was no file, no rule for levels or for what
must never be logged, no handler for exceptions nobody catches, no crash
reporting and no way to look at the log on the device.

## 2. What the lab asks for

1. Serilog with a console sink and a rolling file sink.
2. Log key events with the right levels.
3. Global handlers for `AppDomain.UnhandledException` and
   `TaskScheduler.UnobservedTaskException`; simulate failures and check the log.
4. A crash-reporting service (and analytics); do not send personal data.
5. An in-app log viewer with a level filter.
6. Answer the control questions 7-12 (section 7).

## 3. Our interpretation

| Lab task | In StockRoom | Status |
|----------|--------------|--------|
| Serilog | Configured **only** in `MauiProgram.ConfigureLogging`: `Serilog.Extensions.Logging` + file sink (`logs/stockroom-.log` in the app data folder, daily, 7 files kept) + console sink in DEBUG. One output template, `LogFormat.OutputTemplate`, shared with the parser. Minimum level Debug in DEBUG, Information in Release. Folder behind `ILogPathProvider` | done |
| Key events | `ILogger<T>` injected into the seven ViewModels that change data or can fail, `StockService`, `ProductService`, `CharacteristicService`, `SyncService`, `DatabaseExporter` and the `SqliteRepository` base. Role denied and rejected input are `Warning`, success `Information`, a handled failure `Error` with the exception, repository writes `Debug`. Rules in `AGENTS.md` section 2a, enforced by a test that records every log during an operation with a known counterparty | done |
| Global handlers | `UnhandledExceptionLogger` (Services): `AppDomain.UnhandledException` -> `Critical`, `TaskScheduler.UnobservedTaskException` -> `Error` + `SetObserved`, `Attach()` is idempotent and called once after `Build()`. Android `AndroidEnvironment.UnhandledExceptionRaiser` and WinUI `Application.UnhandledException` forward to the same class (`OnPlatformUnhandled`) | done |
| Simulate | Settings, "Diagnostics" section, **Manager only**: Open logs, Write test log entries (one per level), Throw in background task (faulted `Task` nobody observes, then `GC.Collect`), Send test crash (enabled only with a DSN). Behaviour in `IDiagnosticsService` | done |
| Crash reporting | `ICrashReporter` (`Report`, `IsEnabled`); `SentryCrashReporter` (plain Sentry SDK) when a DSN exists, otherwise `NullCrashReporter`; the choice is made in `MauiProgram` only. `SendDefaultPii = false`, log lines are not sent | done in code; **DSN and dashboard are owner actions** |
| Log viewer | `ILogReader` / `LogReader` (newest files first, `FileShare.ReadWrite`), `LogLineParser`, `LogEntry`, `LogsViewModel`, `LogsPage` (route `logs`): level picker (Debug..Critical), newest first, at most 200 entries, exception text, empty state. Manager only: the Settings entry is hidden **and** the ViewModel refuses with a message and a `Warning` | done |
| Analytics | Not added: the plan keeps scope to crash reporting. Decision 52 | skipped on purpose |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| `Serilog.Extensions.Logging` bridges Serilog into the `ILogger<T>` already injected everywhere; the `Log` static class is not used | `Serilog.Extensions.Hosting`, calling `Log.Information` directly | One logging abstraction in the code (AGENTS 2a); no generic host in MAUI; swapping the logger touches `MauiProgram` only |
| The template and file name pattern live in `LogFormat` (Services), used by `MauiProgram`, `LogReader` and the tests | Repeat the string in the parser | A change to the format breaks `LoggingFlowTests` (real Serilog to a temp folder, read back by `LogReader`) instead of silently breaking the viewer |
| The parser works on the text format, not on a JSON/CLEF file | Write compact JSON for the app and text for humans | The lab asks for a readable file; one text format is the single source for both people and the viewer; the parser is 60 lines and fully tested |
| Exception lines (anything that is not a header) attach to the previous entry as its exception text | Parse stack frames | KISS; the viewer shows the text as it is |
| Repository writes log at `Debug` (success) / `Error` (failure) in the base `InTransactionAsync`; services log the business outcome at `Information` / `Warning` | Log in every repository method | One place, once per write, never in a loop (AGENTS 2a) |
| `SyncService` logs one line per run (pushed N / offline / failed), per-item pushes at `Debug` | Keep one `Information` line per pushed item | 200 queued changes would flood the file and the viewer |
| `UnhandledExceptionLogger` takes `ICrashReporter?` and swallows reporter failures | Make the reporter mandatory | The logger works without a crash service; a failing reporter must not hide the original exception |
| Platform hooks (Android, WinUI) only forward to `UnhandledExceptionLogger.OnPlatformUnhandled` | Logic in `MainApplication` / `App` | Keeps platform code three lines long; everything testable sits in one class |
| The plain `Sentry` 7.0.1 package, initialised with `SentrySdk.Init` in `MauiProgram`; no Sentry log integration | `Sentry.Maui` (tried first) | `Sentry.Maui` 7.0.1 builds for Windows and Android, but its Windows build needs `WinRT.Runtime` 2.3.1 while the app ships 2.2.0, and the Windows app crashed at start (`FileNotFoundException` in `ConfigureCrashReporting`, seen when the exe was run). The plain SDK has no such dependency, catches unhandled .NET exceptions and sends `CaptureException`; only exceptions leave the device, never log lines |
| DSN from env var `STOCKROOM_SENTRY_DSN`, else from the untracked `appsettings.local.json` packaged as a `MauiAsset` when the file exists | Commit the DSN; build-time constant | A DSN is project configuration, not a credential for the repo; absent = null reporter, so a fresh clone builds and runs |
| `SentryDsn.Resolve` is a pure function in Services | Read the file inside `MauiProgram` | Testable; a malformed file means "not configured", never a crash at startup |
| `RolePermissions.CanViewLogs` / `CanUseDiagnostics` (Manager) enforced in `LogsViewModel` and `DiagnosticsService`, hidden in the UI | UI hiding only | AGENTS 2a: UI hides, service refuses with a message and a `Warning` |
| "Throw in background task" creates the faulted task in a non-inlined method, waits 50 ms, then `GC.Collect` + `WaitForPendingFinalizers` | `Task.Run` and wait for the next natural GC | The unobserved handler runs from the task finalizer; forcing a collection makes the demo immediate and repeatable |
| `UnhandledExceptionLogger` is registered in `AddStockRoomCore`, the crash reporter in `MauiProgram` and the fake platform | Register a default `NullCrashReporter` in core | The registration decision belongs to the host (AGENTS 2); tests use `FakeCrashReporter` |

## 5. Changes in the PR

- Packages: `Serilog.Extensions.Logging` 11.0.0, `Serilog.Sinks.File` 7.0.0, `Serilog.Sinks.Console` 6.1.1, `Sentry` 7.0.1.
- `Services/`: `ILogPathProvider`, `MauiLogPathProvider`, `LogFormat`, `LogEntry`, `LogLineParser`,
  `ILogReader`, `LogReader`, `UnhandledExceptionLogger`, `ICrashReporter`, `NullCrashReporter`,
  `SentryCrashReporter`, `SentryDsn`, `IDiagnosticsService`, `DiagnosticsService`;
  `RolePermissions.CanViewLogs` / `CanUseDiagnostics`; `Routes.Logs`;
  logging added to `StockService`, `ProductService`, `CharacteristicService`, `SyncService`, `DatabaseExporter`.
- `Data/`: `SqliteRepository` takes an `ILogger` (write ok / failed); every repository passes its own.
- `ViewModels/`: `LogsViewModel`; logging in the other ViewModels; `SettingsViewModel` gets the Diagnostics commands.
- `Views/`: `LogsPage`; Diagnostics section in `SettingsPage`.
- `MauiProgram` (Serilog, Sentry, `Attach`), `Platforms/Android/MainApplication.cs`, `Platforms/Windows/App.xaml.cs`, `.gitignore` (`appsettings.local.json`).
- Tests (`Mobile_apps_L1_Model_view.Tests`): `LogLineParserTests`, `LogReaderTests` (temp files, multi-line exception,
  level filter, newest first, limit, a file held open by a writer), `UnhandledExceptionLoggerTests` (both handlers, a real
  unobserved task, idempotent `Attach`), `LogsViewModelTests`, `DiagnosticsServiceTests`, `SentryDsnTests`,
  `ServiceLoggingTests` (levels, role denied, **no counterparty in any log**), `RepositoryLoggingTests`,
  `SettingsViewModelTests` (diagnostics), `RolePermissionsTests`, `EndToEnd/LoggingFlowTests` (real Serilog -> `LogReader`).
  Fakes: `CapturingLogger<T>`, `LogRecord`, `FakeCrashReporter`, `FakeLogReader`, `TempLogPath`.
- A race in `SyncServiceTests.SimulateOfflineSwitchedOff_SendsTheQueueWithoutAskingAgain` was fixed (own commit): the test
  now waits for the startup sync before queueing a change.

## 6. Open questions / next steps

- Analytics (screen views, feature use) is not implemented; if wanted it would be a second small interface next to `ICrashReporter`, off by default.
- Log files are not uploaded or shared yet; the viewer is the way to read them on the device.
- Native crashes (Android NDK, iOS) are not captured because the plain `Sentry` SDK is used instead of `Sentry.Maui`; managed
  exceptions, which are what this app throws, are. Revisit when `Sentry.Maui` and the Windows App SDK agree on `WinRT.Runtime`.
- The Windows exe was started on the developer machine: it reached the product list and wrote
  `Data/logs/stockroom-<date>.log` in the documented format (excerpt in the owner actions below is still to be taken by the owner).

## 7. Answers to the control questions

**7. Which log levels exist in .NET and when to use each?**
`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` (and `None` to switch logging off).
In StockRoom: `Debug` is developer detail such as "Write to Products committed" in `SqliteRepository` or per-item pushes in
`SyncService`; `Information` is a user action or lifecycle step that worked ("Recorded In of 4 units of product ... " in
`StockService`, "Product list loaded"); `Warning` is expected but unwanted (a Driver tries to record Out, a duplicate barcode is
rejected, the sync runs offline); `Error` is an operation that failed and was handled (a write rolled back, the log folder cannot be
read) with the exception as first argument; `Critical` is only for the global handler when the app cannot continue.
`Trace` is not used. Release builds log from `Information` up, DEBUG builds from `Debug`.

**8. How does Serilog differ from the standard `ILogger`, and why sinks?**
`ILogger<T>` (Microsoft.Extensions.Logging) is only an abstraction: it has levels, categories and message templates, but it does not
write anything itself; a provider does. Serilog is a logging library that is such a provider (through `Serilog.Extensions.Logging`)
and adds structured events (the template's `{ProductId}` stays a named property), enrichers, output templates and, above all, sinks.
A sink is the destination of an event: the file (`stockroom-.log`, rolling daily, 7 kept) and the console in StockRoom. New
destinations are added in `MauiProgram` without touching any class that logs, because classes only know `ILogger<T>`.

**9. Why can unhandled exceptions in background `Task`s go unnoticed without `TaskScheduler.UnobservedTaskException`?**
A `Task` stores its exception instead of throwing it on the thread that ran it; the exception surfaces only when somebody awaits
or reads `Result`/`Exception`. In a fire-and-forget task nobody does, so nothing is shown and, since .NET 4.5, the process does not
crash either: when the garbage collector finalizes the faulted task, the runtime raises `UnobservedTaskException` and then forgets
the error. Without a handler the failure vanishes (a lost sync, a lost save). `UnhandledExceptionLogger.OnUnobservedTaskException`
writes it as `Error` and calls `SetObserved()`. The "Throw in background task" button in Settings proves it: it throws in a task nobody
awaits, forces a GC, and the Error entry appears in the log viewer.

**10. Manual file logging versus an automatic crash-collection service?**
A log file is local and complete: every level, every action, readable offline (our viewer), but it stays on the device, so you see
it only when the user sends it, and a crash that kills the process may not be flushed. A crash service (Sentry) captures the
exception at the moment of the crash, uploads it when the network allows, groups identical crashes, counts affected users, shows
release and OS version and sends alerts. It has no ordinary events and needs a network and consent. They complement each other:
the file explains what happened before; the service tells that it happened and how often.

**11. What besides the stack trace is useful in an error report?**
App version/build and release channel, OS and version, device model and memory, the user's role (not name), a short trail of
breadcrumbs (last screens and actions), the context tag (here `AppDomain.UnhandledException` or `Diagnostics.SendTestCrash`),
whether the app was online/offline and the size of the sync queue, the entity id or barcode being handled, and the time.
Never the counterparty, passwords or tokens.

**12. Privacy risks of analytics and crash reports?**
Reports can carry personal data by accident: user names, e-mail, IP address and device ids that Sentry attaches by default
(`SendDefaultPii`), the text of log messages, URLs, screenshots, and the contents of local storage. Such data leaves the device for a
third party, may cross borders (GDPR), must be covered by consent and a retention period, and a leaked report exposes people.
StockRoom limits this: `SendDefaultPii = false`, no screenshots, log lines are not forwarded to Sentry, the counterparty is
encrypted in the database and never logged (a test checks that no log message contains it), only ids and barcodes are logged, the
DSN is not committed, and without a DSN nothing is sent at all.

## Owner actions

These cannot be done by the agent and are **not** claimed as verified:

- **Sentry project and DSN.** Create a free project on sentry.io (platform: .NET MAUI), copy the DSN and put it in
  `Mobile_apps_L1_Model_view/appsettings.local.json`: `{ "Sentry": { "Dsn": "https://...@....ingest.sentry.io/..." } }`
  (the file is git-ignored; or set the env var `STOCKROOM_SENTRY_DSN`). Rebuild.
- **Screenshot 1, Sentry.** Sign in as Manager, Settings, Diagnostics, **Send test crash**; screenshot the Sentry dashboard
  showing the `StockRoom test crash` event (and that no user, e-mail or IP is attached).
- **Screenshot 2, log viewer.** Settings, Diagnostics, **Write test log entries**, **Throw in background task**, then **Open logs**;
  screenshot the list with the level picker. Switch the level to Error to show the filter.
- **Log file excerpt.** Open `%LOCALAPPDATA%/User Name/com.companyname.mobile_apps_l1_model_view/Data/logs/stockroom-<date>.log`
  (Android: the app data folder, `files/logs`), copy about ten lines that show several levels, and include the multi-line
  `Error` entry with its exception text. Paste it into the report.
- **Check on a device.** On Android, throw in the background task and confirm that the entry appears; optionally force-stop the app
  after **Send test crash** and confirm that the event arrives.
