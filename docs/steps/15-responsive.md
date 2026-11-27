# Step 15 — Responsive layout: tables and navigation

- **Lab:** — (design iteration after revision 4)
- **Branch:** `design-responsive` (from `design`)
- **PR:** not opened

## 1. Before

- The Products table had a "narrow" mode under 600 wide: it hid the barcode, shrank the columns and offered one
  characteristic through a `▾` picker. Rows had a `SwipeView` on phones and buttons on desktop.
- The operation table (Activity, Home "Today", product history) had a `*` product column of one line and no minimum;
  Activity capped its content at 720 wide.
- The bottom bar had a center pill of a fixed width 172 inside a 720-max bar, so the side places were squeezed on
  320-360 dp phones and a tablet or laptop got a phone-style bar at the bottom.

## 2. What was asked

"On tablet, phone and laptop every element must be responsive. A table must keep its column sizes (roughly absolute
widths) and be scrollable left-right when it does not fit, rather than squeezing or hiding columns; when there is room
it stretches to the full width. Navigation elements must be responsive and comfortable on both tablet and phone."

## 3. Our interpretation

| Task | In StockRoom | Status |
|------|--------------|--------|
| Tables keep widths, scroll sideways | `WideTable` (horizontal `ScrollView` that sizes its content to `max(viewport, MinimumTableWidth)`) around the Products table and the three operation tables | done |
| Table fills the width when there is room | The name / product column is `*`; the 720 cap is removed on Products and Activity | done |
| Navigation comfortable on phone and tablet | Bottom bar under 600, navigation rail from 600 | done |

Table rule: **fixed-width columns plus one flexible column (name / product) with a minimum width.** Wider screen: the
flexible column stretches, the table fills the width. Narrower: the widths stay and the table scrolls sideways, header
and rows together; the rows still scroll vertically.

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| `WideTable` sets `Content.WidthRequest` in `OnSizeAllocated` (only when it changes) | A converter on the page width; a grid with a fixed width | Needs no page code-behind and reuses one view for all tables |
| `ProductTable.MinimumWidth` is pure and in Core; the view model exposes `TableMinimumWidth` | Computing it in XAML | Tested without a page; follows characteristics and the Manager's actions column |
| The narrow mode (`IsNarrow`, `ExtraColumnPicker`, code under the name) is removed | Keep it under 600 | The owner wants no hidden columns |
| Swipe on product rows is removed; edit and delete are a 104-wide actions column for the Manager | Keep the swipe | A horizontal swipe fights the horizontal scroll. 104 (not 96) because two 48 dp phone icon buttons and a gap are 104 |
| Home and product detail keep their 720 text column; their operation table is wrapped in a `WideTable` (376 minimum) | Remove the cap | Those pages are mostly text and short tables; the wrap only matters on phones narrower than 376 + padding |
| Activity: header + vertical scroll are inside one `WideTable`; the empty text is outside | Header outside | The header must scroll sideways with the rows and stay on top vertically |
| `BottomNav` builds only the active layout (`BottomNavBar` or `NavRail`) and decides from the width of its parent grid | Both layouts hidden / shown; its own width | AutomationIds stay unique; no oscillation when it changes size |
| Breakpoint 600 (`NavRailBreakpoint`), rail 88 wide (`NavRailWidth`) | | Material 3 "medium" window |
| Bar columns `*,*,2.4*,*,*`, pill max 200, button text 12 sp | `*,*,2*,*,*`, 14 sp | At 320 dp a half of the pill must fit "+ Receive" and the side places "Products" at 11 sp |

## 5. Changes

- Core: `ProductTable` (`Columns` without narrow / extra, `MinimumWidth`, `ActionsWidth`), `ProductTableRow` (no
  `ShowCode` / `CodeText`), `ProductListViewModel` (`TableMinimumWidth`; no `IsNarrow`, `ExtraColumnOptions`,
  `SelectedExtraColumn`).
- App: `Views/Components/WideTable.cs`, `BottomNav` (placement and mode), `BottomNavBar`, `NavRail`;
  `ProductsPage`, `ActivityPage`, `HomePage`, `ProductDetailPage`, `SearchPage` (rail column), `OperationTable`
  (two-line product name); `Styles.xaml` (`OperationTableMinWidth`, `NavRailBreakpoint`,
  `NavRailWidth`).
- Tests: columns always Barcode, Qty and every characteristic; `MinimumWidth` with and without actions;
  `TableMinimumWidth` follows characteristics and `CanManageProducts`.
- Not changed: StockCount, StockDocument and Search lists are product rows with steppers or fields, not multi-column
  tables, so they stay as they are.

## 6. Owner actions (by hand; the layout was not seen on a screen)

- Windows: resize the window across 600 wide. Under 600 a bottom bar, from 600 a rail on the left with Receive and
  Issue on top; no flicker at the edge; the app bar and content do not jump.
- Windows, wide: the Products table fills the width and the name column stretches; narrow the window and the table
  keeps its widths and scrolls sideways with the header; the vertical scroll still works.
- Phone (320-360 dp): the bar labels (Home, Products, Search, Activity, "+ Receive", "− Issue") are not cut; the
  Products table scrolls sideways; as Manager the edit and delete buttons are in the last column and line up with the
  header; Activity and the Home "Today" table scroll sideways and the product name wraps to two lines.
- Tablet: the rail is used, tables fill the screen, pull to refresh on Products still works inside the sideways scroll.
- The Android build needs `-p:AndroidSdkDirectory=<sdk>` on this machine when `ANDROID_HOME` is not set.

## 7. Open questions

- The Products table's page padding (24 on desktop) stays around the table; the table could run edge to edge later.
