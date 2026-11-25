# Step 10 — UI redesign

- **Lab:** none (owner request after Lab 9; plan in [`docs/design/`](../design))
- **Branch:** `design` (based on `lab9`)
- **PR:** not opened (owner decides the base branch)

## 1. Before

The template look (purple, shadows in styles), one page stack behind a product list with a row of buttons, a sort
picker and a switch, and one operation recorded per scan.

## 2. What was asked

Implement the slices of [`docs/design/tasks.md`](../design/tasks.md): a calm green / dark violet design system, role-aware
navigation, a sortable product table, batch Receive and Issue, restyled pages and a small Recent activity list.

## 3. Our interpretation

Owner decisions that override the open questions of `ux-plan.md` section 10: green palette only (`#1F7A3A`), bottom tabs on
Android and a side panel on Windows, Recent activity yes, supplier / recipient on the document only, issuing more than on
hand is blocked, MAUI updated to the latest 10.0.x servicing release.

| Slice | In StockRoom | Status |
|-------|--------------|--------|
| 1 tokens and theme switching | `Themes/LightGreen.xaml`, `Themes/Dark.xaml`, `ThemePalette`, `MauiThemeApplier` swaps the dictionary | done, not seen on screen |
| 2 icons, text styles, button variants | Fluent System Icons font (MIT), `Icons.xaml`, style classes | done, partly: no outlined look for `Entry` / `Picker` / `SearchBar`, no keyboard focus ring |
| 3 MAUI servicing and toolkit | MAUI 10.0.130, CommunityToolkit.Maui 16.0.0, `IToastService` | done; toasts only on Android / iOS |
| 4 role-aware menu | `NavigationMenu.For(role)` | done |
| 5 shell, 6 status header | `AppShell` built in code, `SessionStatusViewModel`, header / panel components | done (one commit for both), not seen on screen |
| 7 table model, 8 header sorting, 9 table UI | `ProductTable`, `Columns` / `Rows`, `ProductsPage` | done, partly: no pull to refresh, hover row, skeleton rows or `EmptyState` component |
| 10 document model, migration 0005 | `StockDocument`, `Operation.DocumentId`, `SqliteStockDocumentRepository` | done |
| 11 draft, 12 service, 13 ViewModel | `StockDocumentDraft`, `StockDocumentService`, `StockDocumentViewModel` | done |
| 14 page and scanning, 15 Issue specifics | `StockDocumentPage` (one page, both directions), `MauiScanFeedback`, draft counts, detail shortcuts | done, partly: see section 6 |
| 16 restyle remaining pages | cards, titles, shared padding, AutomationIds | done, partly: see section 6 |
| 17 recent activity | `RecentActivityViewModel`, panel on Products (desktop) and Settings (phone) | done |
| 18 blue accent | | skipped (owner: green only). A new palette is one dictionary plus a `PaletteKey` entry |
| 19 documentation | this document, README, domain docs | done |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| Raw colours live in the two theme dictionaries; there is no `Colors.xaml` | primitives file referenced by the themes | a MAUI `Color` element cannot alias another resource |
| Theme contents are tested by reading the XAML as XML in the Core test project | instantiate the dictionaries | brushes need a UI thread, which the test host does not have |
| MAUI 10.0.0 → 10.0.130 | stay and hand-make toasts | both targets built and all tests passed |
| No toast on Windows | Windows app notifications | the app is unpackaged; pages show status text and an Undo button instead |
| Shell entries are added in code (`FlyoutItem` per entry on desktop, one `TabBar` on a phone) | one `FlyoutItem AsMultipleItems` for both | avoids relying on how WinUI draws tabs inside a locked flyout |
| Sort state stays `SelectedSortOption` + `SortDescending`; headers call `SortByColumnCommand` | replace the properties | existing behaviour tests stay valid |
| `StockChanges` version counter tells the product list to reload | events | nothing to unsubscribe |
| Batch camera scanning reuses `CameraScannerView` unchanged | a `Continuous` property | the control already reports every read; repeats are dropped by `ScanDebouncer` |
| Recent activity shows every role all operations | Driver sees own only | decision 85 |

## 5. Changes

- **Core:** `ThemePalette`, `NavigationMenu`, `ProductTable` (+ column, row, cell), `StockDocument`, migration
  `0005_documents.sql`, document repository, draft + store, `ScanDebouncer`, `DocumentNumberGenerator`, `OperationRules`,
  `StockDocumentService`, `StockChanges`, `IToastService`, `IScanFeedback`, `SessionStatusViewModel` (sign-out moved here
  from the product list), `StockDocumentViewModel`, `RecentActivityViewModel`, `ICurrentUserService.Changed`.
  The connectivity refresh now catches a failing queue read (it could end the process).
- **App:** theme dictionaries, styles, icon font, `AppShell`, components in `Views/Components`, `ProductsPage` table,
  `StockDocumentPage`, restyled pages, `MauiToastService`, `MauiScanFeedback`, `VIBRATE` permission.
- **Removed:** `QuantityToColorConverter`, the sort picker, `ProductListBannerTests` (covered by
  `SessionStatusViewModelTests`).
- **Tests:** Core 1226 → 1309, AppTests 42 (converter tests removed, theme swap and scan feedback added).

## 6. Not verified and not done

**Not verified.** The app was never started: no screen, theme switch, navigation, swipe, binding, camera, vibration or
toast was seen at run time. A wrong binding path or a Shell problem would build and pass the tests. The Appium tests were
adapted (sign out now lives in the panel or on Settings) but not run. iOS and Mac Catalyst were not built.

**Not done compared with the plan.**

- Contrast values were taken from the design document, not re-measured on screen.
- Settings keeps a theme picker (no segmented control), has no scan sound / vibration switches, no About card; the Security
  and Diagnostics sections are not cards.
- No scan sound (vibration only); no result banner inside the camera frame, no torch or pause button.
- `QuantityStepper`, `ScanBox`, `DataTableHeader` and `EmptyState` are inline XAML, not separate components; the Operation
  page keeps its `Stepper`.
- Product detail history does not show the document number.
- No `Ctrl+Enter` commit; the header Scan button opens the scanner page also from Receive / Issue.
- A restart opens Products, not the Receive / Issue page that was open (the draft and the count in the menu are there).
- Operations of a document have no location.
- The app icon and splash SVG still carry the template purple inside the artwork.

## Owner actions

1. Start the app on Windows and on an Android device and check: sign-in, side panel / bottom tabs for each of the three
   users (the Driver has no Issue), theme System / Light / Dark switching live, header sorting, row edit / delete (swipe on
   the phone), Receive and Issue with a keyboard or USB scanner, continuous camera scanning and vibration on the phone.
   **Back up `stockroom.db3` first**: the first start applies migration 0005, which older builds refuse to open.
2. Take the screenshots for the report (both themes: sign-in, products, receive, settings).
3. Run the Appium tests (`Mobile_apps_L1_Model_view.UiTests`) with a device.
4. Decide the pull request base for `design`.
