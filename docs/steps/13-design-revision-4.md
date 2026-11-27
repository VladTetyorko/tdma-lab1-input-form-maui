# Step 13 — Design revision 4: from skeleton to a daily tool

- **Lab:** none (owner request; plan in [`../design/revision-4.md`](../design/revision-4.md), analysis in
  [`../design/journeys.md`](../design/journeys.md))
- **Branch:** `design`
- **PR:** not opened

## 1. Before

Revision 3 made the pages look right but did not change the paths: no bottom bar, two Home tiles and four other
buttons, the quantity asked before the product, bare confirm dialogs, a six-button product form, and the Driver's
Home listed other people's operations.

## 2. What was asked

Implement the slices V0 to V12 of the plan. Owner decisions (written approval):

1. The Home tiles go; receiving and issuing is the central button of the bottom bar.
2. Build the simple stock count: Manager only, the database quantity visible while counting, differences posted as
   compensating operations after confirmation.
3. The review offers the last 3 suppliers / recipients as chips; decrypted names only on screen, never logged.

Defaults: the fifth bar slot is Activity; a correction is a separate operation with a note (no new column); the Driver
sees only his own operations.

## 3. Our interpretation

| Slice | In StockRoom | Status |
|-------|--------------|--------|
| V0 navigation model | `NavigationMenu.BottomFor` / `DrawerFor`, new routes and role rules, `BottomNavViewModel` | done |
| V1 bottom bar | `BottomNav`, four root pages, task pages pushed and returning to their origin | done |
| V2 sheet and review | `ISheetService`, `AppSheet`, `DocumentReviewViewModel`, recent names, ⋮ menus | done |
| V3 operations | `IOperationFeed`, `OperationTable`, Activity, new Home | done |
| V4 product row | `ProductRow` everywhere a product is listed | done |
| V5 Search | `SearchPage` replaces the scanner page | done |
| V6 scan, then type | no Qty box, the new line's quantity focused | done |
| V7 product page | two buttons, ⋮ menu, photo menu, history table | done |
| V8 wizard | three steps over `ProductFormViewModel`; the form stays for editing | done |
| V9 correction | `OperationCorrection`, `OperationCorrector`, step 2 of the operation sheet | done |
| V10 stock count | `StockCountService`, `StockCountViewModel`, no schema change | done |
| V11 characteristics, settings | sheet, theme choices, collapsed Diagnostics; Operation page restyled | done |
| V12 documentation | this step, decision log 112-122, design system, domain documents | done |

### Differences from the plan

| Plan | Done | Why |
|------|------|-----|
| Spike: Toolkit `Popup` or our own modal page | Own modal page, no spike | It already worked on the emulator (revision 3) |
| `//settings` stays a root | Settings is a pushed page with ← | As a root without the bar it had no way back to Home |
| A shared root-page `ControlTemplate` | Each root page places `BottomNav` | One line per page; no template indirection |
| `PhotoSheetViewModel`; operation filters in the repository | One `ActionSheetViewModel` for every ⋮ menu; the feed filters in memory | Fewer types; about ten products |
| The product form page is replaced | It stays for editing from the product table (name, barcode, photo) | The wizard only adds |
| Search field focused on a reselect | Also focused when opened from the bar; the camera stays until typing starts | J4 needs 2 actions by name |
| "Draft kept" snackbar | A toast | Nothing to undo |

## 4. Tap counts, measured on the emulator

Counting rules of `journeys.md` section 1. The emulator has no barcode to point at: a scan was replaced by typing the
code into the search field with Enter (the keyboard-scanner path) and is not counted, nor is the tap that put the
cursor back into that field.

| Journey | Limit | Measured |
|---------|------:|---------:|
| J1 delivery, 3 products and a supplier | 7 (8 with a new name) | 8 with a new name; 7 when the name is a chip |
| J2 issue, 2 products, known recipient | 6 | 6 |
| J3 Driver drop-off, 2 products | 5 | 5 |
| J4 find by name / by code | 2 / 1 | 2 / 1 (code by the camera: not tried with a real barcode) |
| J5 unknown product during Receive (Open Food Facts answered) | 5 | 5 |
| J6 stock count, 10 products, 3 differ | 14 | 14 (Enter moves to the next product) |
| J7 correct a wrong quantity | 5 | 5 |
| Any root to any root | 1 | 1 |
| Back to the origin after success | 0 | 0 |

## 5. Screen rules (R1, R2)

| Screen | Filled | Visible secondary actions |
|--------|-------:|---------------------------|
| Home, Products, Search, Activity | the pill | Products: + ; Activity: filter chips |
| Receive / Issue | Review | ⋮ ; line steppers (row level) |
| Product page | the Receive / Issue pair | ⋮ |
| Wizard | Next / Add | steps 1-2 one outlined button, step 3 "+ New characteristic" |
| Stock count | Review | ⋮ |
| Characteristics | + New characteristic | none |
| Sheets | the result-named button | one way back |

Exceptions: Search with an unknown code shows a filled *Create product* beside the pill (two filled controls), and
Settings lists several outlined buttons inside the collapsed Diagnostics.

## 6. Found on the emulator and fixed

- Typing a quantity and then tapping elsewhere crashed the app (`VirtualView cannot be null`): the rebuilt list
  disconnected the focused box. `BorderlessEntry` handlers are no longer auto-disconnected.
- Back with the menu open left the app.
- The Search field had no cursor on arrival; a long "No product has barcode" ran under the stepper; the line text did
  not follow the typed number.

## 7. Not verified and not done

- Emulator only, not a phone. The host is slow; with the emulated camera on, Android's own services crashed once
  (system watchdog, camera provider), so the journeys ran with `-camera-back none`.
- No real barcode was scanned; the scan sound, photos, location, map tiles, biometrics and shake were not tried.
- Windows was built, not run. Appium UI tests were updated to the new ids, not run.
- A `UnhandledExceptionLoggerTests` test (a real unobserved task) failed once and passed on the next run.
- A correction is linked by its note only; see "Known limits" in the README.
- On a phone the cursor goes to the quantity after each scan, so a Bluetooth or USB keyboard scanner would type its next
  code into that box (at most 5 digits are taken). The camera scan is not affected; on a desktop the search box keeps
  the cursor. To be decided when such a scanner is used with a phone.
- In the operation sheet *Open product* stands where other sheets have *Back* (as in the wireframe): it navigates
  instead of only closing; closing is the scrim, the handle or the system Back.
- A draft typed just before the process is killed can be lost: drafts reach preferences when the window stops (as
  before this revision).

## 8. Screenshots

[`../design/screenshots/r4`](../design/screenshots/r4), from the final build (01-39 without a camera, 40-41 with the
emulated one). Compared with r3: the tiles and the recent-activity blob are gone; products and operations each have
one format; every question is a sheet.

## Owner actions

- Install the APK on a phone: scan real barcodes, check the quantity focus after a camera scan, try both themes.
