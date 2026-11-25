# UX plan — StockRoom redesign

Status: **implemented in part** on branch `design`; what differs is listed in [`../steps/10-design.md`](../steps/10-design.md).
Read with [`design-system.md`](design-system.md) (tokens, components' look) and [`tasks.md`](tasks.md) (order of work).
Evidence for the choices is in [`research.md`](research.md).

## 1. Goals and non-goals

Goals, from the owner's request and the research:

1. **One tap for the frequent action.** Scanning, receiving and issuing goods are reachable from every main page.
2. **A real table** for products: click / tap a column header to sort, arrow shows the direction, search stays.
3. **Batch work**: a delivery of 20 items is one *Receive* document, not 20 separate operations typed one by one.
4. **Always know where you are**: who is signed in (role), online / offline / waiting changes, on every page.
5. **Calm look**: white + green (light), near-black + violet (dark), borders instead of shadows, no glow.
6. **Same app on Android (main) and Windows (usable)**: adaptive layout, not two apps.

Non-goals (AGENTS.md §1): no dashboards with charts, no prices, no approvals, no several warehouses, no new auth.
The "Recent activity" panel below is a plain list of the last operations, not a dashboard; it is an **owner decision** (Q4).

## 2. Navigation map

```
                         +-----------+
   app start ----------> |  Startup  | --(no session)--> Login --(ok)--+
                         +-----------+                                  |
                               | (session restored)                     v
                               +-----------------------------> MAIN NAVIGATION (role-aware)
                                                                        |
   +-----------------+-------------------+-------------------+----------+---------+
   |                 |                   |                   |                    |
 Products         Receive             Issue              Scan (quick)          Settings
 (table)          (batch In)          (batch Out)        (single product)      (theme, sync, account,
   |                 |                   |                   |                  diagnostics*, logs*)
   +-> Product detail (characteristics, history, photo*)    +-> Operation (single In/Out, existing)
   |      +-> Edit product*                                  +-> Create product* (unknown barcode)
   +-> Add product*                                         
   +-> Characteristics*                                      * = Manager only
   +-> Map of located operations
```

Role-aware menu (single source: a new pure `NavigationMenu.For(Role)` in Core, built from `RolePermissions`):

| Entry | Storekeeper | Driver | Manager | Route (new = **bold**) |
|-------|:-----------:|:------:|:-------:|------------------------|
| Products | yes | yes | yes | `products` |
| Receive goods | yes | yes | yes | **`receive`** |
| Issue goods | yes | — (hidden) | yes | **`issue`** |
| Scan (quick action, not a tab) | yes | yes | yes | `scanner` |
| Map | yes | yes | yes | `map` (from Products toolbar) |
| Characteristics | — | — | yes | `characteristics` (from Products toolbar) |
| Settings | yes | yes | yes | `settings` |
| Logs | — | — | yes | `logs` (inside Settings) |

Driver never sees *Issue*: hidden in the menu (UI layer) **and** refused by `IStockDocumentService`
(`RolePermissions.CanRecord(role, Out)`, service layer), per AGENTS.md §2a.

### Shell structure (one `AppShell`, adaptive)

- **Windows / tablet (width ≥ 840)**: `FlyoutBehavior="Locked"` — a permanent left side panel (240 px) with the menu,
  the user card on top and the sync status at the bottom. Bottom tab bar hidden.
- **Android phone**: `FlyoutBehavior="Disabled"`, a **bottom tab bar** with 4 tabs: *Products, Receive, Issue, More*
  (Driver: 3 tabs). *More* = Settings page with account, theme, sync and (Manager) diagnostics and logs.
  Thumb-reachable tabs beat a hamburger menu for the two actions used all day (research §3).
- Implementation: one `FlyoutItem` with `FlyoutDisplayOptions="AsMultipleItems"` holding one `Tab` per entry;
  `Shell.FlyoutBehavior` and `Shell.TabBarIsVisible` set with `OnIdiom` (Phone vs Desktop/Tablet).
  Login and Startup stay outside the flyout (`FlyoutItemIsVisible="False"`, absolute routes as today).
  **Risk**: how WinUI renders Shell tabs inside a locked flyout must be checked in slice 4 (spike first, 30 min);
  fallback = build the side panel as a plain `Grid` column in a `MainLayout` page template on desktop.
- Role visibility: `AppShell` sets `IsVisible` of each `Tab` from `NavigationMenu.For(role)` when the user changes
  (code in the app's `Shell*` class, logic in Core, tested there).

## 3. Persistent context ("see your journey from any page")

A reusable **`StatusHeader`** (ContentView) on every main page, bound to one singleton `SessionStatusViewModel`:

```
 phone (48 px)                                              desktop (in the side panel instead)
+----------------------------------------------------+     +------------------------------+
| [O] Petro K. · Storekeeper     (o) Online · 2 wait  [|||] |     | (PK) Petro Kovalenko         |
+----------------------------------------------------+     |      Storekeeper              |
   avatar initials   role chip      sync chip     scan    |  ...menu...                  |
                                                           |  (o) Online · all synced     |
                                                           |  [ Scan ]   (outlined)       |
                                                           |  Sign out                    |
                                                           +------------------------------+
```

- Sync chip states (existing `ConnectivityBannerViewModel`): *Online · all synced* (neutral), *Syncing…* (info),
  *N changes waiting* (warning), *Offline — saved on this device* (warning, never red: offline is normal in a warehouse).
  Tap = go to Settings › Sync.
- Scan button = quick action from anywhere (also shake, existing). On the Receive / Issue pages it focuses that page's scanner instead.
- **Draft badge**: when a Receive or Issue draft has lines, its tab shows a count badge (e.g. *Receive (5)*), so a
  half-done delivery is never forgotten (research: lost work is a top complaint).

## 4. Screens — wireframes

Legend: `[Button]` outlined, `[[Button]]` filled primary (green), `{x}` destructive red outlined/ghost icon,
`( )` chip, `▲▼` sort arrow, `|||` scan icon.

### 4.1 Login (both)

```
+--------------------------------------+
|              StockRoom               |
|        Warehouse · sign in           |
|  Login     [__________________]      |
|  Password  [______________] (eye)    |
|  [[        Sign in          ]]       |
|  [  Use fingerprint  ]  (if enabled) |
|  error text (red, one line)          |
+--------------------------------------+
```
Centered card, max width 400 on desktop. No change in behaviour.

### 4.2 Products — Windows (wide)

```
+------------+-------------------------------------------------------------------------------+
| (OK) Olena | Products                                   [ Map ] [ Characteristics ] [[+ Add product]] |
|  Manager   | [ Search name, barcode or value ________________ (x) ]      12 products       |
|------------|-------------------------------------------------------------------------------|
| Products   | NAME ▲               | BARCODE        | QTY  | BRAND      | WEIGHT, g |        |
| Receive (3)|----------------------+----------------+------+------------+-----------+--------|
| Issue      | Nutella 400 g        | 3017620422003  |  24  | Ferrero    |   400     | [/] {🗑} |
| Settings   | Oreo Original        | 7622210449283  | (0 Out of stock) | Oreo | 154 | [/] {🗑} |
|            | ...                                                                    hover = |
|            |                                                                  soft green row|
|------------|-------------------------------------------------------------------------------|
| (o) Online |  Recent activity: +12 Nutella (Petro, 10:42) · −3 Oreo (Olena, 10:15)  [Show] |
| [ Scan |||]|                                                                               |
| Sign out   |                                                                               |
+------------+-------------------------------------------------------------------------------+
```

- Header cells are buttons: click = sort ascending by that column; click again = descending; the arrow and a bold label
  mark the sorted column; other headers show no arrow (a faint `↕` on hover). Default sort: Name ▲.
- Quantity right-aligned, tabular figures. 0 → red *Out of stock* chip; negative impossible by domain rules.
- Row click → product detail. Row actions (Manager only): edit (pencil, ghost) and delete (trash, red ghost);
  trash disabled with tooltip *"Has stock history — cannot be deleted"* when `HasOperations`.
- `[[+ Add product]]` is the only filled button on the page (one primary action per screen).

### 4.3 Products — Android (narrow)

```
+------------------------------------+
| (PK) Petro · Storekeeper  (o) 2  |||| <- StatusHeader
| Products                       12  |
| [ Search ______________________ x ]|
| NAME ▲            | QTY | BRAND ▾  | <- 3rd column = chosen characteristic
|-------------------+-----+----------|
| Nutella 400 g     |  24 | Ferrero  |
| 3017620422003     |     |          |
|-------------------+-----+----------|
| Oreo Original     | (0) | Oreo     |
| 7622210449283     |     |          |
|  ... swipe left (Manager): [Edit] {Delete} |
|                              ( + ) | <- flat green round "Add product", Manager only, no shadow
+------------------------------------+
| Products | Receive(3) | Issue | More |
+------------------------------------+
```

- Narrow table: **Name** (+ barcode as second line), **Qty**, and **one characteristic column** chosen from the `▾`
  menu (all characteristics listed). Tapping a header sorts; same toggle rules as desktop. No horizontal scrolling.
- Row actions by `SwipeView` (Edit, Delete in red) for the Manager; the same actions are on the detail page, so
  swipe is a shortcut, not the only way (discoverability).
- Pull to refresh (`RefreshView`) re-reads quantities.

### 4.4 Receive goods (batch In) — Android

```
+------------------------------------+
| StatusHeader                       |
| Receive goods          Draft · 5 lines · 37 units |
| +--------------------------------+ |
| |      camera preview 16:9       | | <- continuous scanning; flash/torch toggle; pause button
| |   last: Nutella 400 g  +1 ✓    | | <- result toast inside the frame, 1.5 s
| +--------------------------------+ |
| [ Type or scan barcode ____ ] [Add]| <- also takes a Bluetooth/USB keyboard-wedge scanner
| Supplier (optional) [__________ ]  |
|------------------------------------|
| Nutella 400 g      [-] 12 [+]  {x} | <- QuantityStepper; tap number to type
| on hand 24 → 36                    |
| Oreo Original      [-]  6 [+]  {x} |
| on hand 0 → 6                      |
| ⚠ 4006040000000 unknown  [Create]* {x} | <- not committable until resolved (*Manager) or removed
|------------------------------------|
| [ Discard draft ]  [[ Receive 37 units ]] |
+------------------------------------+
| Products | Receive(5) | Issue | More |
+------------------------------------+
```

### 4.5 Receive / Issue — Windows

```
+------------+-------------------------------------------------------------------------------+
| side panel | Receive goods                                     Draft · 5 lines · 37 units  |
|            | [ Scan or type barcode ____________________ ] [Add]   Supplier [___________]  |
|            | (focus stays in this box: a USB scanner types + Enter → line added)           |
|            |-------------------------------------------------------------------------------|
|            | PRODUCT            | BARCODE        | ON HAND | QTY            | AFTER |       |
|            | Nutella 400 g      | 3017620422003  |   24    | [-] 12 [+]     |  36   | {x}   |
|            | Oreo Original      | 7622210449283  |    0    | [-]  6 [+]     |   6   | {x}   |
|            |-------------------------------------------------------------------------------|
|            |                                 [ Discard draft ]  [[ Receive 37 units ]]     |
+------------+-------------------------------------------------------------------------------+
```

Issue goods is the same page with direction *Out*: button *Issue N units* (filled green; red is reserved for
destructive actions, and issuing goods is normal work), field *Recipient*, column *After* turns red and the line shows
*"Only 4 on hand"* when the quantity exceeds stock; commit disabled while any line is invalid.

### 4.6 Product detail

Header (name, barcode, photo thumbnail, big quantity), two sections: *Characteristics* (key/value grid, Manager edits
inline as today) and *History* (table: date, In/Out chip, qty, user, document number when part of a batch).
Actions: `[Receive]` `[Issue]` (open the batch page with this product as the first line), Manager: `[Edit]`, `{Delete}`.

### 4.7 Settings ("More" on phone)

Sections as cards with 1 px borders: *Account* (name, role, Sign out), *Appearance* (Theme: System / Light / Dark as a
segmented control; Accent: Green / Blue if Q1 is accepted), *Scanning* (shake to scan, scan sound, vibration),
*Sync* (status, waiting count, Sync now, Simulate offline, Share database), *Security* (biometric unlock),
*Diagnostics* and *Logs* (Manager), *About* (version, Open Food Facts ODbL attribution).

### 4.8 Other pages

Scanner (single product), Operation (single In/Out), Product form, Characteristics, Logs, Map: restyled with the shared
components only (slice 16); no flow change. The single Operation page stays for "one quick correction" and deep links.

## 5. User journeys per role

| Role | Typical day | Path in the new UI | Taps (phone) |
|------|-------------|--------------------|--------------|
| **Driver** (In only) | Brings a delivery to the warehouse | Receive tab → scan items (continuous) → adjust 1–2 quantities → *Receive N units* → confirm | 3 + scans |
| **Storekeeper** | Accepts deliveries, issues goods to orders, answers "do we have X?" | Receive / Issue tabs as above; Products → search → read Qty; Scan quick action → product → single Operation for a correction | 1–3 |
| **Manager** | Adds new products, fixes data, checks history and logs | Products → *+ Add product* (or Scan unknown → *Create from Open Food Facts*); detail → edit characteristics; Settings → Logs | 2–4 |

## 6. Batch Receive / Issue — behaviour and transaction semantics

### 6.1 Model (minimal change, already foreseen in `entities.md` "Delivery documents")

New entity **`StockDocument : OwnedEntity`** — one receipt or one issue:

| Field | Type | Rules |
|-------|------|-------|
| `Number` | `string` | Human-readable, unique: `R-20261124-0001` / `I-…`; made by a pure `DocumentNumberGenerator` from `IClock` + count of the day (per device; the server can renumber later — stated limit) |
| `Direction` | `OperationDirection` | `In` = receipt, `Out` = issue |
| `Counterparty` | `string?` | Supplier / recipient; **encrypted** like `Operation.Counterparty` (`IFieldEncryptor`), never logged |
| `Note` | `string?` | Free text (waybill number, order #) |

`Operation` gets **`DocumentId Guid?`** → `StockDocument.Id`. Operations of a document keep `Counterparty` and `Reason`
`NULL` (3NF: those facts belong to the document); single operations keep working exactly as now.
A document is **immutable** like its operations (no edit, no delete; a mistake = a new opposite document or operation).

Migration **`0005_documents.sql`**:

```sql
CREATE TABLE StockDocuments (
    Id TEXT PRIMARY KEY NOT NULL,
    Number TEXT NOT NULL UNIQUE,
    Direction TEXT NOT NULL CHECK (Direction IN ('In', 'Out')),
    Counterparty TEXT NULL,
    Note TEXT NULL,
    CreatedByUserId TEXT NOT NULL REFERENCES Users(Id),
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
ALTER TABLE Operations ADD COLUMN DocumentId TEXT NULL REFERENCES StockDocuments(Id);
CREATE INDEX IX_Operations_DocumentId ON Operations(DocumentId);
```
(Exact column types follow `0001_initial.sql` conventions; the implementer checks them there. SQLite allows
`ADD COLUMN … REFERENCES` because the default is `NULL`. "Operation direction equals document direction" cannot be a
`CHECK` across tables: the service enforces it and a repository test proves the service path.)

### 6.2 Draft (before commit)

- The page edits a **`StockDocumentDraft`** (direction, counterparty, note, lines: barcode + quantity), kept by a
  `StockDocumentDraftStore` through `IAppStateService` (one draft per direction), the counterparty encrypted exactly like
  `OperationDraftStore` does. The draft survives minimise, kill and restart (Lab 3 rules) and is cleared by commit,
  *Discard draft* (confirm) or sign-out.
- **Scan rules** (pure `StockDocumentDraft` methods, unit-tested):
  - same barcode scanned again → that line +1 (no duplicate lines); the line flashes and moves to the top;
  - `ScanDebouncer`: the same code within 1.2 s from the camera is ignored (camera reports a code many times per second);
    a keyboard-wedge or manual entry is never debounced (the user meant it);
  - unknown barcode → line in *warning* state; Manager can *Create* (existing product form, prefilled / Open Food Facts),
    others remove it; commit is blocked while unknown lines exist;
  - quantity: stepper ±1, tap to type; 0 removes the line after confirm-less *Undo* snackbar (5 s);
  - Issue only: each line shows on hand; quantity above on hand → line invalid (red text, reason), commit disabled.
- Feedback per scan: short beep + vibration (setting), green check toast with product name; error: double buzz + red toast.
  Behind a new `IScanFeedback` (app: `HapticFeedback` / `Vibration` from Essentials + a short sound via the existing
  platform; Windows: no-op vibration).

### 6.3 Commit — one transaction

`IStockDocumentService.CommitAsync(StockDocumentDraft)` → `Result<StockDocument>`:

1. Signed in? Role may record this direction for **every** line (`RolePermissions.CanRecord`)? Else `Warning` log + refusal.
2. Resolve each barcode to a product; no unknown lines; quantities > 0; duplicate barcodes merged.
3. For `Out`: quantity per product ≤ quantity on hand **read at commit time** (not the value shown when scanned).
4. Build one `StockDocument` + N `Operation`s with the same `CreatedAt`, `CreatedByUserId`, `DocumentId`.
5. `IStockDocumentRepository.AddAsync(document, operations)` — rows prepared first (encryption is async), then **one
   `RunInTransactionAsync`** inserts the document, all operations and one `SyncQueue` row per inserted row through
   `SyncJournal` (document first, so a server applying the queue in order never sees a dangling `DocumentId`).
6. Log once: `Information` "Committed {Direction} document {DocumentId} with {LineCount} lines, {Units} units by user
   {UserId}" (no counterparty).

Failure handling:

| Failure | What the user sees | Data | Log |
|---------|--------------------|------|-----|
| Validation (role, unknown, stock) | the line(s) marked + one message; draft kept | nothing written | `Warning` |
| Encryption of counterparty fails | "Not saved: …"; draft kept | nothing written | `Error` with exception |
| SQLite error during the transaction | "Not saved, nothing was recorded. Try again."; draft kept | **rolled back as a whole** | `Error` (existing `InTransactionAsync`) |
| App killed during commit | on restart the draft is still there; if the commit had finished, the draft was cleared after it (clear only after success) | all or nothing | — |
| Offline | commit works (local first); chip shows *N changes waiting* | queued | existing sync logs |
| Double tap on commit | command disabled while running (`IsBusy`), so one document | — | — |

Known limit: on a single device the stock check (step 3) and the insert (step 5) are separate awaits; with one writer
this is safe. Moving the check inside the transaction is listed as an option if multi-device pull sync ever arrives.

## 7. Reusable components

All in the app's `Views/Components/` (one public type per file), styled only through `Resources/Styles`, every
interactive part with an `AutomationId` and `SemanticProperties`.

| Component | Kind | Bindable API (sketch) | Used on |
|-----------|------|-----------------------|---------|
| `StatusHeader` | ContentView | `BindingContext = SessionStatusViewModel` | every main page (phone) |
| `SidePanel` content (flyout header/footer) | DataTemplate | same VM | Shell (desktop) |
| `DataTableHeader` | ContentView | `Columns` (list of `TableColumn`), `SortCommand` | Products, Receive, Issue, History |
| `SortHeaderCell` | ContentView | `Title`, `IsSorted`, `IsDescending`, `Command`, `CommandParameter` | inside header |
| `ProductRow` | DataTemplate | `ProductTableRow` | Products |
| `DocumentLineRow` | DataTemplate | `StockDocumentLineItem` | Receive, Issue |
| `QuantityStepper` | ContentView | `Value` (two-way), `Minimum`, `Maximum`, `AutomationIdPrefix` | lines, Operation page |
| `ScanBox` | ContentView | `Text`, `SubmitCommand` (Enter), keeps focus | Receive, Issue, Scanner |
| `CameraScannerView` | existing | add `Continuous` mode | Scanner, Receive, Issue |
| `Chip` | Style on `Border`+`Label` (`StyleClass`) | Neutral / Success / Warning / Danger / Info | status, role, stock |
| `EmptyState` | ContentView | `Icon`, `Title`, `Message`, `ActionText`, `ActionCommand` | lists, search with no result |
| `ConfirmDialog` | existing `IAlertService.ConfirmAsync` | — (restyle later with Toolkit Popup only if needed) | delete, discard, commit |
| `Snackbar/Toast` | `IToastService` (new contract) | `ShowAsync(text, actionText?, action?)` | undo remove line, saved |
| `SectionCard` | Style on `Border` | — | Settings, detail |
| `IconButton` | Style on `Button` with `FontImageSource` | Ghost / Outlined / Danger | row actions, header |

## 8. What changes in Core (contracts first)

| Layer | New / changed | Tests |
|-------|---------------|-------|
| Models | `StockDocument`; `Operation.DocumentId` | model tests |
| Data | `0005_documents.sql`; `StockDocumentRow`; `IStockDocumentRepository` (`AddAsync(document, operations)`, `GetByIdAsync`, `GetRecentAsync`); `IOperationRepository.GetRecentAsync(int count)`; `OperationRow.DocumentId` | real temp SQLite: insert all, **rollback when one row fails** (e.g. FK violation), queue rows in order, migration from 0004 data |
| Services | `IStockDocumentService` + `StockDocumentService`; `StockDocumentDraft` (+ line rules), `StockDocumentDraftStore`; `DocumentNumberGenerator`; `ScanDebouncer`; `ProductTable` / `TableColumn` / `ProductTableRow` (pure table model: columns, cells, header sort state); `NavigationMenu.For(Role)`; `IScanFeedback`, `IToastService` (contracts); `IThemeService` gains accent (only if Q1) | unit tests with fakes; role matrix test extended with "commit document" |
| ViewModels | `SessionStatusViewModel` (wraps `ICurrentUserService` + `ConnectivityBannerViewModel` + draft counts); `StockDocumentViewModel` (one class, direction from navigation parameter / route); `ProductListViewModel`: `Columns`, `SortByColumnCommand`, `SelectedExtraColumn` (phone), replaces `SelectedSortOption` + `SortDescending` UI (internally still `ProductSorter`); `RecentActivityViewModel` (if Q4) | ViewModel tests with fakes (coverage gate ≥ 60 %), end-to-end: *log in as Driver → receive 3 lines → list shows new quantities; Driver cannot open Issue; Storekeeper issue more than on hand → refused, nothing written* |
| App | Theme dictionaries, styles, icon font, `AppShell` restructure, components, pages, `MauiScanFeedback`, `MauiToastService` (CommunityToolkit.Maui Snackbar/Toast, see research), `ThemeApplier` swaps dictionaries | AppTests for applier; UI tests updated (`SortPicker` disappears → `SortHeader_Name`) |

## 9. Accessibility

- Text contrast ≥ 4.5:1, UI boundaries ≥ 3:1 (checked in `design-system.md`); state never by colour only: sort arrow +
  bold, stock chip has text (*Out of stock*), invalid line has an icon and a sentence.
- `SemanticProperties.Description` on icon-only buttons ("Delete Nutella 400 g"), `SemanticProperties.HeadingLevel` on
  page titles, header cells announce "Name, sorted ascending, button".
- Touch targets ≥ 48 dp on Android (stepper buttons 48×48), ≥ 32 px on Windows with keyboard focus visible (2 px
  primary focus border).
- Respect system font scaling (no fixed heights on text rows; rows grow).
- Windows keyboard: Tab order follows reading order; Enter in the scan box adds the line; `Ctrl+Enter` commits the
  document (confirm dialog); `Esc` closes dialogs. A keyboard-wedge barcode scanner works because the scan box keeps focus.
- Sounds and vibration can be switched off in Settings.

## 10. Open decisions for the owner

| # | Question | Proposal |
|---|----------|----------|
| Q1 | Ship the blue/light-blue accent as a user setting, or keep green only? | Design both, implement green; blue = optional slice 18 (YAGNI) |
| Q2 | Exact brand green: "like Fora" — use Fora's colour or our own accessible green? | Our own `#1F7A3A` family (Fora-like lime only as a decorative tint): white text on Fora's light lime fails WCAG, and we must not imitate a real brand |
| Q3 | Phone: bottom tabs (proposed) or a hamburger flyout like desktop? | Bottom tabs |
| Q4 | "Recent activity" list (last 5 operations, all roles) on the products page / side panel? Not a dashboard, but new | Yes, small, read-only |
| Q5 | Documents: keep counterparty and note on the document only (proposed) or also per line? | Document only |
| Q6 | Should a committed document be printable / shareable as a waybill (PDF/text)? | Not now (scope); the number is shown for reference |
| Q7 | Low-stock warning needs a per-product minimum (`Product.MinQuantity`, new column). Add it? | Not in this redesign; only *Out of stock* (0) is highlighted |
| Q8 | Issue page: allow issuing more than on hand with a warning, or block (proposed)? | Block — matches the existing domain rule "Out ≤ on hand" |
| Q9 | Scan sound: needs a small sound asset (license) or the platform tone; acceptable? | Platform/system tone, no asset |
| Q10 | Font: keep Open Sans (template, OFL) or switch to Inter (OFL, better tabular digits)? | Keep Open Sans; revisit with the Figma pass |
| Q11 | Raise MAUI from 10.0.0 to the latest 10.0.x servicing release so CommunityToolkit.Maui 16.0.0 (needs ≥ 10.0.90) can be used? | Yes (fix-only releases); without it, toasts and status-bar colour are hand-made |
