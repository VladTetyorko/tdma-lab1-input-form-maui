# Design revision 4: from skeleton to a daily tool

Status: **plan, not implemented.** It builds on [`journeys.md`](journeys.md) (personas, journeys, tap counts,
sources) and on revisions 2 and 3. Where they disagree, this file wins.

These requirements still hold:

- green light theme and violet dark theme, minimal shadows, no neon;
- camera first;
- quick search shows the product first and the code second;
- one big action at the bottom;
- a sortable table header;
- a document is committed in one transaction;
- the role rules are enforced twice (UI and service, AGENTS §2a);
- shared styles, no inline values.

## 1. What changes, in one paragraph

A **bottom bar** gives four places and one action:

- **Home**: what needs me today;
- **Products**: the table;
- a split central **+ Receive | − Issue** button;
- **Search**: find one product, by name or by camera;
- **Activity**: every operation, as a table.

The drawer keeps rare things: Stock count, Characteristics, Map, Settings, the account and Sign out. Every screen gets
at most one filled button and two visible secondary actions; the rest moves to a ⋮ menu or disappears. Products
look the same everywhere (one product row) and operations look the same everywhere (one operation table). Every
question the app asks uses one styled **sheet** with the fields the task needs and a named next step. Adding a
product becomes a 3-step wizard. Two journeys get new short paths: correcting a mistake (from the operation) and a
stock count (new; needs the owner's approval, §8).

## 2. Owner feedback → decisions

| # | Owner said | Decision | Section |
|---|------------|----------|---------|
| 1 | Overwhelming; 5 buttons where 2 are needed; split into steps | Hard limits per screen (R1, R2); wizard for add product (R6); a remove / move / keep list for every control (§6) | §4, §6 |
| 2 | Driver's Recent activity is an unclear blob | One operation table (R3); the Driver sees only his own operations (finding J-F1) | §4, §5.3 |
| 3 | Receive screen is good | Kept. Only the quantity moves after the scan, and commit goes through the review sheet | §5.6 |
| 4 | "Find a product" unclear; needs a dedicated search entry | A **Search** tab with the magnifier icon; the Scanner page is merged into it | §5.5 |
| 5 | A bottom menu with a big central +/− and Home, List, one more | Bottom bar: Home · Products · [+ │ −] · Search · Activity; the drawer stays for secondary items | §3 |
| 6 | Products as a table or one format everywhere | One `ProductRow` (R3) used by the table, search, suggestions, Home, count and document lines | §4 |
| 7 | Think through who and why; daily use | Personas and journeys | [`journeys.md`](journeys.md) |
| 8 | Dialogs: not a bare "Done"; styled; essential fields; "Next" steps | One `AppSheet` (R5) | §4, §9 |
| 9 | Personas such as manager Marina at a delivery and a stock count | Marina, Petro, Ivan | journeys §2, J1, J6 |
| 10 | Fewer clicks, less overwhelming | Measured: J1 13 → 7, J5 13 → 5, J7 12 → 5 | journeys §4 |

**A reversal to note:** revision 2 (R2) had the owner ask for "no bottom tabs" and two big tiles on Home. This
request replaces both: the central split button takes over the tiles' job, so the tiles go (Q1 in §12).

## 3. Information architecture

### 3.1 Map

```
Sign in ─> ┌───────────────────────── root pages (bottom bar visible) ────────────────────────────┐
           │  Home        Products        [ + Receive │ − Issue ]        Search        Activity   │
           └──┬────────────┬───────────────────┬───────────┬────────────────┬────────────────┬──┘
              │            │                   │           │                │                │
              │            ├─ Product ◄────────┼───────────┼── Search result┤                │
              │            │   ⋮ Edit · Photo · Record with reason/location · Delete          │
              │            └─ + Add product (Manager) ─> Add-product wizard (3 steps)          │
              │                                ▼           ▼                                   │
              │                          Receive page   Issue page  ─> Review sheet ─> done ─> back to origin
              │                                │ unknown code ─> Add-product wizard ─> back to Receive
              └─ cards link to: Products (out of stock), Activity (today)          Activity row ─> Operation sheet
                                                                                     └─ Correct quantity (sheet)
 ☰ drawer (from the top bar of every root page): Stock count* · Characteristics* · Map · Settings ; account ; sync ; Sign out
                                                  * Manager only
```

### 3.2 Bottom bar

```
+------------------------------------------------------+
|  ⌂        ▤      ┌──────────┬──────────┐   ⌕     ≡⇅ |
| Home   Products  │ ＋ Receive│ − Issue  │ Search Activity
|                  └──────────┴──────────┘              |
+------------------------------------------------------+
      height 64 dp (bar) ; centre pill 56 dp high, 160–184 dp wide, filled Primary, RadiusL, no shadow
```

| Rule | Value |
|------|-------|
| Destinations | 4 places + 1 action (Material: 3–5 destinations; NN/g: more than 5 does not fit) |
| Labels | Always under the icon (`LABELED`); 12 sp; selected = `PrimaryText` and a `PrimarySoft` pill behind the icon |
| Centre | One pill split in two halves by a 1 dp `OnPrimary` 40 % divider. Each half is a separate 56 dp target. It is the only filled control on root pages |
| Driver | The pill is a single **＋ Receive** of the same size, with no "−" half and no empty space (Ivan never sees an action he cannot use) |
| Draft badge | A small count (for example "3") on the half whose draft has lines; it is text inside the pill, not a Shell badge (badges need MAUI 11) |
| Visibility | On the four root pages only. Task pages (Receive, Issue, Stock count, wizard, product detail) hide it and show ← plus their own bottom action |
| Keyboard | The bar stays **under** the keyboard (it is not in the `SoftInput` safe-area row); a task's bottom action stays above the keyboard as today |
| Reselect | Tapping the current tab scrolls to the top; on Search it also clears and focuses the field |
| Desktop (Windows, ≥ 840 epx) | The same bar centred, `MaximumWidthRequest` 720; a vertical rail is **not** planned (YAGNI; Windows is the secondary target) |

**Why Search and Products both?** They are different jobs:

- **Products** = browse and compare: the table, sort by any characteristic, all rows.
- **Search** = "this one product, now": a focused field, the camera, at most 5 results, and the product page in one
  tap.

The owner asked for a dedicated search entry (item 4). Merging it into Products is what made it unclear.

### 3.3 Drawer (kept, now secondary)

The owner asked for the drawer with a close cross in revision 2. NN/g shows hidden menus are the least discoverable,
which is exactly right for **rare** tasks. Keep it, but only with what is not in the bottom bar.

| Item | Roles | Note |
|------|-------|------|
| Header: initials, name, role, × | all | unchanged |
| Stock count | Manager | new (§8), only after the owner's approval |
| Characteristics | Manager | unchanged route |
| Operations map | all | unchanged |
| Settings | all | Logs and Diagnostics live inside Settings |
| Footer: sync chip, Sign out | all | unchanged |

Removed from the drawer: Home, Receive goods, Issue goods, Products (all are in the bottom bar).

### 3.4 Back behaviour

| From | System Back / ← does |
|------|-----------------------|
| Home | Leaves the app (Android standard) |
| Products, Search, Activity | Goes to **Home** (one level; tabs have no history among themselves) |
| Receive / Issue with lines | Back keeps the draft (it is persisted already) and shows the snackbar "Draft kept · 3 lines"; the badge on the pill says so too. No question asked |
| Receive / Issue after a commit | Automatic: the success banner shows for 1.5 s, then the page returns to where it was opened from (Home, Products, Search or the product page) |
| Wizard step 2 or 3 | Back goes to the previous step; on step 1 (or step 2 when step 1 was skipped) with input, the sheet "Discard the new product?" appears (existing dirty check) |
| Any sheet | Back, a tap on the scrim or a drag down closes it = Cancel; except the review sheet while saving |
| Product page | Back to the list, search or activity it came from |

## 4. Hard rules (checked on every screen in the final acceptance)

| # | Rule | Source |
|---|------|--------|
| R1 | **At most one filled (primary) button per screen.** On root pages it is the central pill; on task pages it is the bottom action. Two equal filled halves count as one control (the split pill; Receive / Issue on the product page) | Material FAB "primary action of a screen"; Hick's law |
| R2 | **At most two visible secondary actions** (outlined, ghost or icon) besides the primary and navigation. Anything else goes to the ⋮ overflow or is cut. Row-level actions (a stepper, remove) do not count, but a row has at most one action besides its tap | Hick; NN/g progressive disclosure |
| R3 | **One format for a product, one for an operation.** `ProductRow`: name (bold, max 2 lines, highlight), code caption, an "Out of stock" chip under the name, a right column (on hand, or a stepper, or a counted field), optional extra column. `OperationTable`: a header band "Qty · Product · By · Time", one 56 dp row per operation, the signed amount right-aligned and coloured (+ `Success`, − `Danger`), the product name, the document number as a caption, the user's initials chip, local time ("10:42", "Yesterday", "25 Nov"), grouped under day headers. Nothing else shows a product or an operation | Owner items 2, 6; revision 3 §4.1 table rules |
| R4 | **Progressive disclosure, at most two levels.** Optional fields (supplier, note, location, photo) are not on the main screen of a task: they sit in the review sheet or in a wizard step. A disclosure control is never a bare link: it is a row with a label, the current value and a chevron | NN/g progressive disclosure |
| R5 | **One sheet for every question.** `AppSheet`: a drag handle, a title that is the question ("Receive 42 units from…?"), an optional "Step 2 of 3", the fields the task needs (focused first field), and a footer with one filled action named after its result ("Receive 42 units", "Next: details", "Issue 9 units"), plus a ghost "Back" or "Cancel". **Never** "OK", "Done" or "Yes". A phone shows it as a bottom sheet; a wide window shows it centred, max 480 wide. No shadow: the scrim and a 1 px `Border` | Owner item 8; Material bottom sheet; NN/g modal dialogs; NN/g wizards (name the next step) |
| R6 | **A wizard for a rare multi-field task** (add product; the stock count review). 2–3 steps; each step fits one screen, has one primary action named after the next step, and keeps its state when you go back | NN/g wizards and staged disclosure |
| R7 | **One dedicated search** (the Search tab) for "find a product". The quick search inside Receive / Issue / Count stays, because it adds lines rather than finding | Owner item 4 |
| R8 | **Targets**: 48 dp minimum, 56 dp for the bottom action and the pill halves, 8 dp apart; the ⋮ menu is far from the primary | Fitts; revision 2 D3 |
| R9 | **Every task returns to where it started**, by itself after success and by ← otherwise | Journey J10 |
| R10 | **Every screen answers three questions without scrolling:** where am I (title, highlighted tab), what is the one thing to do (the primary), how do I get back (← or the tabs) | NN/g heuristics (revision 2 §1) |
| R11 | **The role decides what exists, not what is disabled.** A role never sees a control it cannot use (the Driver's pill, the Manager-only overflow items); the service still refuses (AGENTS §2a) | revision 2 D1 |
| R12 | **Numbers right, text left, local time, plural helpers** everywhere (revision 3 rules stay) | revision 3 §6 |

## 5. Wireframes

Legend: `[[ X ]]` filled primary; `[ X ]` outlined; `( X )` chip; `⋮` overflow; `←` back; `☰` drawer;
`▸` disclosure row; `═══` the bottom bar of §3.2 (abbreviated). Phone width (Pixel 7, 411 dp). The roles are noted
where a screen differs.

### 5.1 Sign in (unchanged from revision 3)

```
+--------------------------------------+
|              StockRoom               |
|  Login     [ petro               ]   |
|  Password  [ ••••••••          👁 ]   |
|  [[             Sign in            ]] |
|  [ Unlock with biometrics ]          |  <- only when enabled
+--------------------------------------+
```

### 5.2 Home

Manager (Marina) and Storekeeper (Petro):

```
+--------------------------------------+
| ☰        StockRoom                   |   <- no scan icon any more (Search tab)
| Good morning, Petro      (2 waiting) |   <- sync chip only when not synced
|                                      |
| Continue                             |   <- only when a draft exists
| ▸ Receive draft · 3 lines · 42 units |
|                                      |
| Out of stock · 2                  >  |   <- ProductRow format, max 3 rows, ">" opens Products filtered
|  Heinz Baked Beanz 415 g          0  |
|  5000157024671  (Out)                |
|  Fanta Orange 330 ml              0  |
|                                      |
| Today                             >  |   <- OperationTable, max 5 rows, ">" opens Activity
| QTY  PRODUCT                BY  TIME |
| +12  Nutella 400 g          PK 10:42 |
|      R-20261125-0003                 |
|  −3  Oreo Original          OK 10:15 |
|      I-20261125-0001                 |
|══════════════════════════════════════|
|  ⌂     ▤    [＋ Receive│− Issue]  ⌕  ≡ |
+--------------------------------------+
```

Driver (Ivan):

```
+--------------------------------------+
| ☰        StockRoom                   |
| Good evening, Ivan                   |
| My deliveries today               >  |   <- only Ivan's operations (J-F1)
| QTY  PRODUCT                    TIME |   <- no "By" column: it is always him
| +24  Coca-Cola Original 330 ml 08:31 |
| +48  Red Bull Energy Drink     08:30 |
|                                      |
| (empty: "Nothing delivered today.    |
|  Tap ＋ Receive to book a delivery.")|
|══════════════════════════════════════|
|  ⌂     ▤     [ ＋ Receive ]      ⌕  ≡ |
+--------------------------------------+
```

There are no tiles, no *Find a product*, no *Add product* and no greeting role line. Home is not a dashboard: no
numbers to interpret, only lists to act on (AGENTS §1: no dashboards).

### 5.3 Activity (new root page)

```
+--------------------------------------+
| ☰        Activity                    |
| (All) (In) (Out)   (Mine)            |   <- filter chips, one row; Driver: no "Mine" (always his)
| QTY  PRODUCT                BY  TIME |   <- sticky header band
| Today ────────────────────────────── |
| +12  Nutella 400 g          PK 10:42 |
|      R-20261125-0003                 |
|  −3  Oreo Original          OK 10:15 |
| Yesterday ────────────────────────── |
| ...                                  |
|══════════════════════════════════════|
+--------------------------------------+
```

Tapping a row opens the **Operation sheet**:

```
+--------------------------------------+
|               ───                    |
| +21  Nutella 400 g                   |
| Received by Petro · Today 10:42      |
| R-20261125-0003 · from Ferrero Dist. |   <- the counterparty, decrypted, shown only here
| [ Open product ]                     |   <- secondary
| [[      Correct quantity       ]]    |   <- hidden when the role cannot correct in either direction
+--------------------------------------+
```

**Correct quantity** (the same sheet, step 2):

```
| Correct Nutella 400 g (R-…0003)      |
| It should have been   [ 12 ]         |   <- numeric, focused, prefilled with 21
| → Issues 9 units. Note: "Correction  |
|   of R-20261125-0003"                |
| [ Back ]        [[ Issue 9 units ]]  |   <- the label follows the direction; disabled with a reason if not allowed
+--------------------------------------+
```

### 5.4 Products

```
+--------------------------------------+
| ☰        Products                 ＋ |   <- ＋ Add product (Manager only; otherwise an empty 48 slot)
| [⌕ Filter by name, code or value  ×] |
| NAME ▲                   QTY  WEIGHT▾|   <- the header sorts; "WEIGHT▾" picks the extra column (chip in the band)
| Barilla Ragu Bolognese      18   0.4 |
| 8076809513326                        |
| Heinz Baked Beanz 415 g      0 0.415 |
| 5000157024671 (Out)                  |
| ...                                  |
|══════════════════════════════════════|
+--------------------------------------+
```

- Row tap → the product page.
- Swipe left (Manager): Edit, Delete. On desktop these are the two row icons, as today.
- Removed: the round scan icon and the bottom *Add product* bar (two bottom bars on one root page break R1).

### 5.5 Search (new root page; replaces the Scanner page and *Find a product*)

```
+--------------------------------------+
| ☰        Search                      |
| +----------------------------------+ |
| |   camera preview 16:9 (scan)     | |   <- camera first; the "Camera not available" banner as today
| +----------------------------------+ |
| [⌕ Name or barcode               ×] |   <- focused on a reselect; Enter = exact code path
| Nutella 400 g                    36 |   <- ProductRow, max 5, highlight, "Similar name"
| 3017620422003                        |
| Nutella B-ready 22 g              0 |
| 8000500217078 (Out)                  |
|                                      |
| No match: "Not in the catalogue.     |
|  [ Create product ]" (Manager) /     |
|  "Ask the manager to add it." (other)|
|══════════════════════════════════════|
+--------------------------------------+
```

- A scan of a known code opens the product page at once.
- An unknown code shows the OFF lookup line ("Found in Open Food Facts: Nutella 400 g (ODbL)") and *Create product*
  for the Manager.
- While the field has focus on a phone, the camera card collapses (as on Receive).

### 5.6 Receive / Issue (kept; three changes)

```
+--------------------------------------+
| ←  Receive goods                  ⋮  |   <- ⋮ Discard draft
| +----------------------------------+ |
| |        camera preview            | |
| | [✓ Nutella 400 g  +1]            | |
| +----------------------------------+ |
| [⌕ Type a name or barcode        ×] |   <- the Qty box is REMOVED
| Nutella 400 g          [ − | 12 | + ]|   <- ProductRow + stepper; after a scan or pick the
| 3017620422003 · 24 → 36            ⊗ |      quantity is focused with its text selected: typing replaces it
| Oreo Original          [ − |  6 | + ]|
| 7622210449283 · 0 → 6              ⊗ |
|--------------------------------------|
| [[        Review 18 units         ]] |   <- opens the review sheet
+--------------------------------------+
```

1. **Scan, then type.** The quantity of the line just added (or increased) is focused with the text selected, on the
   numeric keyboard. The next scan or pick commits the typed value. A repeat scan of the same product still adds 1.
2. **No Qty box** and no *Supplier and note* link: both move into the review sheet.
3. **Bottom action = Review N units**, which opens the review sheet. After success the page returns to its origin
   (R9).

Review sheet (replaces the confirm sheet and the details expander):

```
+--------------------------------------+
|               ───                    |
| Receive 18 units                     |
| Nutella 400 g      12   24 → 36      |   <- lines, read-only, max 5 then "+ 2 more"
| Oreo Original       6    0 → 6       |
| From   (Ferrero Dist.) (Metro) (+)   |   <- last 3 counterparties as chips; (+) = type a new one
| ▸ Note                       none    |   <- disclosure row; opens a field in place
| [ Back ]     [[ Receive 18 units ]]  |
+--------------------------------------+
```

Issue is the same with *To* (recipient) and *Issue N units*. The over-stock rules stay (a red line, and the button
disabled with "Fix the red lines first").

### 5.7 Product page (two buttons)

```
+--------------------------------------+
| ←  Product                        ⋮  |   <- ⋮ (Manager): Edit details · Change photo · Delete
| +----------------------------------+ |      ⋮ (all):     Record with reason or location
| |      photo 16:9 / "No photo"     | |
| +----------------------------------+ |
| Danone Skyr 140 g                    |
| 3033490004743                        |
| 42 on hand   (In stock)              |
| Weight 0.14 kg · Brand Danone · Skyr |   <- one caption line; "Details ▸" opens all of them
| History                           >  |   <- OperationTable, last 5, ">" = Activity filtered to this product
| +60  ...                    IS 10:19 |
|--------------------------------------|
| [[ ＋ Receive ]]   [[ − Issue ]]      |   <- the 2 buttons; Driver: Receive full width
+--------------------------------------+
```

- **Edit details** opens the same page in edit mode: the characteristic fields, *Save changes* as the bottom action,
  and ← asking before it discards.
- **Change photo** opens a sheet: *Take photo* / *Pick photo* / *Remove photo* (only when one exists).
- *Open in maps* moves from each history row into the Operation sheet ("Show on map", when there is a location).

### 5.8 Add-product wizard (3 steps, a page with a step header)

```
Step 1 of 3 · Code                 Step 2 of 3 · Name & photo         Step 3 of 3 · Details
+------------------------------+   +------------------------------+   +------------------------------+
| ←  New product   ● ○ ○       |   | ←  New product   ● ● ○       |   | ←  New product   ● ● ●       |
| +--------------------------+ |   | Name                         |   | Only what we sort by:        |
| |  camera (scan the code)  | |   | [ Nutella 400 g           ]  |   | Weight (kg)  [ 0.4        ]  |
| +--------------------------+ |   | ✓ From Open Food Facts (ODbL)|   | Brand        [ Ferrero    ]  |
| Barcode [ 3017620422003   ]  |   | ⚠ Already in catalogue: …[Open]  | Category     [            ]  |
|                              |   | Photo  [ Add photo ]         |   | [ + New characteristic ]     |
|------------------------------|   |------------------------------|   |------------------------------|
| [[    Next: name & photo  ]] |   | [[      Next: details     ]] |   | [[      Add product       ]] |
+------------------------------+   +------------------------------+   +------------------------------+
```

- Step 1 is skipped when the code is already known: from an unknown Receive line, or from Search with an unknown code.
- *Next* is disabled with a reason ("Enter a valid 13-digit code", "Enter a name").
- Step 3 is optional (all fields may stay empty).
- After *Add product*, the wizard returns to its origin. From Receive, the unknown line becomes the new product with
  its quantity focused.

### 5.9 Stock count (Manager; only after the owner's approval, §8)

```
+--------------------------------------+     Review sheet
| ←  Stock count                    ⋮  |     +--------------------------------------+
| 7 of 10 counted                      |     | Post stock count corrections         |
| +----------------------------------+ |     | Nutella 400 g     36 → 38    +2      |
| |   camera                         | |     | Oreo Original      6 → 5     −1      |
| +----------------------------------+ |     | Skyr 140 g        42 → 38    −4      |
| [⌕ Type a name or barcode        ×] |     | 7 products match.                    |
| Nutella 400 g        36  [  38 ]  +2 |     | Note  "Stock count 25 Nov 2026"      |
| Oreo Original         6  [   5 ]  −1 |     | [ Back ]  [[ Post 3 corrections ]]   |
| Barilla Ragu         18  [  18 ]   ✓ |     +--------------------------------------+
| Fanta Orange         84  [     ]  —  |   <- not counted yet
|--------------------------------------|
| [[    Review 3 differences      ]]   |
+--------------------------------------+
```

The ⋮ menu has *Discard count*. Uncounted products are **not** touched (they are not counted as 0): the sheet says
"3 products not counted: unchanged".

### 5.10 Characteristics (Manager)

```
+--------------------------------------+     Sheet "New characteristic"
| ←  Characteristics                   |     | Name   [ Volume (l)              ]  |
| Weight (kg)                   Number |     | Type   (Text) (●Number)             |
| Brand                           Text |     | [ Cancel ] [[ Add characteristic ]] |
| ...                                  |
|--------------------------------------|
| [[      + New characteristic     ]]  |
+--------------------------------------+
```

### 5.11 Drawer

```
Manager                                  Driver
+---------------------------+            +---------------------------+
| (OK) Olena Kovalenko    × |            | (IS) Ivan Shevchenko    × |
|      Manager              |            |      Driver               |
| ☑ Stock count             |            | ⌖ Operations map          |
| ◇ Characteristics         |            | ⚙ Settings                |
| ⌖ Operations map          |            |                           |
| ⚙ Settings                |            | (Online · all synced)     |
| (Online · all synced)     |            | [ Sign out ]              |
| [ Sign out ]              |            +---------------------------+
+---------------------------+
```

### 5.12 Settings (reordered; the content is unchanged)

Sections, in order: *Appearance* (theme), *Scanning* (scan sound, shake → opens **Search**), *Security*
(biometrics), *Sync* (state, *Sync now*; *Simulate offline* moves to Diagnostics). Then a collapsed
**▸ Diagnostics (Manager)**, which holds: *Open logs*, *Write test log entries*, *Throw in background task*, *Send
test crash*, *Simulate offline*, *Share database*, and *Open a link* (deep-link test). The Lab 5 / 6 / 7 requirements
stay reachable in two taps.

### 5.13 Operations map

Unchanged, except that *Open in maps* is the row tap (one fewer button per row).

## 6. Every existing control: remove / move / keep

| Screen | Control (AutomationId or text) | Verdict |
|--------|--------------------------------|---------|
| Shell | `FlyoutItem`s Home, Products, Settings | **Keep** as routes (`//home`, `//products`, `//settings`); `FlyoutItemIsVisible=false` for Home / Products; **add** `//search`, `//activity` |
| Drawer | Home, Receive goods, Issue goods, Products items | **Remove** (they are in the bottom bar) |
| Drawer | Map, Characteristics, Settings items; header ×; SyncChip; Sign out | **Keep** |
| Drawer | Stock count | **Add** (Manager, after approval) |
| Home | `HomeScanButton` (app bar scan) | **Remove** → the Search tab |
| Home | Greeting; RoleText | Keep the greeting; **remove** the role line (it is in the drawer header) |
| Home | SyncChip | Keep |
| Home | `HomeReceiveTile`, `HomeIssueTile`, `HomeIssueNotAllowedLabel` | **Remove** → the central pill; the draft caption moves to the "Continue" row and the pill badge |
| Home | `HomeFindProductButton` | **Remove** → the Search tab |
| Home | `HomeAddProductButton` | **Move** → Products app-bar ＋ (Manager) |
| Home | `RecentActivityPanel` / `ActivityRow` | **Replace** with `OperationTable` (Today, max 5); the Driver sees his own only |
| Home | — | **Add** "Continue" draft row and the "Out of stock" list (ProductRow) |
| Products | `ProductsScanButton`-style app-bar scan | **Remove** → Search |
| Products | Filter field, sort headers, `ExtraColumnPicker` | Keep; the picker becomes a ▾ in the header band |
| Products | Row tap, swipe Edit / Delete, desktop `EditProductButton` / `DeleteProductButton` | Keep |
| Products | "Similar name", "Out of stock" chip, EmptyState, StatusLabel, pull to refresh | Keep |
| Products | Bottom bar *Add product* | **Move** → app-bar ＋ (Manager) |
| Scanner page | Whole page (`AddThisProductButton`, `CreateFromLookupButton`, `ScanAgainButton`, *Find product*, barcode field) | **Remove**; merged into Search (*Create product* on a no-match; re-scanning is continuous) |
| Receive / Issue | ← , ⋮ *Discard draft*, summary line, camera, `CameraStatusBanner`, quick search, success banner, EmptyState, lines with stepper, `RemoveLineButton`, *Create product* on an unknown line, Undo snackbar | Keep |
| Receive / Issue | Qty box (`EntryQuantity`) | **Remove** (scan, then type) |
| Receive / Issue | `DetailsButton` (*Supplier and note*), `CounterpartyEntry`, `NoteEntry` | **Move** → the review sheet |
| Receive / Issue | `CommitDocumentButton` "Receive N units" | **Change** → "Review N units" (opens the sheet); the sheet holds the final action |
| Confirm sheet | `ConfirmSheet`, `ConfirmAcceptButton`, `ConfirmCancelButton` | **Replace** with `AppSheet` (same ids kept for the accept / cancel buttons) |
| Product page | `EditDetailButton` (app bar) | **Move** → ⋮ *Edit details* |
| Product page | `PhotoAreaButton`, `AddPhotoButton`, `TakePhotoButton`, `PickPhotoButton`, `RemovePhotoButton` | **Move** → ⋮ *Change photo* → photo sheet (3 actions); a tap on the photo area opens the same sheet (Manager) |
| Product page | `DetailReceiveButton`, `DetailIssueButton` | Keep (the 2 buttons) |
| Product page | `RecordOperationButton` (ghost "Record one operation…") | **Move** → ⋮ *Record with reason or location* (OperationPage stays: Lab 7 location) |
| Product page | History `ActivityRow` + `OpenInMapsButton` per row | **Replace** with `OperationTable` (last 5); *Show on map* moves into the Operation sheet |
| Product page | Characteristics card (view / edit) | Keep; the view becomes one caption line plus *Details ▸* |
| Product page | `SaveCharacteristicsButton` | Keep (edit mode only) |
| Product form | Whole page | **Replace** with the 3-step wizard; `ScanBarcodeButton` goes (the camera starts on step 1); the photo buttons become *Add photo* → photo sheet; `CancelProductButton` goes (← plus the dirty check) |
| Characteristics | List; `NewCharacteristicEntry`, `TextTypeButton`, `NumberTypeButton`, *Add characteristic* | Keep the list; **move** the form into the "New characteristic" sheet; the bottom action opens it |
| OperationPage | Direction, units, counterparty, reason, `AttachLocationButton`, `ClearLocationButton`, Save, Cancel | Keep (Lab 7), reached from the product ⋮ only; Cancel goes (← exists) |
| Settings | All switches and buttons | Keep, reordered (§5.12); diagnostics collapsed |
| Map | Map, pins list, `OpenInMapsButton` | Keep; the row tap = open in maps |
| Login | All | Keep |
| Logs | All | Keep (from Settings → Diagnostics) |

Net effect on the busiest screens:

| Screen | Visible actions now | Proposed |
|--------|--------------------:|---------:|
| Home (Manager) | 6 (scan, 2 tiles, find, add, ☰) | 1 pill + 4 tabs + ☰ (navigation only) |
| Product page (Manager) | 8 + 1 per history row | 2 + ⋮ |
| New product | 6 | 1 primary + ← per step |
| Receive (empty) | Qty, details, commit, ⋮, search | search, commit, ⋮ |

## 7. Correcting a mistake: domain fit

- No schema change. `Operation` stays immutable.
- *Correct quantity* computes `delta = right − booked` for **one operation**:
  - `delta < 0` → an Out of |delta|;
  - `delta > 0` → an In of delta;
  - `0` → nothing.
- It is recorded as a one-line `StockDocument` with the Note "Correction of {Number}" (or of the operation's date for
  a single operation without a document), through the existing `StockDocumentService` (one transaction, sync
  queue, logging).
- Roles come from `RolePermissions.CanRecord(role, direction)`:
  - the Driver can correct **upwards** only;
  - the sheet explains the refusal, and the service refuses too (`Warning` log).
- Out ≤ on hand is still checked. For example, booked 21, already issued 15, so on hand is 6; "should be 12" needs an
  Out of 9 > 6. The sheet then shows "Only 6 on hand. Ask the manager to count Nutella."
- New Core pieces:
  - `Services/OperationCorrection.cs`, pure: given the operation and the right quantity → direction, quantity, note,
    or a refusal reason;
  - `RolePermissions.CanCorrect(role, direction)`, an alias of `CanRecord`, to keep the rule in one place.
- **Later, needing a written decision:** a `CorrectsDocumentId` column to link the correction to the original
  (migration). Not now: the note carries the link for people, and the history stays complete.

## 8. Stock count: decision needed from the owner

**It is a new feature.** AGENTS §1 lists no stock count, and "anything outside scope needs a written decision". It is
planned so that it needs **no new table**, and it is built only after the owner says yes (slice V10 is blocked).

Minimal version, consistent with the domain:

1. **Who:** the Manager (`RolePermissions.CanCountStock`). The Storekeeper could count while the Manager posts, but
   that is a two-person workflow: later.
2. **Count = a local draft**, like `StockDocumentDraft`: `StockCountDraft` (product id → counted quantity, started
   at), persisted with `IAppStateService` so a restart keeps it. It is **not** synced and **not** an entity.
3. **Not blind** (the counter sees "on hand"): with about ten products, anchoring bias matters less than speed. A
   blind mode is a later option (the Cleverence guide: blind counts reduce bias but need tighter guidance).
4. **Post** computes, at post time and for each counted product, `delta = counted − onHand(now)`:
   - the positive deltas → one **In** `StockDocument`;
   - the negative deltas → one **Out** `StockDocument`;
   - both get the Note "Stock count {date}";
   - both are committed **in one transaction** (a new `StockDocumentService.CommitManyAsync`, or a
     `StockCountService` that uses the same repository transaction).
   Out never exceeds on hand, because counted ≥ 0.
5. **Changes during the count:** if on hand changed for a counted product since its count (an operation in between),
   the review sheet marks that row "Changed since you counted: recount" and keeps it out of the post until it is
   recounted. This is the cheap form of the "recount above a tolerance" rule.
6. **Uncounted products are untouched.**
7. **Logging:** `Information` "Stock count posted: {In} in, {Out} out, documents {NumberIn} {NumberOut}"; never the
   quantities per person.

What the owner must decide (Q3, §12): yes / no; Manager only or also the Storekeeper; blind or not.

## 9. Technical feasibility (MAUI 10, CommunityToolkit.Maui 16.0.0)

### 9.1 Bottom bar: our own control, not Shell tabs

| Option | Facts (sources in journeys §6) | Verdict |
|--------|--------------------------------|---------|
| Shell `TabBar` | "The TabBar type disables the flyout" (MAUI tabs doc) | **No**: the drawer must stay |
| `FlyoutItem` with several `ShellContent`s (native tabs + flyout) | Works with a flyout. Styling is colour properties only (`TabBarBackgroundColor`, `TabBarTitleColor`, …); a centre split button is not possible without a custom handler / renderer per platform; more than 5 tabs adds *More*; badges need MAUI 11 | **No**: the split centre button is the owner's key ask |
| **Own `BottomNav` ContentView** in a shared root-page `ControlTemplate` | We already hide the Shell nav bar and draw our own `AppBar` (revision 3 S2); the same pattern at the bottom. Shell stays the router (absolute routes `//home`, `//products`, `//search`, `//activity`); the flyout keeps working | **Yes** |

Design of the own bar:

- **Core:**
  - `ViewModels/BottomNavViewModel.cs`: items from `NavigationMenu.BottomFor(role)`, `Selected` from the current
    route (`INavigationService` raises `Navigated`, already used by `AppShell.OnNavigated` → `DrawerViewModel.Select`);
  - `CanIssue` from `RolePermissions`;
  - draft counts from `IStockDocumentDraftStore.Changed`;
  - commands `Open(route)`, `Receive`, `Issue`.
- **App:**
  - `Views/Components/BottomNav.xaml` (5-column grid; the pill is a `Border` with two `Button`s as `RowOverlay`, ids
    `NavHome`, `NavProducts`, `NavReceive`, `NavIssue`, `NavSearch`, `NavActivity`);
  - `Resources/Styles/RootPage.xaml`: a `ControlTemplate` with rows `AppBar | ContentPresenter | BottomNav`, applied
    to the four root pages (DRY).
- **Task pages** are pushed on the current root (`receive` relative to `//products`, etc.), so ← returns to the
  origin (R9).
  - The routes today are `//home/receive`. They become relative `Routing.RegisterRoute("receive", …)` pushes; Shell
    already supports relative routes.
  - "Return to origin after success" is `GoToAsync("..")`.
- **Risk:**
  - the keyboard pushing the bar up on Android when a root page has a focused field (Search, the Products filter);
  - fix: the bar is outside the `SoftInput` safe-area row, or hidden while the keyboard is up (`IKeyboard.IsVisible`,
    if needed) **[to verify on the emulator]**.
- **Windows:** the same bar; the flyout works as today.

### 9.2 One sheet: `AppSheet` on CommunityToolkit `Popup`

- **Core contract:**
  - `Services/ISheetService.cs`:
    - `Task<TResult?> ShowAsync<TViewModel, TResult>(Action<TViewModel>? init)`;
    - `Task<bool> ConfirmAsync(ConfirmRequest)`, keeping the existing `IConfirmSheet` callers.
  - `ViewModels/Sheets/*`: `DocumentReviewViewModel`, `OperationSheetViewModel`, `CorrectionViewModel`,
    `NewCharacteristicViewModel`, `PhotoSheetViewModel`, `CountReviewViewModel`.
  - All are testable without MAUI.
- **App:**
  - `Services/ToolkitSheetService.cs` uses `IPopupService` / `Popup<T>` (Toolkit 16.0.0 is already referenced);
  - `Views/Sheets/AppSheet.xaml` = handle, title, step text, `ContentPresenter`, footer (`SheetPrimaryButton`,
    `SheetSecondaryButton`).
  - The defaults are set once in `MauiProgram.UseMauiCommunityToolkit(o => o.SetPopupDefaults(...);
    o.SetPopupOptionsDefaults(...))`:
    - `BackgroundColor` / `BackgroundColorDark` = the `Surface` tokens of the two themes;
    - `Shadow = null` (minimal shadows);
    - `PageOverlayColor` = the `Overlay` token;
    - `Shape` = `RoundRectangle` 16 on the top corners;
    - `VerticalOptions = End` on a phone, `Center` on a wide window.
- **Facts from the Toolkit doc that shape the design:**
  1. A popup is hosted on a **modal page**, so the page below gets `OnDisappearing` / `OnAppearing`. Our
     `IPageLifecycleAware` stops the camera there.
     - So **the quantity is never asked in a popup during scanning** (it is inline, §5.6). The review sheet is fine
       (scanning is over).
     - The lifecycle forwarder must not reload the page on return: use `WasPreviousPageACommunityToolkitPopupPage()`
       in `PageLifecycleBehavior` to skip `OnAppearing` refreshes that are not needed **[to verify]**.
  2. Back and a tap outside dismiss only while `CanBeDismissedByTappingOutsideOfPopup` is true. The review sheet sets
     it to false while saving.
  3. A navigation removes the popup and completes the task as dismissed. This is safe for "return to origin".
  4. A popup margin of 0 on every side is replaced by the default margin, so a full-width bottom sheet needs the
     default margin set to 0 too **[to verify]**.
- **Risk:** the soft keyboard over a bottom popup with a field (Correct quantity, Note) on Android **[to verify]**.
- **Fallback:** the current `ConfirmSheetPage` technique (a transparent modal `ContentPage`, already working on the
  emulator) generalised to `SheetPage` with a `ContentPresenter`. Slice V2 starts with a 1-hour spike and picks one.
  The contract does not change.

### 9.3 Other pieces

| Need | How | Risk |
|------|-----|------|
| Focus the quantity after a scan, text selected | `Entry.Focus()` + `CursorPosition = 0`, `SelectionLength = Text.Length` through a `FocusRequest` property on the line (behaviour in the app, flag in the ViewModel) | Android selection on focus **[to verify]** |
| Recent counterparties (chips) | `IStockDocumentRepository.GetRecentCounterpartiesAsync(direction, 3)`, decrypted in the repository as today; never logged | Personal data shown only to roles that record in that direction |
| Driver sees only his own operations | `IOperationQuery.GetRecentAsync(count, createdBy?)`; `ActivityViewModel` passes the current user id for the Driver; the service enforces it too | — |
| `OperationTable` | One `ContentView` + `OperationRowItem` (Core), used by Home, Activity and the product page; a sticky header via `CollectionView.Header`; day groups via `IsGrouped` | Grouped `CollectionView` on Windows **[to verify]** |
| Wizard | One `ProductWizardPage` + `ProductWizardViewModel` (Core) with `Step` 1..3, reusing `ProductFormViewModel`'s validation and OFF lookup (refactor, own commit) | — |

## 10. Implementation slices (in order)

Each slice follows AGENTS §2b:

- contract first;
- tests with the slice;
- `dotnet test Mobile_apps_L1_Model_view.Tests` green;
- commit `[Design] …`;
- both builds before the last UI commit;
- emulator screenshots of the touched screens into `docs/design/screenshots/r4/`, opened and looked at.

| # | Slice | Main files | Tests | Acceptance |
|---|-------|-----------|-------|------------|
| V0 | Core navigation model | `Services/Routes.cs` (+`Search`, `Activity`, `StockCount`, relative task routes), `Services/NavigationMenu.cs` (`BottomFor(role)`, `DrawerFor(role)` without Home / Receive / Issue / Products), `RolePermissions` (+`CanCountStock`, `CanCorrect`), `ViewModels/BottomNavViewModel.cs` | `NavigationMenuTests` per role, `BottomNavViewModelTests` (selection follows the route; the Driver has no Issue; the draft badge follows the store), `RolePermissionsMatrixTests` | No MAUI in Core; coverage ≥ 60 % |
| V1 | Bottom bar and roots | `Views/Components/BottomNav.xaml(.cs)`, `Resources/Styles/RootPage.xaml`, `AppShell.xaml.cs` (roots home / products / search / activity, relative task routes), `DrawerContent` items, remove the app-bar scan on Home / Products, `StockDocumentViewModel` returns to origin (`..`) after success | `StockDocumentViewModelTests` (after a commit → `GoToAsync("..")` after the banner), `HomeViewModelTests` updated, UiTests ids | Root ↔ root in **1 tap**; Receive from any root in **1 tap**; after a commit back to the origin in **0 taps**; Back on a root → Home |
| V2 | `AppSheet` + review sheet | spike (Toolkit Popup vs `SheetPage`), `Services/ISheetService.cs`, `ViewModels/Sheets/DocumentReviewViewModel.cs`, `Views/Sheets/AppSheet.xaml`, `DocumentReviewSheet.xaml`; `IConfirmSheet` re-implemented on it; recent counterparties query | `DocumentReviewViewModelTests` (lines, chips, a new name, commit once, saving blocks dismiss), repository test for recent counterparties (temp SQLite) | No bare "OK / Done" anywhere (grep the XAML for `Text="OK"`, `"Done"`, `"Yes"` → 0); the sheet in both themes, no shadow |
| V3 | `OperationTable`, Activity, Home | `ViewModels/OperationRowItem.cs`, `ActivityViewModel.cs`, `HomeViewModel` (Continue, Out of stock, Today), `Views/Components/OperationTable.xaml`, `ActivityPage.xaml`, `HomePage.xaml` rewritten; operation query with `createdBy` | `ActivityViewModelTests` (filters; **Driver gets only his own**, also when he asks for All), `OperationRowItemTests` (sign, colour flag, local time "Today 10:42"), `HomeViewModelTests`; EndToEnd: *Driver receives 2 → Home shows only his 2 rows* | Home and Activity rows are the same template; Home fits a Pixel 7 without scrolling with 2 drafts + 3 out of stock + 5 rows |
| V4 | One `ProductRow` | `Views/Components/ProductRow.xaml` (right slot), used by `ProductsPage`, `QuickSearchBox`, Home out-of-stock, document lines, count lines | `ProductListViewModelTests` unchanged and green; AppTests resource keys | Screenshot diff: the same name / code / chip geometry in all 5 places |
| V5 | Search tab | `ViewModels/SearchViewModel.cs` (camera state, `QuickSearchViewModel`, OFF lookup for unknown codes, open product), `Views/SearchPage.xaml`; delete `ScannerPage` + `ScannerViewModel` (refactor commit; move its tests); shake → `//search` | `SearchViewModelTests` (known code → product page; unknown → lookup + Create for the Manager only + refused for others with a `Warning`; reselect clears and focuses) | Find by name in **≤ 2 taps**, by code in **1 tap + scan** |
| V6 | Receive / Issue: scan then type | `StockDocumentViewModel` (remove `EntryQuantity`; `FocusedLine` after add; *Review* opens the sheet), `StockDocumentPage.xaml` (no Qty box, no details expander), a focus-and-select behaviour | `StockDocumentViewModelTests` (scan → focused line; typing replaces; a repeat scan adds 1; review → commit once); EndToEnd: *log in → + → scan 3 → type → Review → Receive → Products shows the new stock* | J1 **≤ 7**, J2 **≤ 6**, J3 **≤ 5** actions (counted on the emulator, §11) |
| V7 | Product page with 2 buttons | `ProductDetailPage.xaml`, `ProductDetailViewModel` (overflow items by role), `PhotoSheetViewModel`, `OperationSheetViewModel` | `ProductDetailViewModelTests` (a non-Manager overflow has only *Record with reason or location*; the photo sheet commands; the service still refuses) | ≤ 2 visible actions + ⋮ (R1, R2) |
| V8 | Add-product wizard | `ProductWizardViewModel` (Core), `ProductWizardPage.xaml`; `ProductFormViewModel` validation / lookup extracted (refactor commit); returns the created product to Receive | `ProductWizardViewModelTests` (step 1 skipped with a known code; Next disabled with a reason; OFF fills the name and the matching characteristics; Back keeps state; the dirty check; Manager only); EndToEnd: *unknown scan → wizard → back in Receive with the line* | J5 **≤ 5** actions |
| V9 | Correct quantity | `Services/OperationCorrection.cs`, `CorrectionViewModel`, the Operation sheet step 2 | `OperationCorrectionTests` (±, 0, Driver Out refused, over on hand refused, the note text), `CorrectionViewModelTests`; EndToEnd: *21 booked → correct to 12 → on hand −9, two operations, the first unchanged* | J7 **≤ 5** actions |
| V10 | Stock count (**blocked until Q3 = yes**) | `Services/StockCountDraft.cs`, `StockCountService.cs` (two documents, one transaction), `StockCountViewModel`, `CountReviewViewModel`, `StockCountPage.xaml` | Draft persistence, delta maths, "changed since counted", the transaction rolls back both documents on failure (temp SQLite), Manager only | J6 **≤ 14** actions for 10 products / 3 differences |
| V11 | Characteristics and Settings | `NewCharacteristicViewModel` sheet; `CharacteristicsPage.xaml` (list + 1 button); `SettingsPage.xaml` order and the collapsed Diagnostics | `CharacteristicsViewModelTests` through the sheet VM | R1 / R2 hold |
| V12 | Docs and the final check | `docs/steps/13-design-revision-4.md`, README step log + decision log (§13), `design-system.md` (R1–R12, the bar and sheet tokens), `docs/domain/domain-model.md` (correction and count wording), AppTests theme keys, UiTests ids | All green; both builds | §11 passes |

## 11. Final acceptance

### 11.1 Tap budgets (counted on the emulator with the helper script, the counting rules of journeys §1)

| Journey | Max actions |
|---------|------------:|
| J1 Delivery, 3 products + known supplier | 7 |
| J2 Issue, 2 products + known recipient | 6 |
| J3 Driver drop-off, 2 products | 5 |
| J4 Find by name / by code | 2 / 1 |
| J5 Unknown product during Receive (OFF name found) | 5 |
| J6 Stock count, 10 / 3 differ (if approved) | 14 |
| J7 Correct a wrong quantity | 5 |
| Any root → any root | 1 |
| Any task → back to its origin after success | 0 |

### 11.2 Screen rules

R1–R12 on every screenshot. For each screen, the step document lists the number of filled buttons (≤ 1) and the
visible secondary actions (≤ 2).

### 11.3 Emulator screenshots (r4)

Taken with `scripts/emulator-screens.ps1` (revision 3 §8), into `docs/design/screenshots/r4/`, both themes where
marked:

- Home: Manager, Storekeeper, Driver (light + dark);
- Activity;
- Operation sheet and correction;
- Products (light + dark);
- Search: camera, typing, no match;
- Receive: a focused line after a scan, the review sheet (light + dark), returned to the origin after success;
- Issue over-stock;
- Product page: Manager and Driver, ⋮ open, photo sheet;
- Wizard: steps 1, 2 and 3;
- Characteristics and its sheet;
- Drawer: Manager and Driver;
- Settings with Diagnostics open;
- Stock count and its review (if approved).

Each one is opened and looked at. The step document compares it with r2 / r3.

### 11.4 Regression and gates

- No `FATAL EXCEPTION` with `-camera-back none` (revision 3 S1), including on the Search tab.
- `dotnet test` green; ViewModel coverage ≥ 60 %.
- Windows and Android builds.

## 12. Questions that need the owner

| # | Question | Proposal |
|---|----------|----------|
| Q1 | The Home tiles go, because the central **+ │ −** pill does their job. Agreed? (It reverses revision 2 R3) | Yes: one place for in / out, always visible |
| Q2 | Is the 5th slot **Activity** (all operations) right, or would you rather have something else there? | Activity: it is where mistakes are found and fixed, and it is the Driver's proof of delivery |
| Q3 | **Stock count** (new feature, §8): build it? Manager only? Show on hand while counting (not blind)? | Yes; Manager only; not blind |
| Q4 | May the review sheet suggest the **last 3 suppliers / recipients** as chips? They are personal data, encrypted at rest and shown only to roles that record that direction | Yes |
| Q5 | Should a correction later be **linked** to the original document (a new column, a migration), or is the note "Correction of R-…" enough? | The note now; the link only if reports need it |

## 13. Proposed decision-log rows (README, written in V12)

| # | Decision | Why |
|---|----------|-----|
| 110 | Own bottom bar (Home · Products · [+ Receive │ − Issue] · Search · Activity) in a root-page template; Shell stays the router; the drawer keeps the rare items | Owner request; Shell `TabBar` disables the flyout and native tabs cannot draw a split centre button |
| 111 | Screen rules R1–R12: ≤ 1 filled button, ≤ 2 visible secondary actions, one product row, one operation table, one sheet, wizards for rare multi-field tasks | Owner: "overwhelming"; Hick's law; NN/g progressive disclosure |
| 112 | `AppSheet` on CommunityToolkit `Popup` behind `ISheetService`; never "OK / Done"; the primary is named after its result | Owner: styled dialogs with the needed fields and "Next" steps |
| 113 | Scan, then type the quantity; the Qty box goes; supplier / note move to the review sheet | Fewer taps (J1 13 → 7); people name the product before the count |
| 114 | Search tab replaces the Scanner page and *Find a product* | Owner: a dedicated search entry; one place instead of three |
| 115 | Correct quantity from an operation = a one-line opposite document with the note "Correction of …", same roles as recording | `Operation` is immutable; no schema change |
| 116 | The Driver's activity lists only his own operations, enforced in the query | entities.md "view own" |
| 117 | Stock count as a local draft posted as up to two documents in one transaction (if Q3 = yes) | Domain unchanged; minimal; the owner's written decision |
