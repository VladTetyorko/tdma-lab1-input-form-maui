# Step 12 — Design revision 3: seen on a device, then polished

- **Lab:** none (owner request; plan in [`../design/revision-3.md`](../design/revision-3.md))
- **Branch:** `design`
- **PR:** not opened

## 1. Before

Step 11 had been built but never seen on a screen. The first emulator run
([screenshots](../design/screenshots/r2)) showed a crash without a camera (F1), a hamburger on pushed pages, a green
status bar in the dark theme, the bottom button behind the keyboard, a search with no tolerance and a number of
alignment and text defects (F1 to F9).

## 2. What was asked

Implement slices S1 to S9 of the plan in order and check them on the emulator. Owner decisions: centred title on the
phone; no table library; keep the commit confirmation as a themed sheet; scan sound on by default; no History page, no
undo after commit, no minimum stock, no packs.

## 3. Our interpretation

| Slice | In StockRoom | Status |
|-------|--------------|--------|
| S1 camera | real camera check, `Failed` state with *Try again*, CameraX start failures handled | done |
| S2 shell | own `AppBar`, status bar, back arrow, keyboard | done |
| S3 search core | `ProductMatcher`, `SearchText`, `FuzzyText` | done |
| S4 search UI | `HighlightedLabel`, "Similar name", outlined filter field, `TextMatch` removed | done |
| S5 feedback | `Plural`, success banner, `IConfirmSheet`, scan sound and its switch | done; Windows has no sound |
| S6 table, Home | aligned headers, stock chip, column chip, equal buttons, one activity row | done |
| S7 product pages | edit mode, photo area, Receive / Issue pair, form asks before discarding | done |
| S8 other pages | sign-in, scanner, characteristics, map, settings | done; startup (lazy Maps) not changed |
| S9 documentation | this step, decision log, screenshots in [`../design/screenshots/r3`](../design/screenshots/r3) | done |

### Findings F1 to F9

| # | Result |
|---|--------|
| F1 crash without a camera | fixed; Receive and New product open with `-camera-back none`, no `FATAL EXCEPTION` |
| F2 hamburger on pushed pages | fixed |
| F3 green status bar | fixed |
| F4 bottom bar behind the keyboard | fixed |
| F5 empty card in Settings | fixed |
| F6 no search tolerance | fixed |
| F7 texts | fixed |
| F8 two Undo controls | fixed on the phone (snackbar only, above the button); desktop keeps the link |
| F9 startup and discarded input | the form asks now; **startup work is deferred** (see below) |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| `IBarcodeScanner` gained `Failed` / `ReportStartFailure` | a separate availability service | Smallest change; `IsCameraSupported` is already the gate |
| `AppBarViewModel.LeftAsync` plus `Routes.IsRoot`; no `CanGoBack` | a `CanGoBack` on the navigation service | Which pages are roots is known without asking the Shell |
| Bottom edge of page grids is `SafeAreaEdges="All"` | `adjustResize` only | The window is edge to edge; resize alone left the bar under the keyboard |
| A one-letter word next to other words may match a whole word ("500 g") | single letters only at the start of the name | Otherwise "500 g" finds nothing |
| Digits next to a letter word are compared with names only | barcode tiers for every digit word | "nutella 400" also found the 500 g jar whose barcode starts with 400 |
| No `ProductIndex` cache | prepare names once on load | About ten products; not needed yet |
| Narrow value column is 88, not 72 | 72 as in the plan | "Weight (kg)" does not fit in 72 |
| The sheet does not count as leaving the page (`PageLifecycleBehavior`) | reload after every question | The reload removed the success banner |
| Photo choice is an inline pair under the photo area | a second sheet | One component less |
| *Record one operation with reason or location* stays as a quiet link on the product page | remove it with the old buttons | It is the way to attach a location (Lab 7) |

## 5. Changes

- Core: `Services/Search/*`, `Plural`, `ActivityTime`, `CameraFailures`, `IKeyboard`, `IConfirmSheet`, `ITonePlayer`,
  `IScanSoundSettings`, `AppBarViewModel`, `ProductHistoryRow`.
- App: `AppBar`, `HighlightedLabel`, `SuccessBanner`, `ActivityRow`, `ConfirmSheetPage`, `MauiConfirmSheet`,
  `MauiKeyboard`, `MauiTonePlayer`; every page restyled.
- Tests: new tests with each slice; UI test ids `MenuButton`, `BackButton`.

## 6. Not verified and not done

- Checked on an emulator only, on a host under heavy load (Android showed "isn't responding" dialogs; the app's main
  thread was idle in the one trace that was examined). Not checked on a phone.
- Not tried: a real barcode in front of a camera, the camera failing while running (the handler is tested only by its
  type-name rule), scan sound (the emulator ran without audio), Open Food Facts, the Windows exe, Appium.
- `UseMauiMaps` still initialises at start (F9): MAUI gives no simple lazy switch; the cold start is to be measured on
  the owner's phone.
- The map page logs a `MoveToRegion` warning while its map has no size.
- `Operation` page (single operation with location) still has the old style.
- Login: *Sign in* stays outlined while a field is empty (the disabled style of revision 2).

## 7. Screenshots compared with revision 2

Folder [`../design/screenshots/r3`](../design/screenshots/r3). They come from the builds of the day: 03, 04, 16, 17,
23 to 25 were taken before the last view-only fixes (framed stepper, text colours, empty status line, keyboard inset).

| Screens | r2 | r3 | What changed |
|---------|----|----|--------------|
| Sign-in | 01, 02, 33 | 01, 02 | outlined fields, password reveal, *Sign in* above the keyboard (the wrong-password message was not retaken) |
| Home, drawer | 03, 04, 13, 14, 34, 35 | 03 to 05, 27, 29 | symmetric bar, equal buttons, dark status bar |
| Receive | 05, 06, 19 to 26 | 06 to 10, 28 | no crash, bar above the keyboard, tolerant search, sheet, banner |
| Issue | 27 to 29 | 11 to 13 | out-of-stock chip, no negative stock, one Undo above the button |
| Products | 08, 15, 16 | 14, 15 | aligned columns, chips, "nutela" finds Nutella |
| Product, form | 09, 10, 30, 31 | 16 to 21 | edit mode, Receive / Issue pair, warning under the name, discard question |
| Other pages | 11, 12, 17, 18, 32 | 22 to 26 | new style |

On the emulator: no `FATAL EXCEPTION` with the camera emulated or absent. The pass without a camera used the last
build; Windows was built, not run.

## Owner actions

- Install the APK on a phone: scan a real barcode, listen for the beeps, try the keyboard and both themes.
