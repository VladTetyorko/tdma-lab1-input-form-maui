# Step 11 — Design revision 2: counter-first StockRoom

- **Lab:** none (owner request after step 10; plan in [`../design/revision-2.md`](../design/revision-2.md))
- **Branch:** `design`
- **PR:** not opened (owner decides the base branch)

## 1. Before

Step 10: side panel on desktop, bottom tabs on a phone, the product list as the start page, a Receive / Issue page with
plain entries and a small commit button, typing and camera mixed together. The app had never been started on a device.

## 2. What was asked

The requests R1–R7 of the plan: camera first with typing always possible; a drawer opened by a hamburger and closed by
a cross; a home page with two big buttons; a big high-contrast action button at the bottom, never smaller than an
input; a quick-search drop-down (product first, code second); a script that builds an APK; the gaps of step 10.

## 3. Our interpretation

The open questions of the plan (section 8) were decided by taking its own proposal: drawer on Windows too (O1), both
tiles green (O2), a quantity box next to the search (O3), no emulator download, the owner's phone instead (O4),
Products in the drawer and on Home (O5).

| Slice | In StockRoom | Status |
|-------|--------------|--------|
| S0 APK script | `scripts/build-apk.ps1` | done and run; `-Install` / `-Launch` never run against a device |
| S1 smoke check | — | **skipped**: `adb devices` listed no device and nothing may be downloaded |
| S2 Core: Home, routes, menu | `Routes.Home`, `NavigationMenu`, `HomeActions`, `HomeViewModel`, `INavigationDrawer`, restore of Receive / Issue | done, tested |
| S3 drawer shell | `AppShell`, `DrawerViewModel`, `DrawerContent`, `ReceivePage`, `IssuePage` | built, not seen on screen |
| S4 home page | `HomePage`, `ActionTile` | built, not seen on screen; no *Show all* button and no Map link |
| S5 components | `OutlinedField`, `BorderlessEntry`, `BottomActionBar`, `EmptyState`, `CameraStatusBanner`, `CameraState` | built, not seen on screen |
| S6 quick search, Core | `ProductSuggestions`, `QuickSearchViewModel`, `TextMatch` | done, tested |
| S7 quick search, UI | `QuickSearchBox`, `QuickSearchKeysBehavior` | built; arrow keys not tried |
| S8 Receive / Issue | camera first, search + quantity, bottom bar, Discard in the toolbar, details expander | ViewModel tested; page not seen |
| S9 product form | barcode first with camera, Open Food Facts prefill, duplicate warning, bottom bar | ViewModel tested; page not seen |
| S10 other pages | Products bottom bar, toolbar scan, empty states, pull to refresh, one Sign out | ViewModel tested; pages not seen |
| S11 documentation | this document, README, UI test ids | done |

Extra, asked together with the plan: `StartupGuardTests` check what would crash at start but compiles (service
registrations, page registrations, resource keys named twice or missing, XAML types taken from the wrong assembly).
They found one real problem: `ChipSuccessText` was both a theme colour and a style key; the colour is now
`ChipSuccessForeground`.

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| The drawer is `Shell.FlyoutContent` with our own view and a `DrawerViewModel` | `FlyoutItem` + `MenuItem` + item templates (the plan) | exact order and role filtering are testable in Core, every row has an `AutomationId`, no dependence on how `MenuItem` visibility behaves |
| A row or tile is composed of labels with a transparent button on top | tap gesture on a grid | the whole row stays one focusable button |
| The product list is marked stale by whoever leaves for it from Home or the drawer | reload on every appearance | pages opened from Home do not report to the list |
| The camera is released while something is typed or the drop-down is open | only hide the preview | a hidden camera must not add lines |
| After a pick on a phone with a camera the search box loses the focus | keep the focus everywhere | the preview comes back; without a camera and on desktop the box keeps the focus for keyboard scanners |
| Duplicate warning on the product form: exact barcode, exact name or a tapped suggestion | warn on every similar name | a similar name is not "already in the catalog" |
| Supplier / recipient and note stay in the expander on wide windows too | beside the search (wireframe 4.8) | one layout for both |

## 5. Changes

- **Core:** the files of S2, S6 and S9 above, `CameraMessages`, `LookupMessages`; `SessionStatusViewModel` lost the
  draft counts and Scan (the drawer has the counts, pages have a toolbar scan).
- **App:** new components in `Views/Components`, `HomePage`, rewritten `AppShell`, `StockDocumentPage`,
  `ProductFormPage`, `ProductsPage`; sizes: 48 / 40 for controls, 56 / 48 for the main action, disabled primary button
  with an outline; the round add button, status header and side panel components are removed.
- **Tests:** Core 1309 → 1387, AppTests 42 (unchanged).
- **UI tests:** ids and flow adapted (Home first, sign out in the drawer); they build and were **not run**.

## 6. Not verified and not done

**Nothing in this step was seen on a screen.** The app was not started on Android (no device) or on Windows (it would
use the real database). Unverified in particular:

- that the app starts; the drawer (hamburger, ×, scrim, Back, Esc), its look and width on Windows and Android;
- absolute routes `//home/receive` and `//home/issue`, and Back returning to Home;
- the borderless entry mapping and focus ring; the bottom bar staying above the Android keyboard
  (`SafeAreaEdges`); the toolbar overflow with *Discard draft*;
- the camera preview, permission refusal and *Allow camera*; Up / Down / Esc in the search box on Windows;
- both themes on the new components; any shadow the platform adds to the drawer;
- Open Food Facts prefill against the real service.

Not done: scan sound, torch toggle, result banner inside the camera frame, `Ctrl+Enter`, a Home *Show all* button,
a Windows `Esc` accelerator for the drawer. iOS and Mac Catalyst were not built.

## Owner actions

Install the APK on the phone:

1. Phone: Settings → About phone → tap *Build number* seven times; then Developer options → switch **USB debugging** on.
2. Connect by cable and accept the *Allow USB debugging* prompt.
3. In the repository folder: `powershell -ExecutionPolicy Bypass -File scripts\build-apk.ps1 -Install -Launch`.
   Without USB debugging: copy the file from `artifacts\apk\` to the phone, open it and allow *install unknown apps*.
4. If the install says the signature differs (an APK from CI or another computer is there), uninstall the old app
   first. **That deletes the app data on the phone.**

Then check, and report what is wrong:

1. The app starts, sign-in works for all three users, Home shows two tiles (one for the Driver).
2. Drawer: opens with the hamburger, closes with ×, scrim tap and Back; Receive from the drawer returns Home on Back.
3. Receive: camera scanning, camera refused, typing "nut" and picking, quantity 12, the button above the keyboard.
4. Product form: scan a barcode, name prefill, duplicate warning.
5. Light and dark theme on Home, the drawer, Receive.
6. On Windows (**back up `stockroom.db3` first**): the drawer, Esc, arrow keys in the search, a USB or typed barcode
   followed by Enter.

If it closes at start: `adb logcat -d | findstr /C:"FATAL EXCEPTION" /C:"AndroidRuntime"`.
