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

### Layers

```
Models/        entities and enums — no dependencies
Services/      domain services and abstractions over the platform (navigation, current user, clock, preferences, scanner)
Data/          repositories (in-memory until Lab 4, SQLite from Lab 4), migrations
ViewModels/    state + commands, depend on Services/Data interfaces only
Views/         XAML pages, no logic in code-behind except lifecycle forwarding
Converters/    value converters
```

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
  *pick user → scan → Out 3 units → list shows the new quantity*.
- `dotnet test Mobile_apps_L1_Model_view.Tests` must pass before every commit.
- The app must build for Windows (`-f net10.0-windows10.0.19041.0`) and Android (`-f net10.0-android`).

## 5. Workflow

- One lab = one branch = one pull request. Branches are stacked:
  `lab1` → `master`, `lab2` → `lab1`, `lab3` → `lab2`, `lab4` → `lab3`
  (the course asks for branches named `lab3` / `lab4`).
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
