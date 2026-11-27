# Implementation slices — StockRoom redesign

> Navigation (slices 5, 6), the Receive / Issue page (14) and the Products actions are superseded by
> [`revision-2.md`](revision-2.md); this list is kept as the record of step 10.

Each slice is **one commit** `[Design] <what>` on a branch stacked on `design` (proposal: `redesign`), with tests,
green `dotnet test Mobile_apps_L1_Model_view.Tests`, and (for app slices) a Windows build
(`-f net10.0-windows10.0.19041.0`) — Android build at least every third slice and before the PR.
Contracts first (interface + fake, then ViewModel, then implementation), AGENTS.md rules apply throughout.
Specs: [`design-system.md`](design-system.md), [`ux-plan.md`](ux-plan.md). Open questions (Q1–Q10) are in ux-plan §10;
a slice that depends on one says so.

Common rules for the implementing agent:
- Never `git add -A`; never commit `Resources/Raw/`, `docks/`, `REPORT_Part2.md`, the root `.docx`.
- Do not run the Windows exe against the real local database (use tests; for a visual check rename/backup is the owner's job).
- Every new interactive control has an `AutomationId`; every colour/size comes from `Resources/Styles` tokens.
- Use the Write tool for files containing apostrophes (Bash heredocs break).
- `MauiProgram.cs` and `StockRoom.Core/ServiceCollectionExtensions.cs` are touched by many slices: one-line additions;
  rebase rather than run those slices in parallel branches (see the parallel table at the end).

---

## Phase A — foundation

### Slice 1 — Design tokens and theme switching
- **Files**: `Resources/Styles/Colors.xaml` (palette primitives only), new `Resources/Styles/Themes/LightGreen.xaml`,
  `Themes/Dark.xaml` (semantic keys of design-system §3), `Resources/Styles/Styles.xaml` (all colours via
  `DynamicResource` semantic keys, template `Shadow` style removed), `App.xaml` (merge default light dictionary),
  the app's `IThemeApplier` implementation (swap the theme dictionary + set `UserAppTheme`; on `System` follow
  `RequestedThemeChanged`), `Platforms/Android/Resources/values/colors.xml` (status/nav bar colours).
- **Core**: `ThemeService` unchanged in API; add `ThemePalette` pure resolver `(ThemeSetting, AppTheme systemTheme) → PaletteKey` if the applier needs logic (tested in Core).
- **Tests**: Core resolver tests; AppTests: applier swaps exactly one theme dictionary, repeated apply does not stack dictionaries.
- **Accept**: switching System/Light/Dark in Settings recolours every existing page live without restart; no page has a
  hard-coded colour left (`grep -E '#[0-9A-Fa-f]{6}' Views/*.xaml` returns nothing); contrast table in design-system holds.
- **Risk**: `AppThemeBinding` values cannot be `DynamicResource`; we use dictionary swap only, so remove existing
  `AppThemeBinding` colour usages in Styles.xaml in the same slice. Platform controls (Picker popups, Switch thumb on
  WinUI) may keep system colours — note, do not hack.

### Slice 2 — Icon font, typography and control variants
- **Files**: icon font TTF + its license file under `Resources/Fonts/` (font chosen in research.md), `MauiProgram.cs`
  (`AddFont`), new `Resources/Styles/Icons.xaml` (glyph code constants as `x:String` keys), `Styles.xaml`: text styles
  (Display/Title/Heading/Body/Caption/Overline/Numeric), Button classes `Primary`, `Outlined`, `Danger`, `Ghost`,
  `Icon`, Entry/SearchBar/Picker outlined look, `Chip*` styles, `SectionCard`. Apply the new classes to existing buttons
  without moving them (ProductsPage *Add product* = Primary, Delete = Danger, the rest Outlined).
- **Tests**: none in Core (pure XAML); AppTests: resource dictionary loads and every `StaticResource`/`DynamicResource`
  key used by the XAML exists (a small test that parses Views/*.xaml for keys and checks them against merged dictionaries).
- **Accept**: Windows build; every button on every page uses a variant class; disabled/pressed/hover states visible.
- **Risk**: `FontImageSource` colour in buttons needs explicit `Color` per state (no tint inheritance) — use
  `CommunityToolkit.Maui` `IconTintColorBehavior` only if slice 3 accepts the toolkit; otherwise set `Color` in styles.

### Slice 3 — MAUI servicing update, CommunityToolkit.Maui 16.0.0 and `IToastService`
- **Prerequisite**: the app currently resolves `Microsoft.Maui.Controls` **10.0.0**; CommunityToolkit.Maui **16.0.0**
  (the last version targeting net10; 17.x is net11-only) needs Controls **≥ 10.0.90**. First commit: set
  `<MauiVersion>` to the latest 10.0.x servicing release in the app csproj, run all tests, build Windows + Android
  (servicing releases are fixes only, but this is a dependency change — owner is told in the PR). If the owner refuses
  the bump, skip the toolkit: `IToastService` gets an in-page banner implementation and status bar colours go to
  `Platforms/Android` styles.
- **Files**: app csproj (package `CommunityToolkit.Maui` 16.0.0, pinned), `MauiProgram.cs` (`UseMauiCommunityToolkit`), Core `Services/IToastService.cs`,
  app `Services/MauiToastService.cs` (Snackbar with action, Toast), test fake `FakeToastService`.
- **Tests**: fake used by later ViewModel tests; AppTests for nothing platform-bound (wrapper is a pass-through).
- **Accept**: Windows + Android build (toolkit requires `.UseMauiCommunityToolkit()`; Windows Snackbar needs the
  documented WinUI setup — follow the package README, nothing else).
- **Risk**: package version must support `net10.0-*` (verified version in research.md); on Windows Snackbar needs
  app notifications config — if it fails, `MauiToastService` falls back to an in-page banner (still behind the interface).

---

## Phase B — shell and persistent context

### Slice 4 — Role-aware navigation menu (Core)
- **Files**: Core `Services/NavigationMenu.cs` (`For(Role) → IReadOnlyList<MenuEntry>`), `Services/MenuEntry.cs`,
  `Routes.cs` (`Receive = "receive"`, `Issue = "issue"`), `RolePermissions` unchanged (menu reads it).
- **Tests**: `NavigationMenuTests` (Driver has no Issue; Manager has all; order stable); extend
  `RolePermissionsMatrixTests` only if a rule changes (it should not).
- **Accept**: pure, no MAUI reference. **Risk**: low.

### Slice 5 — AppShell restructure (side panel on desktop, bottom tabs on phone)
- **Spike first (≤ 30 min, not committed)**: one `FlyoutItem AsMultipleItems` with tabs + `FlyoutBehavior`/`TabBarIsVisible`
  via `OnIdiom`; check WinUI rendering. Fallback documented in ux-plan §2.
- **Files**: `AppShell.xaml(.cs)`, `ShellNavigationService` (absolute routes to tabs), placeholder pages for Receive /
  Issue (title + EmptyState "Coming in slice 12"), Settings becomes a tab, `ProductsPage` loses Settings / Sign out
  buttons (moved to panel / More), the shell applies `NavigationMenu.For(role)` visibility on user change.
- **Tests**: Core navigation tests for new routes; existing ViewModel tests that asserted `OpenSettingsCommand` keep
  passing or are adapted (command stays for deep paths); UiTests: update AutomationIds in `UiScenarios.cs`.
- **Accept**: Windows: locked side panel with 4 entries; Android: 4 bottom tabs (Driver 3); login page shows no menu;
  sign-out returns to login and resets visible tabs.
- **Risk**: highest UI risk of the plan (Shell + WinUI). Keep the old shell in git history; one-commit revert is the fallback.

### Slice 6 — `SessionStatusViewModel` and `StatusHeader`
- **Files**: Core `ViewModels/SessionStatusViewModel.cs` (user name, initials, role text, `Connectivity` (existing
  `ConnectivityBannerViewModel`), `ReceiveDraftCount`/`IssueDraftCount` (0 until slice 11), `ScanCommand`,
  `OpenSyncCommand`); app `Views/Components/StatusHeader.xaml`, flyout header/footer templates; DI singleton.
- **Tests**: ViewModel tests with fakes (user change updates text; sign-out clears; offline chip text).
- **Accept**: header visible on Products, Settings (phone), panel shows the same data on desktop; ProductsPage no longer
  hosts its own connectivity labels (DRY).
- **Risk**: singleton VM bound in several pages — no page-specific state in it.

---

## Phase C — product table

### Slice 7 — Pure table model
- **Files**: Core `Services/TableColumn.cs` (key, title, `SortOption`, alignment, `IsSorted`, `IsDescending`,
  `ArrowGlyphKey`), `Services/ProductTable.cs` (`Columns(characteristics, wide|narrow, extraColumnId)`,
  `Toggle(columns, clicked) → new columns + (SortOption, descending)`), `Services/ProductTableRow.cs`
  (cells in column order from `ProductSummary`).
- **Tests**: toggle rules (new column → ascending; same column → flips; only one sorted column), cells for missing
  values (`—`), narrow layout = Name, Qty, chosen characteristic.
- **Accept**: pure; `ProductSorter` untouched. **Risk**: low.

### Slice 8 — Product list ViewModel: header sorting
- **Files**: `ProductListViewModel.cs` (`Columns`, `Rows`, `SortByColumnCommand`, `SelectedExtraColumn`,
  `ExtraColumnOptions`; remove `SortDescending`/`SelectedSortOption` from the public surface or keep them internal),
  drafts unaffected (sort not persisted — same as today? check and keep behaviour).
- **Tests**: update existing sort tests to drive `SortByColumnCommand`; search + sort combined; characteristic added at
  runtime appears as a column.
- **Accept**: ViewModel coverage gate still ≥ 60 % (it is ~97 %). **Risk**: many existing tests reference the picker
  properties — adapt, do not delete behaviour coverage.

### Slice 9 — Products page UI (table, wide + narrow)
- **Files**: `Views/Components/DataTableHeader.xaml`, `SortHeaderCell.xaml`, `Views/ProductsPage.xaml` (header + rows
  using `Grid ColumnDefinitions="*,Auto"` + `BindableLayout` cells with column widths from `TableColumn.Width`),
  `OnIdiom` / `AdaptiveTrigger`-style width switch (narrow < 600), `QuantityToColorConverter` replaced by a stock chip
  style (delete the converter if unused), `EmptyState` component, primary *Add product* button / phone round add button,
  red trash ghost button, `SwipeView` on phone.
- **Tests**: UiTests AutomationIds (`SortHeader_Name`, `SortHeader_Quantity`, `ProductRow`, `DeleteProductButton`);
  AppTests none.
- **Accept**: clicking a header sorts and shows the arrow; search still filters; Manager sees edit/delete, others not;
  delete asks for confirmation (existing `IAlertService.ConfirmAsync`); Windows and Android build.
- **Risk**: `CollectionView` + `SwipeView` on WinUI (swipe is touch-only) → `OnIdiom` shows inline buttons on desktop.

---

## Phase D — batch Receive / Issue

### Slice 10 — `StockDocument` model, migration 0005, repository
- **Files**: Core `Models/StockDocument.cs`, `Models/Operation.cs` (+ `DocumentId`), `Data/Migrations/0005_documents.sql`,
  `Data/Rows/StockDocumentRow.cs`, `Data/Rows/OperationRow.cs`, `Data/RowTables.cs`, `Data/IStockDocumentRepository.cs`,
  `Data/SqliteStockDocumentRepository.cs` (`AddAsync(document, operations)`: prepare all rows (encrypt), then one
  `InTransactionAsync` inserting document, operations and their `SyncJournal` entries), `IOperationRepository.GetRecentAsync`,
  `docs/domain/entities.md` (entity, diagram, who-does-what), in-memory test double.
- **Tests** (real temp SQLite): migration from a 0004 database keeps data; document + 3 operations committed with 4 queue
  rows in order; **one invalid operation (unknown ProductId → FK) rolls back everything, queue empty**; counterparty
  stored encrypted; `GetRecentAsync` newest first.
- **Accept**: existing 1226 tests green. **Risk**: `SqliteRepository` base is per-entity — add a protected
  multi-row transaction helper instead of copying code (DRY), refactor in its own commit if > few lines.

### Slice 11 — Draft model and store
- **Files**: Core `Services/StockDocumentDraft.cs` (lines, `AddScan(barcode)`, `SetQuantity`, `Remove`, totals),
  `Services/ScanDebouncer.cs` (`IClock`), `Services/StockDocumentDraftStore.cs` (+ interface; via `IAppStateService`,
  counterparty encrypted like `OperationDraftStore`), sign-out clears both drafts.
- **Tests**: repeat scan increments, debounce window, quantity 0 removes, totals, store round-trip, undecryptable
  counterparty dropped, no counterparty in logs (reuse the log-capture test pattern).
- **Risk**: low.

### Slice 12 — `StockDocumentService`
- **Files**: Core `Services/IStockDocumentService.cs`, `StockDocumentService.cs`, `DocumentNumberGenerator.cs`,
  DI registration.
- **Tests**: role refusal (Driver Out) with `Warning` and nothing written (Moq `Times.Never` on repository); unknown
  barcode; Out above on hand; duplicate lines merged; success logs once with ids only; number format and daily counter.
- **Risk**: logic overlap with `StockService.RecordAsync` — extract shared validation (role / quantity / product) into
  one `OperationRules` helper used by both (refactor commit first).

### Slice 13 — `StockDocumentViewModel` (Receive and Issue)
- **Files**: Core `ViewModels/StockDocumentViewModel.cs`, `ViewModels/StockDocumentLineItem.cs`, Core
  `Services/IScanFeedback.cs` + fake; uses `IToastService`, `IAlertService`, `IPermissionGuard`, `IBarcodeScanner`,
  `IProductCatalog`, `IStockService` (on hand per line), `IPageLifecycleAware` (camera on/off).
- **Commands**: `ScanDetected`, `AddTyped`, `Increment`, `Decrement`, `SetQuantity`, `RemoveLine` (+ undo),
  `CreateProduct` (Manager, unknown line), `Discard` (confirm), `Commit` (confirm summary, busy guard).
- **Tests**: ViewModel tests with fakes for every command and each failure row of ux-plan §6.3; end-to-end flow in
  `Tests/EndToEnd/`: *Driver receives 3 products → list quantities updated*, *Storekeeper issues too many → refused,
  nothing written*, *kill (new container with same state store) → draft restored*.
- **Risk**: biggest ViewModel; keep line logic in `StockDocumentDraft` (slice 11), the VM only orchestrates.

### Slice 14 — Receive page UI and continuous scanning
- **Files**: `Views/StockDocumentPage.xaml` (one page, title/labels from VM direction; two routes register the same
  page type), `Views/Components/QuantityStepper.xaml`, `ScanBox.xaml`, `DocumentLineRow` template,
  `CameraScannerView.cs` (`Continuous` bindable property: keep the reader running, raise every detection; debounce stays in Core),
  app `Services/MauiScanFeedback.cs` (`HapticFeedback`/`Vibration`, system tone; never throws), AppShell tab wiring.
- **Tests**: AppTests for feedback wrapper not throwing when vibration unsupported; UiTests IDs (`ScanBox`,
  `CommitDocumentButton`, `LineQuantity_{barcode}`).
- **Accept**: Windows: keyboard-wedge flow (type + Enter adds/increments); Android: camera continuous (owner verifies on a device).
- **Risk**: ZXing continuous detection floods events → debouncer; camera must stop on `OnDisappearing` and on tab switch.

### Slice 15 — Issue page specifics
- **Files**: VM direction-specific texts and stock validation display; page triggers for invalid lines; draft badge count
  wired into `SessionStatusViewModel`; product detail `[Receive]` / `[Issue]` buttons (open page with first line).
- **Tests**: VM: line invalid when above on hand, commit disabled; badge counts update on draft change.
- **Risk**: low.

---

## Phase E — polish and documentation

### Slice 16 — Restyle remaining pages (parallelisable per page)
16a Login · 16b Product detail (+ history table with document number) · 16c Product form · 16d Characteristics ·
16e Settings (sections, segmented theme control) · 16f Scanner + Operation (use QuantityStepper) · 16g Logs · 16h Map.
- **Files**: one page XAML each (+ its ViewModel only if a display property is missing, with a test).
- **Accept**: only shared styles/components; every interactive control keeps or gains an `AutomationId`.

### Slice 17 — Recent activity (only if Q4 = yes)
- **Files**: Core `ViewModels/RecentActivityViewModel.cs` (last 5, product name, user, direction, document number),
  panel on Products (desktop) and Settings/More (phone).
- **Tests**: VM with fakes; refresh after a commit.

### Slice 18 — Blue accent option (only if Q1 = yes)
- **Files**: `Themes/LightBlue.xaml`, `IThemeService` accent setting (+ `AccentSetting` enum, preferences key), Settings control.
- **Tests**: ThemeService persistence tests, resolver tests.

### Slice 19 — Documentation and release check
- Step document `docs/steps/10-redesign.md` (template), README step log + decision log rows (theme dictionaries,
  icon font, toolkit, Shell adaptive nav, StockDocument + 0005, header sorting), `docs/domain/*` updated, screenshots
  listed under **Owner actions**; Windows + Android builds; full test run; PR `redesign` → `design` (or as the owner
  decides) with ✔/✘ per slice.

---

## Parallel work

| Can run in parallel | Why it is safe | Watch |
|---------------------|----------------|-------|
| Slice 4 ∥ Slice 7 ∥ Slice 10 (after slice 1) | different Core folders/files | `ServiceCollectionExtensions.cs` one-line conflicts |
| Slice 11 ∥ Slice 12 (after 10) | draft vs service files | both register DI |
| Slice 16a–16h (after 2) | one page file each | none if VMs untouched |
| Slice 17 ∥ Slice 18 | unrelated | Settings page if both add controls (16e first) |

Strictly sequential: 1 → 2 → 5 → 6 (shell & styles touch shared XAML), 7 → 8 → 9, 10 → 12 → 13 → 14 → 15.
Suggested order for a single agent: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16a–h, 17?, 18?, 19.
