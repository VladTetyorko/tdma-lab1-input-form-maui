# AGENTS.md — rules for anyone (human or AI agent) working on StockRoom

Read this file, [`README.md`](README.md), [`docs/plan.md`](docs/plan.md) and
[`docs/domain/`](docs/domain) before changing anything. These rules are not optional.

## 1. Product scope — keep it simple

- One warehouse, three users (Storekeeper, Driver, Manager), about ten products.
- Features: product list with characteristics and quantity on hand; sort by any
  characteristic; add products and characteristics; **scan a barcode → choose
  In or Out → enter the number of units** → an `Operation` is recorded.
- A barcode identifies a **product** (a kind of unit), not a single item. One
  scan plus a quantity records many units.
- No dashboards, approvals, prices, several warehouses or real authentication
  unless the plan says so. Anything outside scope needs a written decision.
- Scanning accepts 1-D barcodes **and QR codes**. A QR code may carry a plain
  barcode or a StockRoom deep link (`stockroom://product/{barcode}`).
- Unknown barcodes can be looked up in the **Open Food Facts** open database
  (ODbL; keep the attribution) to prefill a new product. Seed data uses real
  barcodes from that database, not invented ones.
- Authentication is **local and minimal**: fixed accounts, a login and a
  password, no registration, no password recovery, no server accounts.
- UI is plain and functional for now. It will be redesigned in Figma later,
  so use shared styles (`Resources/Styles`) instead of inline values.

## 2. Engineering principles

| Principle | What it means here |
|-----------|--------------------|
| **SOLID** | One reason to change per class. ViewModels depend on **interfaces** (`IProductRepository`, `INavigationService`, `ICurrentUserService`, …), never on `Shell.Current`, `SQLiteAsyncConnection`, `Preferences` or other platform statics directly |
| **Dependency inversion** | Every service is registered in `MauiProgram` and injected through the constructor. Pages get their ViewModel through DI. No `new` of services inside ViewModels |
| **DRY** | Shared behaviour lives once: `Entity`/`OwnedEntity` base, a validation helper, one quantity calculator. If you copy code a second time, extract it |
| **KISS / YAGNI** | The simplest thing that satisfies the current lab. No speculative abstractions |
| **Decomposition** | Small classes and methods. One public type per file. Folders = namespaces = layers |
| **Low coupling, high cohesion** | Models know nothing about ViewModels; ViewModels know nothing about Views; Data knows nothing about UI |
| **3NF** | Data follows [`docs/domain/entities.md`](docs/domain/entities.md): UUID keys, `CreatedAt`/`UpdatedAt`, `CreatedByUserId`, no stored computed values (quantity on hand is computed from `Operations`) |
| **Immutability where the domain says so** | `Operation` is never edited or deleted |
| **Contracts first** | Every capability (logging sink, crash reporter, auth, biometrics, camera, location, sensors, lookup) is an interface in `Services/`; the MAUI/Serilog/Sentry/HTTP implementation sits behind it and is registered in `MauiProgram`. Do not re-implement what a library or `Microsoft.Maui.Essentials` already does: wrap it |
| **Fail soft** | A denied permission, missing hardware, no network or a failing service never crashes the app: the user gets a clear message and the event is logged at the right level |

### Layers

```
Models/        entities and enums — no dependencies
Services/      domain services and abstractions over the platform (navigation, current user, clock, preferences, scanner, secure storage, connectivity, sharing), sync, encryption
Data/          SQLite repositories, migrations (numbered SQL scripts), sync queue storage
ViewModels/    state + commands, depend on Services/Data interfaces only
Views/         XAML pages, no logic in code-behind except lifecycle forwarding
Converters/    value converters
```

From Lab 8 `Models/`, `Data/`, `ViewModels/` and the platform-free part of `Services/` live in the
platform-neutral **`StockRoom.Core`** project (`net10.0`, **no reference to `Microsoft.Maui.*`**; namespaces are
unchanged, `Mobile_apps_L1_Model_view.*`). The MAUI app keeps `Views/`, `Converters/`, `Platforms/`, the
`Maui*` / `Shell*` / `Camera*` / `Sentry*` implementations of Core interfaces (in its `Services/` folder) and
`MauiProgram`. A new platform-free class goes to Core; a class that needs a MAUI type goes to the app behind a Core interface.

## 2a. Cross-cutting rules (Labs 5–9)

### Logging (Lab 5)

- Inject `ILogger<T>`; never use the static `Log` class outside `MauiProgram`
  and the global exception handler.
- Message **templates**, not interpolation: `LogInformation("Saved product {Barcode}", barcode)`.
- Levels: `Debug` developer detail; `Information` a user action or lifecycle
  step that succeeded; `Warning` something expected but unwanted (validation
  rejected, permission denied, offline, role denied); `Error` an operation
  failed and was handled (exception passed as the first argument); `Critical`
  the app cannot continue (global handler only).
- Every ViewModel command that changes data and every repository/sync write
  logs its result once. No logging inside tight loops.
- **Never log** passwords, password hashes, tokens, encryption keys, the
  decrypted `Counterparty` or any other personal data. Log ids and barcodes.
- The log file format is fixed (the in-app log viewer parses it); changing it
  needs a test change in `Services/`.

### Security (Lab 6)

- **Passwords are never stored.** Only a salted hash: PBKDF2-HMAC-SHA256,
  per-user random 16-byte salt, at least 210 000 iterations, one
  self-describing string (`pbkdf2-sha256$iterations$salt$hash`). Compare in
  constant time. Passwords are not logged, not put in drafts, not in `Preferences`.
- The session token and every secret live only behind `ISecretStore`
  (`SecureStorage`). `Preferences` holds non-secret settings only.
- Authorization has **two layers, both mandatory**: UI binding hides what a
  role cannot do, **and** the command/service refuses it (user message +
  `Warning` log). The service layer is the one that counts.
- All permission rules live in `RolePermissions`; no `role == Role.X` in ViewModels.
- Certificate pinning applies to **our own server only**, never to third-party
  hosts such as Open Food Facts.

### Platform capabilities (Lab 7)

- Camera, gallery, location, sensors, maps, connectivity, permissions: each
  behind its own interface (`IPermissionService`, `IPhotoService`,
  `ILocationService`, `IShakeDetector`, `IMapLauncher`, …). No static MAUI API
  in a ViewModel.
- Always: check support → ask permission (explain why on rationale) → act →
  handle `FeatureNotSupportedException`, `FeatureNotEnabledException`,
  `PermissionException` and user cancel. Each outcome = one user message + one log line.
- Sensors and the camera start in `OnAppearing` and stop in `OnDisappearing`
  through `IPageLifecycleAware`; sensor events reach the UI through an
  `IMainThread` abstraction, not `MainThread` inside a ViewModel.
- Photos are files in the app data folder; the database stores the relative path only.
- Windows must stay usable: every hardware feature has a fallback or a clear "not available" state.

### Tests and CI (Lab 8)

- Core logic is tested without MAUI. Unit tests use fakes (existing style) or
  Moq where an interaction must be verified; assertions may use FluentAssertions 6.x (Apache-2.0).
- Repositories are tested against a real temporary SQLite file.
- ViewModel line coverage stays **≥ 60 %** (CI gate). New code ships with tests, as before.
- Interactive controls carry an `AutomationId`; UI (Appium) tests are marked
  `[Trait("Category", "UI")]` and are excluded from the default run and from CI.
- CI (`.github/workflows/ci.yml`): restore → test with coverage → publish
  coverage → build Android (Lab 9 also uploads the APK). A red CI blocks the merge.

## 2b. Iteration cycle (every lab, in this order)

1. **Read** the lab PDF (`docks/`) and list its tasks in the step document.
2. **Gap check**: compare each task with the code; write "exists / missing / adapt".
3. **Branch** from the previous lab (stack rule in §5). Never work on `master`.
4. **Contract first**: interface and test double, then ViewModel code against
   the interface, then the real implementation. Smallest vertical slice that
   works (one screen, one command) before the next.
5. **Test with the slice**: run `dotnet test Mobile_apps_L1_Model_view.Tests`
   after each slice, not only at the end.
6. **Commit** after each green slice: `[Lab N] <what>`. Refactoring in its own commit (§3).
7. **Build both targets** (Windows, Android) before the last commit.
8. **Document**: step document, README step log and decision log, answers to
   the lab's control questions, `docs/domain` if the model changed.
9. **Open the PR** (`labN` → previous lab). The PR text lists the lab tasks with
   ✔/✘ and what the owner must verify by hand.
10. **Stop and report.** Do not build the next lab on unreviewed work unless the
    owner said so; if you do, keep the branches stacked.

Things only the owner can do (screenshots, a Sentry DSN, an Android device,
GitHub repository settings) go under "Owner actions" in the step document and
are never faked.

## 3. Refactoring rule

When you touch code, improve it **if** the improvement makes it more correct
or clearly easier to read. Do not rewrite code you only pass by: a one-line
fix stays a one-line fix. Refactoring goes in its own commit when it is more
than a few lines.

## 4. Tests

- Every package (folder/namespace) has tests in `Mobile_apps_L1_Model_view.Tests/<Package>/`.
- Every behaviour change comes with tests. ViewModels are tested with fakes of
  their interfaces; no UI is needed.
- `Mobile_apps_L1_Model_view.Tests/EndToEnd/` holds flow tests that build the
  real DI container (real services and repositories, fake platform
  services) and drive whole user flows through ViewModels, e.g.
  *log in → scan → Out 3 units → list shows the new quantity*.
- `dotnet test Mobile_apps_L1_Model_view.Tests` must pass before every commit.
- The app must build for Windows (`-f net10.0-windows10.0.19041.0`) and Android (`-f net10.0-android`).

## 5. Workflow

- One lab = one branch = one pull request. Branches are stacked:
  `lab1` → `master`, `lab2` → `lab1`, `lab3` → `lab2`, `lab4` → `lab3` (all merged);
  then `plan-labs-5-9` → `master`, `catalog` → `plan-labs-5-9`, `lab5` → `catalog`,
  `lab6` → `lab5`, `lab7` → `lab6`, `lab8` → `lab7`, `lab9` → `lab8`
  (the course asks for `labN` names; `lab9` also gets the tag `v1.0`).
  `catalog` is the open-barcode-data step, not a lab.
- Small commits with meaningful messages: `[Lab N] <what>`.
- Each lab has a step document `docs/steps/0N-*.md` (template:
  `docs/steps/_template.md`): before / what the lab asks / our interpretation /
  decisions / changes / answers to the lab's control questions.
- Every architectural decision is also added to the **Decision log** in `README.md`
  (one line: decision, why).
- Update the step log in `README.md` when a lab is done.
- Never commit `bin/`, `obj/`, the lab PDFs (`docks/`), or personal report files.

## 6. Language

Code, comments, docs, commit messages and UI text are in **English**.
