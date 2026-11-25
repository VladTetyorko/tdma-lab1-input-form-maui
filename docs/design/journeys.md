# User journeys: who uses StockRoom, what for, and how many taps it takes

Status: **analysis for design revision 4** ([`revision-4.md`](revision-4.md)). It starts from the people, not the
screens. The current state is the `design` branch after revision 3 (commit `6bfb4ed`): the code in `Views/` and
`StockRoom.Core/ViewModels/`, and the screenshots in [`screenshots/r2/`](screenshots/r2). Revision 3 changed the
look but not the paths. Where a path differs from an r2 screenshot, the code wins.

## 0. Owner feedback that started this (translated)

1. "Everything is overwhelming." The product page and the add-product page show about five buttons where two are
   needed. Split the work into steps.
2. Recent activity on the Driver's Home is "not a table, an ugly unclear blob of text".
3. The Receive screen is fine, "rather good".
4. "Find a product" is unclear. Search needs its own dedicated icon and entry.
5. There is no bottom menu, and there must be one. Move part of the drawer into it: a big central (maybe split)
   **+ / −** button for receive / issue, plus Home, the list and perhaps one more.
6. Wherever products appear, show them as a table, or at least in one consistent format.
7. Learn who the user is, what they do, why, where they tap during the day and how they get back. The app must work
   for daily use. Today it is a skeleton.
8. Dialogs must not be a bare "Done". They should be styled popups that look like the app, hold the fields the
   journey needs, and offer "Next" steps.
9. Think from the user's side first. Invent personas and their use cases, e.g. manager Marina when a delivery
   arrives and during a stock count.
10. Fewer taps, less on screen. Nobody should get lost in the amount of stuff.

## 1. Method and counting rules

The method follows a journey map ([NN/g, journey mapping](https://www.nngroup.com/articles/customer-journey-mapping/)):
an **actor** (a persona), a **scenario**, the **phases**, what the person does and thinks in each phase, and the
**opportunities**.

Tap counting, the same for "now" and "proposed":

| Counted as 1 action | Not counted |
|---------------------|-------------|
| A tap on a control (button, row, field, chip, tab) | Pointing the camera at a barcode (listed as *scan*) |
| Typing one value and its keyboard Enter (a number, a name) | Reading the screen |
| A system Back press | Waiting for the success banner |

- **Start:** the app is open on Home and the user is signed in. The session lasts 30 days, so this is the daily case.
- **"Now"** is counted from the real XAML and ViewModels on `design` (`HomePage.xaml`, `StockDocumentPage.xaml`,
  `StockDocumentViewModel`, `ProductDetailPage.xaml`, `ProductFormPage.xaml`, `ProductsPage.xaml`,
  `ScannerPage.xaml`, `CharacteristicsPage.xaml`, `DrawerContent.xaml`).
- **"Proposed"** is counted from the wireframes in `revision-4.md` §5.

Facts from the code that the counts rest on:

- On Receive / Issue, the **Qty box must be set before the scan**. It resets to 1 after every add
  (`StockDocumentViewModel.EntryQuantity`). To scan 12 units you tap Qty, type 12, then scan. Scanning first and
  correcting the line also takes a tap on the line's quantity entry plus typing.
- The supplier / recipient and note sit behind a ghost button, "Supplier and note".
- *Receive N units* opens a confirm sheet (`ConfirmSheetPage`), which needs one more tap.
- After a commit the page **stays** on Receive with an empty draft. Getting back needs Back.
- Home's Recent activity (`RecentActivityViewModel`) shows the last operations of **all** users, also to the Driver.
  [`entities.md`](../domain/entities.md) says the Driver may "view own" operations. See §5 finding J-F1.
- There is no stock count and no "correct this" action. A mistake is fixed by hand with a document in the opposite
  direction, which the domain requires (`Operation` is immutable, entities.md).

## 2. Personas (one per existing role)

The demo accounts stay as they are (`olena` / `petro` / `ivan`, README "Demo accounts"). The personas give each role a
face for design decisions. Marina is the owner's own example of a manager. She signs in with the Manager account.

### Marina, Manager (signs in as `olena`)

- **Who:** 41, runs the small warehouse and owns the catalogue. Uses the app on her phone between calls, sometimes on
  the Windows PC in the office.
- **Day:** checks the morning's deliveries, adds a new product a supplier brought, fixes the odd mistake, and once a
  month counts the shelves.
- **Wants:** to trust the numbers; to see at a glance what went in and out today and what has run out; to finish
  rare tasks (a new product, a stock count) without remembering how they work.
- **Frustrations:** screens that show every option at once; having to work out a difference by hand; not knowing
  whether a correction went through.
- **Jobs to be done:**
  - "When a supplier brings something new, I want to add it in one go (code, name, the two or three facts we sort by)
    so the delivery can be booked straight away."
  - "When I count the shelves, I want to type what I see and let the app book the differences, so the stock is right
    again."
  - "When someone booked the wrong quantity, I want to fix it from the entry itself, so I do not have to rebuild it."

### Petro, Storekeeper (signs in as `petro`)

- **Who:** 29, at the counter all day, often in gloves, usually with someone waiting. Opens the app 40 to 60 times a
  day.
- **Day:** receives deliveries (several products, cases of 6 to 24), hands goods out to drivers and colleagues,
  answers "do we have X?".
- **Wants:** one obvious place for *in* and one for *out*; scan, type the number, done; to hear or see that it
  worked; to get back to where he was without thinking.
- **Frustrations:** an extra confirmation every time; looking for the quantity box; pages that stay open after the
  job is done; dialogs that ask him things he has already said.
- **Jobs to be done:**
  - "When a delivery arrives, I want to book all its lines with their counts in one pass, so the driver can leave."
  - "When someone asks for goods, I want to issue them and see the new stock, so I know whether to reorder."
  - "When someone asks 'do we have X?', I want the answer in two taps."

### Ivan, Driver (signs in as `ivan`)

- **Who:** 35, delivers to this warehouse two or three times a week. Uses the app only at drop-off, standing at the
  door, often with one hand.
- **Day:** unloads, books what he brought (**In only**, `RolePermissions.CanRecord`), leaves.
- **Wants:** a single big "I'm delivering" action; to see **his own** deliveries of today, as a clear list, as
  proof; nothing he cannot use.
- **Frustrations:** other people's operations mixed into "his" list (owner item 2); buttons he is not allowed to
  press; text he has to read.
- **Jobs to be done:**
  - "When I drop off goods, I want to book them quickly so I can leave."
  - "When the storekeeper asks what I brought, I want to show my list of today."

## 3. A day at the warehouse (journey map, condensed)

| Time | Who | Phase | Does | Thinks / feels | Opportunity |
|------|-----|-------|------|----------------|-------------|
| 08:00 | Marina | Start | Opens the app | "What happened since yesterday? Anything out of stock?" | Home answers both without a tap: today's operations as a table and an out-of-stock list |
| 08:30 | Ivan | Drop-off | Books 3 products, 72 units | "Quick, I'm parked badly." | The **+** half of the central button, then scan, number, scan, number; one review sheet |
| 08:35 | Petro | Check | Looks at Ivan's receipt | "Did he book 24 or 42 Coke?" | Activity tab: one row per operation, the document number on the row |
| 10:00 | Petro | Issue | Hands out 2 products | "Driver waiting." | The **−** half; recipient chosen from recent names in the review sheet |
| 11:15 | Petro | Question | "Do we have Skyr?" | "Two seconds." | The Search tab: the field has focus, he types "sky" and the stock shows in the row |
| 13:00 | Marina | New product | A supplier brings an unknown item | "I don't want to fill a big form." | A 3-step wizard started from the unknown line, prefilled from Open Food Facts |
| 15:40 | Petro | Mistake | Booked 21 instead of 12 | "Can I undo?" | Tap the operation in Activity, enter the right number, the app books the difference |
| 17:30 | Marina | Month end | Counts 10 products | "Let the app do the sums." | Stock count: scan, type the counted number, review the differences, post |

## 4. Journeys: now and proposed

Each journey lists the steps now (from the code) and as proposed (`revision-4.md`), with the action count. *scan* =
pointing the camera, not counted.

### J1. A delivery arrives (Petro; the same for Marina): 3 products, 12 / 6 / 24 units, supplier named

| Now (13 actions) | Proposed (7, or 8 with a new supplier name) |
|------------------|-----------------------------------------------|
| 1. Tap the *Receive goods* tile | 1. Tap **+ Receive** (bottom bar, centre left) |
| 2–3. Tap Qty, type 12; *scan* Nutella | *scan* Nutella → the line appears with its quantity field focused and "1" selected |
| 4–5. Tap Qty, type 6; *scan* Oreo | 2. Type 12 (replaces the 1); *scan* Oreo |
| 6–7. Tap Qty, type 24; *scan* Coke | 3. Type 6; *scan* Coke; 4. Type 24 |
| 8. Tap *Supplier and note* | 5. Tap **Review 42 units** (bottom bar) |
| 9–10. Tap Supplier, type the name | 6. In the review sheet, tap the supplier chip (one of the last 3), or tap the field and type (+1) |
| 11. Tap *Receive 42 units* | 7. Tap **Receive 42 units** in the sheet |
| 12. Tap *Receive* in the confirm sheet | (success banner; returns automatically to the page the user came from) |
| 13. Back to Home | |

What changes: the quantity follows the scan instead of preceding it, so you never hunt for the Qty box. The
confirmation and the optional details become one review sheet (owner item 8). Back is automatic.

### J2. Handing goods out (Petro): 2 products, recipient from earlier

| Now (11) | Proposed (6) |
|----------|--------------|
| Tile; 2 × (tap Qty, type, *scan*); details; tap Recipient, type; *Issue N units*; confirm; Back | **− Issue**; *scan*, type; *scan*, type; *Review*; recipient chip; *Issue 5 units* |

### J3. A driver's drop-off (Ivan): 2 products, no counterparty (he is the supplier)

| Now (8) | Proposed (5) |
|---------|--------------|
| Tile; 2 × (tap Qty, type, *scan*); *Receive N units*; confirm; Back | **+ Receive** (a single, unsplit button for the Driver); *scan*, type; *scan*, type; *Review*; *Receive N units* |

Ivan's Home lists **his** operations of today as a table (J-F1). Today he sees everybody's.

### J4. "Do we have X?" (anyone)

| Now | Proposed |
|-----|----------|
| By name: *Find a product* (1) → Products, tap the search field (2), type (3) → read the row. **3** | **Search** tab (1) → the field already has focus; type (2) → the row shows stock right-aligned. **2** |
| By code: the scan icon in the app bar (1) → Scanner page, *scan* → *Find product* (2) → detail. **2** | **Search** tab (1) with the camera on top (camera first) → *scan* → the product page opens. **1** |

The Scanner page and the *Find a product* button disappear. Search is one dedicated place with an icon (owner item 4).

### J5. An unknown product during a delivery (Marina)

| Now (13) | Proposed (5) |
|----------|--------------|
| *scan* → "Unknown barcode" line → *Create product* (1) | *scan* → the unknown line → *Create product* (1) |
| Form: barcode prefilled; Open Food Facts (OFF) prefills the name (or tap and type: 2–3) | Wizard step 1 (code) is skipped because the code is known. Step 2 *Name & photo*: the name comes from OFF → **Next: details** (2) |
| *Add product* (4); back on Receive, the line is resolved | Step 3 *Details*: only the characteristics used for sorting, with OFF values filled where they match (brand). Type the weight (3–4) → **Add product** (5) |
| Later, to set the characteristics: ☰ (5) → Products (6) → the row (7) → Edit (8) → 2 × (tap, type) (12) → *Save* (13) | Back on Receive automatically, the line resolved and its quantity field focused |

### J6. Monthly stock count (Marina): 10 products, 3 differ (+2, −1, −4)

The app has no stock count yet. This is **a new feature and needs the owner's written decision** (AGENTS §1);
`revision-4.md` §8 has the minimal version.

| Now: workaround (≈ 30, plus mental arithmetic) | Proposed (14, no arithmetic) |
|------------------------------------------------|------------------------------|
| Open Products and read the 10 stock numbers (1); compare them with the shelf on paper | ☰ (1) → *Stock count* (2) |
| Work out each difference by hand | 10 × (*scan*, type the counted number) (3–12) |
| One Receive for the surplus: tile, find, pick, qty, note (≈ 11) | **Review 3 differences** (13): the sheet lists +2, −1, −4 with the note "Stock count 25 Nov" |
| One Issue for the shortages: tile, 2 lines, note (≈ 16); Back (2) | **Post 3 corrections** (14): one transaction, up to two documents (In and Out) |

### J7. Fixing a mistake (Petro booked 21 Nutella instead of 12)

Operations are immutable (entities.md): the fix is a new operation in the opposite direction.

| Now (12) | Proposed (5) |
|----------|--------------|
| *Issue goods* tile (1); tap search (2), type (3), pick (4); tap the line quantity (5), type 9 (6); *Recipient and note* (7); tap Note (8), type "correction of R-…" (9); *Issue 9 units* (10); confirm (11); Back (12). The user must remember the document number and work out 21 − 12 | **Activity** tab (1) → the row "+21 Nutella 400 g · R-20261125-0003" (2) → sheet *Operation* → **Correct quantity** (3) → "It should be: [21]", type 12 (4) → the sheet shows "Issues 9 units, note: Correction of R-20261125-0003" → **Issue 9 units** (5) |

- If the correction needs an Out and the user may not issue (the Driver after over-receiving), the sheet says "Ask a
  storekeeper or the manager to correct this", and the service refuses as well.
- An under-receipt ("should be 30") books an In of 9, which the Driver may do.

### J8. A new characteristic (Marina, rare)

| Now (6) | Proposed (6) |
|---------|--------------|
| ☰; *Characteristics*; tap Name, type; *Text / Number*; *Add characteristic* | ☰; *Characteristics*; **+ New**; the sheet: type the name (focused), *Text / Number* chip, **Add characteristic** |

It is not shorter, but the page shrinks to a list with one button, and the form lives in the shared sheet. From the
add-product wizard, step 3 offers "+ New characteristic" in the same sheet, so the usual path saves the trip.

### J9. Continue an interrupted draft (anyone)

| Now (1) | Proposed (1) |
|---------|--------------|
| The tile caption says "Draft: 3 lines"; tap the tile | The **+** or **−** half shows a small count "3"; tap it |

### J10. "Where am I, and how do I get back?" (everyone, all day)

| Now | Proposed |
|-----|----------|
| Root pages and actions are in the drawer. After a task you press Back. Home, Products and Settings are roots; the rest is pushed | Four roots in the bottom bar (Home, Products, Search, Activity), always visible on root pages, with the current one highlighted. A task (Receive, Issue, Count, a wizard) covers the bar, has ← and returns to where it started, also by itself after success |

### Summary

| Journey | Now | Proposed | Saved |
|---------|----:|---------:|------:|
| J1 Delivery, 3 products + supplier | 13 | 7 | 46 % |
| J2 Issue, 2 products + recipient | 11 | 6 | 45 % |
| J3 Driver drop-off, 2 products | 8 | 5 | 38 % |
| J4 Find a product by name / by code | 3 / 2 | 2 / 1 | 33–50 % |
| J5 Unknown product during a delivery | 13 | 5 | 62 % |
| J6 Stock count, 10 products, 3 differ | ≈ 30 + sums | 14 | ≈ 50 %, no arithmetic |
| J7 Fix a wrong quantity | 12 | 5 | 58 % |
| J8 New characteristic | 6 | 6 | 0 (fewer controls on screen) |

## 5. Findings from the journeys

| # | Finding | Evidence | Where it is solved |
|---|---------|----------|--------------------|
| J-F1 | The Driver's Home lists other people's operations. entities.md, "Who changes what": the Driver may "view own" operations | `RecentActivityViewModel` reads `GetRecentAsync(Count)` without a user filter; screenshot [34](screenshots/r2/34-home-driver.png) shows Olena's and Petro's rows | revision-4 slice V3 (Driver: own operations only, in the service, not only in the UI) |
| J-F2 | Recent activity is a label stack (amount, title, caption), not a table; dates "01.01 10:22" | `ActivityRow.xaml`; [03](screenshots/r2/03-home-manager.png) | One `OperationTable` format (revision-4 §4 R3) |
| J-F3 | The quantity is asked **before** the product, which is the opposite of how people think ("this one, twelve of them") | `EntryQuantity` resets to 1 after each add | Scan, then type (revision-4 V6) |
| J-F4 | Product detail has up to 8 actions: Edit, Add/Change photo, Take, Pick, Remove, Receive, Issue, "Record one operation…", plus *Open in maps* per history row | `ProductDetailPage.xaml` | Two buttons and an overflow menu (revision-4 §6) |
| J-F5 | The add-product form shows 6 actions at once: Scan barcode, Take photo, Pick photo, Remove photo, Add product, Cancel | `ProductFormPage.xaml` | A 3-step wizard, one primary action per step |
| J-F6 | Searching lives in three places that behave differently: *Find a product* (→ Products filter), the app-bar scan (→ Scanner page), the quick search inside Receive | `HomeViewModel.FindProductAsync`, `ScanAsync` | One Search tab; the in-document quick search stays because it adds lines |
| J-F7 | After a commit the user stays on an empty Receive page | `StockDocumentViewModel` after commit | Return to the origin after the success banner |
| J-F8 | Correcting a mistake needs the document number from memory and a subtraction | no correction action | "Correct quantity" on an operation (revision-4 V9) |

## 6. Sources read for this analysis (2026-11-25)

Each page below was fetched and read for this revision. Sources from earlier revisions are in
[`research.md`](research.md), [`revision-2.md`](revision-2.md) §1 and [`revision-3.md`](revision-3.md) §3.

| Source | What we took from it |
|--------|----------------------|
| [NN/g: Journey mapping 101](https://www.nngroup.com/articles/customer-journey-mapping/) | Actor, scenario, phases, actions / mindsets / emotions, opportunities. "Journey maps should always be created to support a known business goal" |
| [NN/g: Progressive disclosure](https://www.nngroup.com/articles/progressive-disclosure/) | "Initially, show users only a few of the most important options." At most two disclosure levels. Staged disclosure (wizards) fits steps "that have little interaction" |
| [NN/g: Wizards](https://www.nngroup.com/articles/wizards/) | For infrequent processes; show the steps and the current one; explicit previous / next; "Generic labels such as Next and Previous have weak information scent", so name the next step; let users leave and resume |
| [NN/g: Modal and nonmodal dialogs](https://www.nngroup.com/articles/modal-nonmodal-dialog/) | Use modals for missing information that blocks a user-started process and for short, direct dialogs; overuse makes people ignore them |
| [NN/g: Basic patterns for mobile navigation](https://www.nngroup.com/articles/mobile-navigation-patterns/) | Tab / navigation bars suit few options, and more than 5 is hard to fit. Bars are persistent and discoverable; a hidden menu is the least discoverable ("out of sight is out of mind"). Label the icons |
| [Material Components Android: Bottom navigation](https://github.com/material-components/material-components-android/blob/master/docs/components/BottomNavigation.md) | "Navigation bars can have three to five destinations"; icon **and** label on each; 24 dp icons |
| [Material Components Android: FAB](https://github.com/material-components/material-components-android/blob/master/docs/components/FloatingActionButton.md) | A FAB "represents the primary action of a screen", the most common action; it needs a content description |
| [Material Components Android: Bottom sheet](https://github.com/material-components/material-components-android/blob/master/docs/components/BottomSheet.md) | Modal sheets block the screen behind a scrim and are dismissed by a tap outside, by dragging down, and by Back; a drag handle of at least 48 dp |
| [Laws of UX: Hick's law](https://lawsofux.com/hicks-law/) | Decision time grows with the number and complexity of choices: cut the options, split complex tasks into steps, highlight the recommended option |
| [Laws of UX: Fitts's law](https://lawsofux.com/fittss-law/) | Time to hit a target depends on its distance and size: big, near, and with space between neighbours |
| [Cleverence: Warehouse cycle count best practices](https://www.cleverence.com/articles/for-business/warehouse-cycle-count-best-practices-4827/) (a vendor blog) | Blind counts reduce anchoring bias but need tighter guidance; recount above a tolerance; post adjustments with who, when, variance and a reason; on mobile: "scan bin, then item, then enter quantity" |
| [.NET MAUI: Shell tabs](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/tabs) | "The TabBar type disables the flyout"; tabs plus a flyout come from a `FlyoutItem` with several `ShellContent`s; tab styling is colour properties only; more than 5 tabs adds a *More* tab; badges are MAUI 11 only |
| [.NET MAUI: Shell flyout](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/flyout) | `FlyoutDisplayOptions`; tabs appear when a `FlyoutItem` has more than one `ShellContent`; `FlyoutItemIsVisible` hides an item from the flyout while keeping the route |
| [.NET MAUI Community Toolkit: Popup](https://learn.microsoft.com/dotnet/communitytoolkit/maui/views/popup) | `ShowPopupAsync` / `IPopupService` / `PopupOptions` (`Shape`, `Shadow`, `PageOverlayColor`, `CanBeDismissedByTappingOutsideOfPopup`); the popup is hosted on a **modal page**, so the page below gets `OnDisappearing` / `OnAppearing`; theme-aware background through `SetPopupDefaults` |
