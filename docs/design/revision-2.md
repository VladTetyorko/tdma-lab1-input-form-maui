# Design revision 2 — counter-first StockRoom

Status: **implemented; first seen on an emulator in [`revision-3.md`](revision-3.md)** (findings and the next plan there; see [`../steps/11-design-revision.md`](../steps/11-design-revision.md),
which also lists the deviations). It revises [`ux-plan.md`](ux-plan.md) and
[`design-system.md`](design-system.md) where they disagree; everything else in them still holds (green light / dark violet
themes, no shadows, outlined secondary buttons, no neon, sortable table, batch documents in one transaction, role rules,
English UI). What was built before this revision and what it left open: [`../steps/10-design.md`](../steps/10-design.md).

## 0. Owner request (translated) and our reading

| # | Request | Our reading |
|---|---------|-------------|
| R1 | Rework receiving, issuing and adding a product: **camera first** when it works, typing always possible but secondary; typing is the main path when there is no camera. Then notes and the other fields. The **Add / commit button is big, high-contrast, not smaller than the inputs, at the bottom** | Receive / Issue / Product form get the same order: *identify the product* (camera, or typed name / code) → *lines and quantities* → *optional details* → *one big filled action in a bottom bar* |
| R2 | Menu: no fixed side panel and no bottom tabs; a **drawer** opened by the hamburger (three lines) and closed by a cross | `FlyoutBehavior.Flyout` on every device, custom header with a close button |
| R3 | Main screen = **two big buttons, Receive items and Issue items**. The user is the person at the counter | New `Home` page, the start page after sign-in |
| R4 | Review design rules (Material 3, Apple HIG, Nielsen, touch targets, thumb zone, POS / scanner apps) with sources, then redo the design from them | Section 1 (sources), section 2 (rules adopted) |
| R5 | A script that builds an APK for the owner's phone | `scripts/build-apk.ps1`, slice S0 (first) |
| R6 | (added later) **Quick search drop-down** in manual entry: suggestions while typing, product first (name, key characteristics, stock) and code second; match name and code; pick = add / select; no-match case | Slice S6 + S7, reused on Receive, Issue and the product form |
| R7 | Fix the gaps of step 10 that matter here | Outlined inputs with a focus ring, camera unavailable banner, empty states, disabled-button reason, restart restores Receive / Issue |

Role rule (unchanged, `RolePermissions.CanRecord`): the **Driver** may receive but not issue.

## 1. Research — sources read for this revision

All pages below were fetched on 2026-11-25 and the stated facts were read on them, except where marked
**[unverified]**. Earlier sources (Scandit, Odoo, inFlow, Sortly, NN/g data tables) are in [`research.md`](research.md) §2.

| Source | What it says (used here) |
|--------|--------------------------|
| [NN/g — 10 usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/) | Visibility of system status; match the real world; user control ("clearly marked emergency exit", undo); consistency; error prevention; recognition rather than recall ("make elements, actions, and options visible"); flexibility (shortcuts for experts); minimalist design; plain-language errors that suggest a fix |
| [NN/g — Hamburger menus and hidden navigation](https://www.nngroup.com/articles/find-navigation-mobile-even-hamburger/) | Hidden navigation was used less (Bloomberg hidden menu 44 % vs combined visible + menu 89 %) and later; "the hamburger is an imperfect answer"; if navigation is hidden, make the icon stand out with a clear signifier; **combine**: expose the key items, collapse the rest |
| [W3C — Understanding SC 2.5.8 Target Size (Minimum)](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html) | AA minimum 24×24 CSS px (or spacing so 24 px circles do not overlap); the enhanced SC 2.5.5 uses 44×44 |
| [Google — Android accessibility: touch target size](https://support.google.com/accessibility/android/answer/7101858) | 48×48 dp (≈ 9 mm), targets separated by ≥ 8 dp |
| [Apple — UI design dos and don'ts](https://developer.apple.com/design/tips/) | Controls ≥ 44×44 pt; text ≥ 11 pt; ample contrast. (The HIG "Buttons" page itself did not render for the fetcher; the "one prominent button per context" rule is **[unverified]** from that page) |
| [Microsoft — Windows targeting guidelines](https://learn.microsoft.com/windows/apps/develop/input/guidelines-for-targeting) | Touch target ≈ 7.5 mm = **40×40 epx**; make frequently pressed targets larger; targets with severe consequences get more padding and distance |
| [Material Components Android — Navigation drawer](https://github.com/material-components/material-components-android/blob/master/docs/components/NavigationDrawer.md) | Modal drawer is for mobile where space is limited, blocks content with a scrim (default black 60 %); closes by selecting an item, back gesture, the scrim (optional) and **Esc** on a keyboard; open/close widgets ≥ 48 dp with accessibility labels. (m3.material.io itself needs JavaScript and could not be read: Material 3 statements beyond this repository are **[unverified]**) |
| [Material Components Android — Text field](https://github.com/material-components/material-components-android/blob/master/docs/components/TextField.md) | Outlined field: label, 1 dp outline (`colorOutline`), **2 dp outline in `colorPrimary` when focused**, error outline `colorError`, error text replaces helper text, optional leading / trailing icons (clear text) |
| [Hoober — How do users really hold mobile devices (UXmatters, 2013)](https://www.uxmatters.com/mt/archives/2013/02/how-do-users-really-hold-mobile-devices.php) | 1 333 observations: one hand 49 %, cradled 36 %, two hands 15 %; grips change often. Author's 2017 comment: disregard the reach drawings, people touch the **centre** most. So: no rigid "thumb map"; keep the frequent action large and low-centre, keep rare / risky actions away from it |
| [Baymard — Autocomplete design](https://baymard.com/blog/autocomplete-design) | ≤ 10 suggestions on desktop, **4–8 on mobile**; no scrollbar inside the list; shade the active row; ↑ / ↓ move, Enter picks, list wraps; mobile: large text, big targets, dividers |
| [NN/g — Search suggestions](https://www.nngroup.com/articles/site-search-suggestions/) | A suggestion must lead to a good result (no dead ends); style typed text vs suggested text differently; on mobile keep suggestions simple |
| [Scandit — Scanning at scale UX insights](https://www.scandit.com/blog/scanning-at-scale-ux-insights/) | Scanning starts hundreds of times a day: "a small icon or button just won't do"; feedback by sight, sound and haptics; a 150 ms pause after feedback builds confidence. It does not discuss manual-entry fallback |
| [.NET MAUI — Shell flyout](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/flyout) | `FlyoutBehavior` = `Disabled` / `Flyout` / `Locked`; `FlyoutHeader`, `FlyoutFooter`, `FlyoutContent(Template)`, `FlyoutBackdrop` (brush), `FlyoutWidth`, `FlyoutIcon`; `MenuItem` with `Command`; `Shell.FlyoutItemIsVisible`; open / close in code with `Shell.Current.FlyoutIsPresented`; flyout item badges are **.NET MAUI 11 only** |
| [.NET MAUI — Safe area (.NET 10)](https://learn.microsoft.com/dotnet/maui/user-interface/safe-area) | `SafeAreaEdges` = `None` / `SoftInput` / `Container` / `Default` / `All`; `ContentPage` defaults to `None` on .NET 10; a Grid with `SafeAreaEdges="Container, Container, Container, SoftInput"` keeps a bottom bar above the keyboard; `SoftInput` does not work on a `ScrollView` directly — wrap it |
| [.NET MAUI — Soft keyboard input mode (Android)](https://learn.microsoft.com/dotnet/maui/android/platform-specifics/soft-keyboard-input-mode) | `Application.WindowSoftInputModeAdjust` = `Pan` / `Resize`; on .NET 10 `Resize` may also need `SafeAreaEdges="All"` |
| [.NET MAUI — Customize handlers](https://learn.microsoft.com/dotnet/maui/user-interface/handlers/customize) | `EntryHandler.Mapper.AppendToMapping(key, …)` changes the native view (Android `MauiAppCompatEditText`, Windows `TextBox`); global unless filtered by a subclass (`if (view is MyEntry)`) |

POS / counter guidance: no vendor guideline page with checkable rules was found that is public and readable (Square and
Shopify POS pages are product help, not design rules) — the POS-specific rules below come from the scanner sources above
and the comparable apps in `research.md` (Odoo, inFlow, Sortly: scan → running list → one commit).

## 2. Rules adopted

| # | Rule | From | Where it shows |
|---|------|------|----------------|
| D1 | **The two daily actions are visible, not in a menu.** Home shows *Receive goods* and *Issue goods* as large tiles; the drawer holds the rest | NN/g hidden navigation (combine visible + menu); heuristic 6 | Home |
| D2 | **Drawer = modal flyout on every device** with a scrim (`Overlay` token, no shadow), a 48 dp hamburger, a 48 dp close (×) in its header, closes on item tap, scrim tap, Back, Esc (Windows: **[to verify]**) | Owner R2; Material drawer docs; MAUI flyout docs | Shell |
| D3 | **Touch targets**: ≥ 48 dp on Android, ≥ 40 epx on Windows (was 36), ≥ 8 dp apart; frequent targets bigger: main action 56 dp / 48 epx, home tiles ≥ 112 dp | Google 48 dp; Windows 40 epx; Apple 44 pt; WCAG 2.5.8; Windows "frequent = larger" | Tokens, all pages |
| D4 | **The main action is a full-width filled button in a bottom action bar** that stays above the soft keyboard; it is never smaller than an input; the label says what happens and how much ("Receive 37 units") | Owner R1; Scandit (big, pressed all day); Hoober (low-centre reach); heuristic 2 | Receive, Issue, Product form, Products (Manager) |
| D5 | **Risky actions are away from the main action**: *Discard draft* moves to the page toolbar (overflow on Android) with a confirm; delete stays on rows / detail | Windows targeting (severe consequence = distance); heuristic 3 / 5 | Receive, Issue |
| D6 | **Camera first, typing always there.** Camera works → preview on top, the search field under it. No camera / denied / failed → a clear banner with the reason and a fix, the search field moves up and gets focus | Owner R1; heuristic 1, 9; AGENTS "fail soft" | Receive, Issue, Product form |
| D7 | **Recognition over recall in manual entry**: a quick-search drop-down shows product name and key characteristics first, the code second; ≤ 5 rows on a phone, ≤ 8 on desktop; no scrollbar inside; ↑ / ↓ / Enter / Esc on Windows; a no-match row offers the next step | Baymard; NN/g suggestions; heuristic 6, 7 | S6, S7 |
| D8 | **Outlined inputs**: label above, 1 px `BorderStrong` outline, 2 px `FocusRing` when focused, `Danger` outline + message under it on error, clear (×) button inside when text is present | Material text field docs; design-system §7 | All forms |
| D9 | **Every state is visible**: scan result banner (name + "+1", or the problem), draft summary, sync chip only when it is not "all synced", empty states with a next step, disabled buttons with a reason | Heuristic 1, 8, 9 | All pages |
| D10 | **Two equal primaries on Home are both filled** (exception to "one filled button per screen"); every other screen keeps one filled button | Owner R3 (high contrast) + design-system §1 | Home |
| D11 | **Disabled filled button keeps its shape**: `SurfaceAlt` fill + 1 px `BorderStrong` outline (3.55:1) + a caption with the reason (e.g. "Add a product first") | WCAG 1.4.11 non-text contrast (ratio from design-system §4); heuristic 9 | Bottom bars |
| D12 | **Errors in plain language** next to the thing that is wrong, with the fix ("Only 4 on hand — lower the quantity"), never a code | Heuristic 9 | Lines, forms |

## 3. Navigation (replaces ux-plan §2 "Shell structure" and §3 header)

```
Sign in ──> HOME ───────────────┬── [Receive goods] ──> Receive (pushed on Home: Back returns Home)
             │                  ├── [Issue goods]   ──> Issue   (pushed on Home; hidden for the Driver)
             │                  └── recent activity, quick links
             └── ☰ drawer: Home · Receive goods (3) · Issue goods · Products · Map · Characteristics* · Settings
                          header: user card + (×)    footer: sync status · Sign out           * Manager only
```

- **Top-level `FlyoutItem`s**: Home, Products, Settings (absolute routes `//home`, `//products`, `//settings`).
- **`MenuItem`s** (commands, not Shell content): *Receive goods*, *Issue goods*, *Map*, *Characteristics*. Receive / Issue
  navigate to `//home/receive` / `//home/issue`, so the system Back button and the toolbar back arrow return to Home
  (heuristic 3) instead of closing the app; Map / Characteristics push on the current page as today.
- `Receive` and `Issue` become two thin page classes `ReceivePage` / `IssuePage : StockDocumentPage` registered with
  `Routing.RegisterRoute` and DI, so no `RouteFactory` is needed.
- Draft counts stay in the menu text ("Receive goods (3)"); Shell badges exist only in MAUI 11, so no badge.
- Role visibility: `Shell.SetFlyoutItemIsVisible` / `IsVisible` from `NavigationMenu.For(role)`; the service layer still
  refuses (`StockDocumentService`), and `StockDocumentViewModel` refuses to open *Issue* for a Driver (guard message +
  back to Home) in case of a restored or typed route.
- The **StatusHeader with its Scan button disappears** from pages. The drawer header shows the user card; pages show a
  compact `SyncChip` in the title row **only** when offline / waiting / syncing. *Scan to look up* is a toolbar icon
  (48 dp, `SemanticProperties.Description="Scan a barcode"`) on Home and Products.

### MAUI feasibility (read in the docs, still to be seen running)

| Need | MAUI 10 way | Risk / check |
|------|-------------|--------------|
| Drawer on phone and Windows | `Shell.FlyoutBehavior="Flyout"` (default) on Shell; `Disabled` set on the Login page only (attached property on the page) | WinUI renders the flyout as a `NavigationView` pane; whether it overlays (not pushes) content on a wide window is **[to verify]** in the S3 spike. Fallback: `FlyoutWidth` 320 and accept push |
| Hamburger icon | Default Shell toggle; `FlyoutIcon` = `FontImageSource` (`IconNavigation`, 24) for a consistent look | Android shows it in the toolbar on root pages only — correct (pushed pages show Back). Its `AutomationId` cannot be set; Appium finds it by content description ("Open navigation drawer" on Android **[unverified]**) |
| Close (×) in the header | `FlyoutHeader` = `DrawerHeader` ContentView with a 48 dp icon button bound to `SessionStatusViewModel.CloseMenuCommand` | none |
| Close from a ViewModel | New Core interface `INavigationDrawer { bool IsOpen { get; } void Open(); void Close(); }`; app class `ShellNavigationDrawer` sets `Shell.Current.FlyoutIsPresented` on the main thread | ViewModels never touch `Shell.Current` (AGENTS §2) |
| Scrim, no shadow | `Shell.FlyoutBackdrop` = `{DynamicResource Overlay}` brush; `FlyoutBackgroundColor` = `Surface`; 1 px `Border` on the drawer edge | Android draws its own drawer elevation shadow **[to verify]**; if visible, accept (platform chrome) and record |
| Item look | `Shell.ItemTemplate` / `MenuItemTemplate` (one template for both): 56 dp rows, 24 icon, `TextBody`, selected = `PrimarySoft` + `PrimaryText` | none |
| Esc closes the drawer on Windows | WinUI `NavigationView` closes its overlay pane on Esc **[unverified]**; if not, a `KeyboardAccelerator` in `Platforms/Windows` calls `INavigationDrawer.Close` | S3 spike |

## 4. Wireframes

Legend: `[[ X ]]` filled primary, `[ X ]` outlined, `( )` chip, `{x}` red ghost icon, `☰` hamburger, `×` close.

### 4.1 Home — phone (Storekeeper, Manager)

```
+--------------------------------------+
| ☰  StockRoom              (|||) scan  |  <- nav bar; scan = look-up, 48 dp
|                     (Offline · 2 wait)|  <- only when not "all synced"
|  Good morning, Petro                 |
|  Storekeeper                         |
|                                      |
| +----------------------------------+ |
| |  [↓]  RECEIVE GOODS              | |  <- filled tile, 120 dp, full width
| |       Draft: 5 lines · continue  | |     caption only when a draft exists
| +----------------------------------+ |
| +----------------------------------+ |
| |  [↑]  ISSUE GOODS                | |  <- filled tile, 120 dp
| |       Hand goods out             | |
| +----------------------------------+ |
|                                      |
|  [ Find a product ]  [ + Add product ]* |  <- outlined, 48 dp (* Manager)
|  Recent activity                     |
|  +12 Nutella 400 g · Petro · 10:42   |
|  −3  Oreo Original · Olena · 10:15   |
|  [ Show all ]                        |
+--------------------------------------+
```

Driver: one *Receive goods* tile, then the caption "Issuing goods is done by a storekeeper or a manager." (no disabled
tile — a disabled tile invites a tap that cannot work). Tiles sit in the middle / lower half of the screen (Hoober:
centre is touched most); recent activity scrolls below them.

### 4.2 Home — wide (Windows, tablet ≥ 840)

```
+------------------------------------------------------------------------------------+
| ☰  StockRoom                                                       (|||) Scan      |
|  Good morning, Olena · Manager                                                      |
|  +------------------------------------+  +------------------------------------+     |
|  |  [↓]  RECEIVE GOODS                |  |  [↑]  ISSUE GOODS                  |     |  <- 2 tiles, 112 epx,
|  |       Draft: 5 lines · continue    |  |       Hand goods out               |     |     max width 1200
|  +------------------------------------+  +------------------------------------+     |
|  [ Find a product ]  [ + Add product ]  [ Map ]                                     |
|  Recent activity ------------------------------------------------------------------ |
|  +12 Nutella 400 g   R-20261125-0003   Petro   10:42                                |
+------------------------------------------------------------------------------------+
```

### 4.3 Drawer — phone (open)

```
+---------------------------+----------+
| (PK) Petro Kovalenko   ×  |##########|  <- × 48 dp, AutomationId CloseMenuButton
|      Storekeeper          |## scrim ##|  <- Overlay 40 %, tap = close
|---------------------------|##########|
| ⌂  Home                   |##########|  <- selected: PrimarySoft row
| ↓  Receive goods (5)      |##########|
| ↑  Issue goods            |##########|
| ▤  Products               |##########|
| ⌖  Map                    |##########|
| ⚙  Settings               |##########|
|                           |##########|
|---------------------------|##########|
| (o) Online · all synced   |##########|
| [ Sign out ]              |##########|
+---------------------------+----------+
   width min(320, 85 % of screen)
```

### 4.4 Drawer — wide

Same content, width 320, overlays the page with the scrim (or pushes it, see §3 risk). No permanent side panel.

### 4.5 Receive goods — phone, camera works

```
+--------------------------------------+
| ←  Receive goods            ⋮        |  <- ⋮ = Discard draft (confirm)
| Draft · 2 lines · 18 units           |
| +----------------------------------+ |
| |        camera preview 4:3        | |  <- continuous; torch toggle top-right
| | [✓ Nutella 400 g  +1]            | |  <- result banner, 1.5 s
| +----------------------------------+ |
| +----------------------------+ +---+ |
| | 🔍 Type a name or barcode   | | 1 | |  <- outlined 48 dp; qty for typed adds
| +----------------------------+ +---+ |
|                                      |
| Nutella 400 g          [-] 12 [+] {x}|  <- 48 dp steppers
| 3017620422003 · on hand 24 → 36      |
| Oreo Original          [-]  6 [+] {x}|
| 7622210449283 · on hand 0 → 6        |
|                                      |
| ▸ Supplier and note (optional)       |  <- expander, collapsed
|--------------------------------------|
| [[        Receive 18 units        ]] |  <- bottom bar, 56 dp, full width
+--------------------------------------+
```

### 4.6 Receive goods — phone, no camera / typing, keyboard open

```
+--------------------------------------+
| ←  Receive goods            ⋮        |
| ⚠ Camera not available on this device.|  <- WarningSoft banner; "denied" variant:
|   Type a name or barcode.            |     "Camera access is off. [Allow camera]"
| +----------------------------+ +---+ |
| | nute                     × | | 1 | |  <- focused: 2 px FocusRing
| +----------------------------+ +---+ |
| +----------------------------------+ |
| | Nutella 400 g          24 on hand| |  <- suggestion: name first (TextBodyStrong)
| | Ferrero · 400 g                  | |     key characteristics (caption)
| | 3017620422003                    | |     code last (caption, TextSecondary)
| |----------------------------------| |
| | Nutella B-ready 22 g    0 on hand| |
| | Ferrero · 22 g · 8000500217078   | |
| +----------------------------------+ |  <- max 5 rows, no inner scrollbar
|--------------------------------------|
| [[        Receive 18 units        ]] |  <- stays above the keyboard (SafeAreaEdges SoftInput)
|######## soft keyboard ###############|
+--------------------------------------+
```

While the search field has focus on a phone, the camera card collapses (`IsTyping`) so the field and suggestions sit at
the top and the keyboard covers only the lines. Lines are hidden behind the open drop-down; closing it (pick, Esc,
Back, tap outside) shows them again.

No match:

```
| +----------------------------------+ |
| | No product matches "nutela".     | |
| | [ + Create product "nutela" ]*   | |  <- Manager; others: "Ask a manager to add it."
| +----------------------------------+ |
```

### 4.7 Issue goods

Same page with direction Out: title *Issue goods*, field *Recipient*, lines show "Only 4 on hand" in `Danger` with an
icon when the quantity is above stock, the button reads *Issue N units*; while a line is invalid the button is disabled
with the caption "Fix the red lines first".

### 4.8 Receive / Issue — wide (Windows)

```
+------------------------------------------------------------------------------------------+
| ←  Receive goods                                   Draft · 2 lines · 18 units   [Discard draft] |
| ⚠ No camera on this computer. Type a name or barcode, or use a USB scanner.              |
| +------------------------------------------------+ +-----+                               |
| | 🔍 Type a name or barcode                    × | |  1  |   Supplier [______________]   |
| +------------------------------------------------+ +-----+   Note     [______________]   |
| | ▸ Nutella 400 g · Ferrero · 400 g   24 on hand | <- ↑/↓ highlight, Enter adds, Esc closes |
| |   3017620422003                                |                                        |
| +------------------------------------------------+                                        |
|------------------------------------------------------------------------------------------|
| PRODUCT            | BARCODE        | ON HAND | QTY          | AFTER |                    |
| Nutella 400 g      | 3017620422003  |   24    | [-] 12 [+]   |  36   | {x}                |
|------------------------------------------------------------------------------------------|
|                                                     [[      Receive 18 units       ]]   |  <- 48 epx, min 320 wide
+------------------------------------------------------------------------------------------+
```

The focus stays in the search box after every add, so a keyboard-wedge scanner keeps working (existing behaviour).

### 4.9 Add product — phone

```
+--------------------------------------+
| ←  New product                       |
| 1. Barcode                           |
| +----------------------------------+ |
| |   camera preview (scan barcode)  | |  <- Android; closes after a read
| +----------------------------------+ |
| +----------------------------------+ |
| | Barcode (EAN-13)                 | |
| | 3017620422003                  × | |
| +----------------------------------+ |
|  ✓ Found in Open Food Facts — name filled in  (ODbL)|
| 2. Name                              |
| +----------------------------------+ |
| | Nutella 400 g                    | |
| +----------------------------------+ |
|  ⚠ Already in the catalog: Nutella 400 g (3017620422003) [Open]|  <- quick search, duplicate guard
| 3. Photo (optional)  [ Take photo ] [ Pick photo ] |
|--------------------------------------|
| [[           Add product          ]] |  <- bottom bar; "Save changes" when editing
+--------------------------------------+
```

### 4.10 Add product — wide

```
+------------------------------------------------------------------------------+
| ←  New product                                                               |
|  +---------------------------------+   +----------------------------------+  |
|  | Barcode (EAN-13)                |   | Photo (optional)                 |  |
|  | [ 3017620422003             × ] |   | [thumbnail]  [ Pick photo ]      |  |
|  | ✓ Found in Open Food Facts      |   |                                  |  |
|  | Name                            |   |                                  |  |
|  | [ Nutella 400 g               ] |   |                                  |  |
|  +---------------------------------+   +----------------------------------+  |
|------------------------------------------------------------------------------|
|                                  [ Cancel ]   [[        Add product        ]] |
+------------------------------------------------------------------------------+
```

## 5. Implementation slices (in order)

Each slice: contract first, tests with the slice, `dotnet test Mobile_apps_L1_Model_view.Tests` green, commit
`[Design] …`. Build Windows and Android before the last commit of a UI slice. New platform-free classes go to
`StockRoom.Core`; anything with a MAUI type goes to the app behind a Core interface.

### S0 — APK build script (first)

**Files:** `scripts/build-apk.ps1` (new), `README.md` (how to run), `docs/RELEASE.md` (point to the script).
`.gitignore` already ignores `artifacts/` and `*.apk`.

Behaviour:

1. `param([switch]$Install, [switch]$Launch, [string]$Serial, [string]$SdkPath, [string]$OutDir = "artifacts/apk")`;
   `$ErrorActionPreference = 'Stop'`; runs in Windows PowerShell 5.1 and PowerShell 7; resolves paths from
   `$PSScriptRoot` so it works from any folder.
2. **Find the SDK**: `-SdkPath` → `$env:ANDROID_HOME` → `$env:ANDROID_SDK_ROOT` → `$env:LOCALAPPDATA\Android\Sdk`;
   fail with a clear message if none contains `platforms\` and `build-tools\`. (This machine: found at
   `%LOCALAPPDATA%\Android\Sdk`, platforms `android-36` and `android-37.0`, build-tools `36.0.0`.)
3. **Find the JDK**: `$env:JAVA_HOME` (this machine: Microsoft JDK 17) → pass `-p:JavaSdkDirectory`; if missing, let
   the .NET Android SDK use its default and say so.
4. `dotnet publish Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-android -c Release
   -p:AndroidPackageFormat=apk -p:AndroidSdkDirectory="<sdk>" [-p:JavaSdkDirectory="<jdk>"]` — the same command CI
   uses; **debug-signed** because no keystore is configured (decision 86). Fail on a non-zero exit code.
5. Copy `bin/Release/net10.0-android/publish/*-Signed.apk` to `$OutDir/StockRoom-<ApplicationDisplayVersion>-<git short
   sha>.apk`; print path, size and SHA-256. Never copy the unsigned twin.
6. `-Install`: `adb` from `<sdk>\platform-tools`; list devices; zero → "Connect the phone with USB debugging on" and exit
   1; more than one without `-Serial` → list them and exit 1; then `adb [-s] install -r <apk>`. On
   `INSTALL_FAILED_UPDATE_INCOMPATIBLE` explain that an APK signed with another key (e.g. the CI artifact) is installed
   and must be uninstalled first — **uninstalling deletes the app data on the phone**, so the script never does it itself.
7. `-Launch`: `adb shell monkey -p com.companyname.mobile_apps_l1_model_view -c android.intent.category.LAUNCHER 1`.

**Acceptance:** from a clean checkout `pwsh scripts/build-apk.ps1` produces one installable APK under `artifacts/apk/`;
`git status` stays clean; a wrong `-SdkPath` fails with a message, not a stack trace; `-Install` with no device fails
with the USB message. **Tests:** none automated (a build script; CI keeps its own `apk` job). Manual run recorded in the
step document.

### S1 — Startup smoke check (conditional: **no emulator on this machine**)

Checked on 2026-11-25: `%LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe -list-avds` prints nothing, there is no
`system-images` folder, no `%USERPROFILE%\.android\avd`, and `adb devices` lists no device. So the implementer cannot
start the app on Android without downloading a system image (≈ 1–2 GB; needs owner approval and a hypervisor) or
the owner's phone.

Plan when a device exists (owner's phone over USB, or an AVD the owner approves:
`sdkmanager "system-images;android-36;google_apis;x86_64"` + `avdmanager create avd -n stockroom -k …`):

1. `scripts/build-apk.ps1 -Install -Launch`.
2. `adb logcat -d` filtered for `FATAL EXCEPTION`, `AndroidRuntime` and the app's Serilog tag → none after 20 s.
3. `adb exec-out screencap -p > artifacts/screens/<page>.png` for Login, Home, drawer open, Receive (both themes).
4. Record results in the step document; screenshots are not committed (personal report material).

The Windows exe is **not** started by the agent (it uses the owner's real database).

### S2 — Core: Home, routes and menu

**Files (Core):** `Services/Routes.cs` (+`Home`, `ForShell` includes it), `Services/NavigationMenu.cs` (entries Home,
Receive, Issue, Products, Map, Characteristics, Settings; `MenuEntry` gains `Kind` = `Top` / `Action` and `Glyph` key),
`Services/HomeActions.cs` + `HomeAction.cs` (role → primary tiles and secondary links, read from `RolePermissions`),
`ViewModels/HomeViewModel.cs` (greeting, role text, tiles with draft captions from `IStockDocumentDraftStore`,
`OpenCommand(route)`, owns a `RecentActivityViewModel`), `Services/INavigationDrawer.cs`,
`AppLifecycleService` / `LoginViewModel` start on `Routes.Home`, restore `//home/receive` / `//home/issue` when that
was the last page (gap from step 10).
**Tests:** `NavigationMenuTests` (per role, order, kinds), `HomeActionsTests` (Driver has no Issue tile and gets the
explanation; Manager gets *Add product*), `HomeViewModelTests` (draft caption appears / disappears on `drafts.Changed`,
open command navigates to `//home/receive`), `RoutesTests`, updated `LoginViewModelTests`, `AppLifecycleServiceTests`,
`StartupNavigatorTests`, `AppLifecycleFlowTests` (expected route `home`), `RolePermissionsMatrixTests` unchanged.
**Acceptance:** all routes and role rules covered; no MAUI reference in Core.

### S3 — Drawer shell (spike first, 30 min)

**Files (app):** `AppShell.xaml(.cs)` rewritten: `FlyoutBehavior="Flyout"`, no `TabBar`, no `Locked`; FlyoutItems Home /
Products / Settings, MenuItems for the actions; `Shell.ItemTemplate`/`MenuItemTemplate` in `Resources/Styles`;
`Views/Components/DrawerHeader.xaml` (user card + × button, replaces `SidePanelHeader`),
`Views/Components/DrawerFooter.xaml` (sync + Sign out, replaces `SidePanelFooter`); `Services/ShellNavigationDrawer.cs`;
`Views/ReceivePage.cs`, `Views/IssuePage.cs`; `LoginPage` sets `Shell.FlyoutBehavior="Disabled"`; `MauiProgram`
registrations; Windows Esc accelerator only if the spike shows it is needed. `SessionStatusViewModel` gains
`CloseMenuCommand` (uses `INavigationDrawer`). Remove `StatusHeader` usages; keep `SyncChip`.
**Tests:** `SessionStatusViewModelTests` (close command calls the drawer fake), AppTests: none new (Shell needs a UI
thread). UiTests: update `AutomationId`s (`CloseMenuButton`, `MenuItem_Receive`, `MenuItem_Issue`, `SignOutButton`).
**Acceptance (by hand, recorded):** hamburger opens, × / scrim / item / Back close; Driver sees no *Issue goods*;
Receive opened from the drawer returns Home on Back; Windows and Android build.

### S4 — Home page

**Files (app):** `Views/HomePage.xaml(.cs)`, `Styles.xaml` `ActionTile` style (filled `Primary`, `RadiusM`, min height
`HomeTileHeight`, icon 32 + `TextTitle` + caption `OnPrimary` 90 %), `OnIdiom` / width trigger for 1 or 2 columns.
**AutomationIds:** `HomeReceiveTile`, `HomeIssueTile`, `HomeFindProductButton`, `HomeAddProductButton`,
`HomeIssueNotAllowedLabel`. **Acceptance:** contrast `OnPrimary` on `Primary` 5.38 (light) / 5.69 (dark) — already in
design-system §4; tiles ≥ 112 dp; Driver layout as §4.1.

### S5 — Shared input and layout components (gaps of step 10)

**Files (app):** `Views/Components/OutlinedField.xaml(.cs)` (label, inner `BorderlessEntry`, clear ×, `ErrorText`,
visual states Normal / Focused / Error / Disabled driven by the inner entry's `Focused`/`Unfocused`),
`Views/Components/BorderlessEntry.cs` (subclass), handler mapping in `MauiProgram`
(`EntryHandler.Mapper.AppendToMapping("Borderless", …)` filtered by `view is BorderlessEntry`: Android
`BackgroundTintList` transparent; Windows `TextBox.BorderThickness = 0`, transparent background, focus underline
resources off — **[to verify]** on both), `Views/Components/BottomActionBar.xaml` (Grid row with `Surface` background,
1 px top `Border`, padding 16 / 12, one `Primary` button + optional caption), `Views/Components/EmptyState.xaml`
(icon, title, message, optional outlined action), `Views/Components/CameraStatusBanner.xaml`.
Page root grids that host a bottom bar get `SafeAreaEdges="Container, Container, Container, SoftInput"` (MAUI .NET 10
doc pattern); the scrolling part is a `ScrollView` inside that grid.
**Styles / tokens:** `ControlHeight` 48 phone / **40** desktop; new `ActionHeight` 56 / 48, `HomeTileHeight` 120 / 112,
`DrawerWidth` 320; disabled `Primary` gets 1 px `BorderStrong` (D11); `Fab` style removed.
**Core:** `Services/CameraState.cs` (`Unsupported`, `Denied`, `Active`, `Off`) + `CameraMessages` (texts per state —
pure, tested).
**Tests:** `CameraMessagesTests`; AppTests: theme XML test extended for the new keys in both dictionaries.
**Acceptance:** an `Entry` in an `OutlinedField` shows the 2 px ring on focus on Windows (keyboard Tab) and Android; the
bottom bar stays visible with the keyboard open on Android (S1 device or owner).

### S6 — Quick search: Core

**Files (Core):** `Services/ProductSuggestions.cs` (pure), `Services/ProductSuggestion.cs` (record: `Product`,
`Title` = name, `Details` = first two non-empty characteristic values in characteristic order joined by " · ",
`Code` = barcode, `StockText` = "24 on hand" / "Out of stock", `IsExactCode`), `ViewModels/QuickSearchViewModel.cs`
(reusable, transient), shared `Services/TextMatch.cs` extracted from `ProductSearch` (both use it — DRY, refactoring in
its own commit).
Rules (`ProductSuggestions.Find(products, characteristics, text, max)`):

- trimmed, case-insensitive; **name and code only** (owner R6; the list search keeps matching values);
- at least 2 characters, or 1+ digits (a code can be typed from its start);
- rank: exact code → code starts with → name starts with → a word of the name starts with → name or code contains;
  ties by name; at most `max` (5 phone, 8 desktop — Baymard);
- no I/O: the ViewModel loads `IProductCatalog.GetSummariesAsync()` and `ICharacteristicService.GetAllAsync()` once per
  page appearance and after a commit / created product (≈ 10 products — **no debounce timer**, filtering is instant;
  if the catalogue ever grows past a few thousand, add a 150 ms debounce behind an `IDelay` abstraction — YAGNI now).

`QuickSearchViewModel`: `Query`, `Suggestions`, `HighlightedIndex` (−1 = none), `IsOpen`, `NoMatchText`,
`CanCreate` (from `RolePermissions.CanManageProducts`), commands `Pick(suggestion)`, `MoveNext`, `MovePrevious`,
`Submit` (Enter), `Close` (Esc), `Create`; raises `Picked(ProductSummary)` and `SubmittedText(string)`.
**Enter rule (keeps keyboard-wedge scanners working):** a highlighted row → pick it; otherwise, if the text parses as a
barcode (`ScanPayloadParser`) → `SubmittedText` (the existing barcode path, also for unknown codes); otherwise, if
exactly one suggestion → pick it; otherwise nothing. Typing never auto-picks.
No match: Manager → *Create product "…"* opens the product form with `NavigationKeys.Barcode` (digits) or
`NavigationKeys.ProductName` (text) — both already supported by `ProductFormViewModel`; others see "Ask a manager to add
it." Logging: `Debug` only (search is not a data change); a create navigation logs `Information`.
**Tests:** `ProductSuggestionsTests` (ranking, max, digits vs letters, empty, details order, out of stock text, no
characteristic values), `QuickSearchViewModelTests` (open / close, highlight wraps, Enter rules incl. wedge path,
no-match texts per role, create refused for a Storekeeper with a `Warning`), `ProductSearchTests` unchanged and green.

### S7 — Quick search: UI component

**Files (app):** `Views/Components/QuickSearchBox.xaml(.cs)` (an `OutlinedField` + a `VerticalStackLayout` with
`BindableLayout` of suggestions placed **in the page flow under the field** — not a popup, so it works the same on both
platforms and is not hidden behind the Android keyboard), suggestion row 56 dp phone / 44 desktop, divider, highlighted
row `PrimarySoft`; `Views/QuickSearchKeysBehavior.cs` + `Platforms/Windows` part: `TextBox.PreviewKeyDown` maps ↑ / ↓ /
Enter / Esc to the commands (MAUI `Entry` has no key events) — **[to verify]** in a spike; fallback: Tab into the rows,
Enter on a focused row.
**AutomationIds:** `QuickSearchEntry`, `QuickSearchSuggestions`, `Suggestion_{barcode}`, `QuickSearchNoMatch`,
`QuickSearchCreateButton`, `QuickSearchClearButton`.
**Acceptance:** typing "nut" shows Nutella rows with name first and code last; ↓ ↓ Enter adds the second on Windows;
a USB scanner typing 13 digits + Enter adds the line without touching the list.

### S8 — Receive / Issue page rework

**Files:** `Views/StockDocumentPage.xaml(.cs)` (order of §4.5–4.8; camera card collapses while typing; `CameraStatusBanner`;
`QuickSearchBox` + quantity box; lines; *Supplier / Recipient and note* expander; `BottomActionBar`; *Discard draft* as
`ToolbarItem` `Order="Secondary"`), `Core/ViewModels/StockDocumentViewModel.cs`: inject `QuickSearchViewModel`; new
`CameraState`, `CameraMessage`, `RetryCameraCommand`, `IsTyping`, `EntryQuantity` (default 1, whole number ≥ 1, reset
after an add), `AddProduct(ProductSummary, quantity)` from a pick; `CommitHint` ("Add a product first" / "Fix the red
lines first"); Issue opened by a Driver → guard refusal, `Warning`, navigate Home. `StockDocumentDraft` gains
`Add(barcode, quantity)` (existing `Add(barcode)` = 1).
**Tests:** `StockDocumentViewModelTests` (pick adds N; camera state per scanner fake: unsupported / denied / active;
retry asks again; Driver on Issue refused and navigated Home; commit hint texts; typed wedge path unchanged),
`StockDocumentDraftTests` (`Add` with quantity merges), EndToEnd `StockDocumentFlowTests` new flow: *log in as
Storekeeper → Home → Receive → type "nut" → pick → quantity 12 → commit → Products shows 36*, and *Driver → Issue →
refused, nothing written*.
**Acceptance:** wireframes §4.5–4.8; commit button ≥ 56 dp phone / 48 epx desktop, full width on a phone, min 320 epx on
desktop; Undo after remove still works.

### S9 — Product form rework

**Files:** `Views/ProductFormPage.xaml`: barcode first (camera card on Android via `CameraScannerView`, stops after one
read; `OutlinedField` for typing), Open Food Facts lookup when a valid unknown code is entered (reuse `IProductLookup`
as `ScannerViewModel` does; keep the ODbL attribution text), name with duplicate guard (a `QuickSearchViewModel` in
"select" mode: a match shows "Already in the catalog: … [Open]" instead of adding), photo, `BottomActionBar` with *Add
product* / *Save changes*; Cancel = toolbar back (existing `BackButtonBehavior`) plus an outlined *Cancel* on desktop.
`ProductFormViewModel`: `ScanDetectedCommand`, `CameraState`, `LookupCommand`, `DuplicateText`, `OpenDuplicateCommand`.
**Tests:** `ProductFormViewModelTests` (scan fills barcode and stops the camera; lookup prefills name only when empty;
duplicate by name and by code shown; save still refused for non-managers).
**Acceptance:** §4.9 / §4.10.

### S10 — Products and other pages

`ProductsPage`: remove `StatusHeader` and the round `Fab`; Manager gets a `BottomActionBar` *Add product*; Map and
Characteristics move to the drawer; `EmptyState` for "no products" (role texts from design-system) and "nothing
matches"; pull to refresh (`RefreshView`) — gap from step 10. `SettingsPage`: Account card loses Sign out (it is in the
drawer footer) — keep one place. Tests: `ProductListViewModelTests` for empty texts per role and the refresh command.

### S11 — Documentation and verification

`docs/steps/11-design-revision.md` (template), README step log and **decision log** (proposed rows 94–100 below; 90 is
superseded by 94), `docs/design/tasks.md` marked superseded where needed, AppTests theme keys, UiTests ids, both builds,
S1 smoke check if a device exists, owner actions.

## 6. Proposed decision-log rows (README, written in S11)

| # | Decision | Why |
|---|----------|-----|
| 94 | Drawer (`FlyoutBehavior.Flyout`) on every device replaces the side panel and the bottom tabs (supersedes 90) | Owner request; the daily actions are on Home, so hiding the rest costs little (NN/g: combine visible + menu) |
| 95 | Home with two filled action tiles is the start page; the Driver sees one tile and a sentence | The counter user's two jobs are one tap away; a disabled tile would invite a useless tap |
| 96 | Receive / Issue are pushed on Home (`//home/receive`), opened by menu items, not Shell content | Back returns Home instead of closing the app |
| 97 | One big filled action in a bottom bar above the keyboard (`SafeAreaEdges … SoftInput`); Discard moves to the toolbar | Owner request; risky action away from the frequent one |
| 98 | Quick search: pure `ProductSuggestions` (name and code, ranked, ≤ 5 / 8), no debounce timer, list in the page flow | ≈ 10 products filter instantly; a popup would hide behind the Android keyboard |
| 99 | Enter in the search box: highlighted row → valid barcode → single suggestion, in that order | Keyboard-wedge scanners keep the old exact-barcode path |
| 100 | Outlined inputs via `OutlinedField` + `BorderlessEntry` handler mapping (filtered by subclass) | MAUI `Entry` has no outline or focus ring; a global mapping would change every entry |

## 7. Acceptance for the whole revision

1. Every requirement R1–R7 is traceable to a slice and a test or a recorded manual check.
2. `dotnet test Mobile_apps_L1_Model_view.Tests` green; ViewModel line coverage ≥ 60 %.
3. Windows and Android builds green; `scripts/build-apk.ps1` produced the APK used for the hand checks.
4. No `Shell.Current`, `DeviceInfo`, `MainThread` or other MAUI static in Core or ViewModels.
5. All new interactive controls have an `AutomationId` and, for icon-only ones, `SemanticProperties.Description`.
6. Each new colour pair is in design-system §4 with its ratio (none planned: tiles reuse `OnPrimary` on `Primary`).

## 8. Open questions for the owner

| # | Question | Proposal |
|---|----------|----------|
| O1 | Windows has a wide screen: drawer there too, or keep a permanent panel only on desktop? | Drawer everywhere, as asked; revisit after seeing it |
| O2 | Should *Receive* and *Issue* tiles differ in colour (e.g. Issue in the violet / a second hue) to tell them apart at a glance? | No: same green, different icon and label — colour must not be the only signal, and red is reserved for destructive actions |
| O3 | Manual quantity: a small "Qty" box next to the search (proposed), or ask the quantity after each pick? | Qty box (default 1); a dialog per item slows a delivery of 20 items |
| O4 | Emulator: may the implementer download an Android system image (≈ 1–2 GB) to create an AVD for screenshots, or will the owner use the phone? | Owner's phone via `build-apk.ps1 -Install` |
| O5 | Should *Products* stay in the drawer only, or also as a secondary button on Home? | Both (Home: *Find a product*) |
