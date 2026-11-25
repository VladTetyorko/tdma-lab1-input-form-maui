# Design revision 3: seen on a device, then polished

Status: **plan, not implemented.** It builds on [`revision-2.md`](revision-2.md) and
[`design-system.md`](design-system.md). Where they disagree, this file wins. The earlier owner requirements still
hold:

- green light theme and violet dark theme, minimal shadows, no neon;
- a drawer opened by a hamburger and closed by a cross;
- two big *Receive* / *Issue* buttons on Home;
- one big action button at the bottom of a page;
- the camera comes first;
- quick search shows the product first and the code second.

## 0. How the app was checked

This is the first time the app was started by us. Branch `design`, APK `artifacts/apk/StockRoom-1.0-6636fb0.apk` (debug-signed
Release, built by `scripts/build-apk.ps1`).

| Item | Value |
|------|-------|
| Emulator | AVD `stockroom`: Pixel 7 profile, 1080×2400 at 420 dpi; `system-images;android-35;google_apis;x86_64` (rev 9, installed with the owner's approval, no new licence prompt); 4 cores, 3 GB RAM; WHPX acceleration usable |
| GPU | `-gpu swiftshader_indirect`. With `-gpu host` and `-no-window`, `screencap` returns a **white** picture, so host GPU cannot be used for screenshots |
| Host load | The host was at about 97 % CPU from other programs, so the emulator was starved. Android showed "System UI / StockRoom isn't responding" dialogs for the first minutes after every boot. The ANR traces show the app's main thread idle in `Looper.pollOnce`, and other system apps hung the same way. **These dialogs are host starvation, not an app bug** |
| Cold start | `am start -W` took 22 s cold and 7.6 s warm on this starved emulator. The log shows `MapsInitializer` and Google Play Services Dynamite loading **at app start** (`UseMauiMaps`), then `Choreographer: Skipped 761 frames`. This is not proof of a slow phone, but startup work is worth measuring on the owner's phone (finding F9) |
| Driven with | `adb shell input`, `uiautomator dump`, `screencap`; helper [`scripts/emulator-screens.ps1`](../../scripts/emulator-screens.ps1) |
| Screenshots | [`screenshots/r2/`](screenshots/r2) (540 px wide, 34 files, ≈ 2.6 MB) |

Covered: login, the wrong-password message, Home (Manager and Driver), the drawer (both roles, both themes), Receive
(camera, suggestions with the keyboard open, a line, details, overflow menu, confirm, done), Issue (out-of-stock
suggestion, over-stock line, remove and undo), Products (light, dark, a search with a typo), product detail with
history, the new-product form with the duplicate warning, the scanner page, Settings (light, dark), Characteristics
and Map.

Not covered: Windows (not run, by rule), a real camera reading a real barcode, the Open Food Facts prefill (the
emulator was effectively offline), and the Storekeeper role (its screens are the Manager's minus product management).

## 1. Findings from the screenshots

Severity: **P0** = crash or data risk; **P1** = broken function or the owner's first impression; **P2** = polish.

### 1.1 Crashes and broken functions (fix first)

| # | Sev | Finding | Evidence | Cause, from code or log |
|---|-----|---------|----------|-------------------------|
| F1 | **P0** | **The app crashes when Receive / Issue / New product opens on a device without a usable camera.** It happens right after the camera permission is granted: `FATAL EXCEPTION: main … androidx.camera.core.CameraUnavailableException: Device reporting less cameras than anticipated … Available cameras: 0`, thrown in `ZXing.Net.Maui.CameraManager.Connect`. Restarting the app goes back to Home (no crash loop) | [05](screenshots/r2/05-receive-camera-permission.png) → [06](screenshots/r2/06-receive-crash-no-camera-launcher.png) (launcher, app gone) | `Services/CameraBarcodeScanner.cs` has `IsCameraSupported => true` hard-coded. `CameraScannerView` creates `CameraBarcodeReaderView` without checking that a camera exists, and ZXing rethrows the CameraX init failure on the UI looper, where no `try` can reach it. The same error can occur on real phones when the camera is held by another app or fails to start ("Retrying initialization might resolve temporary camera errors"). Tablets without a back camera and Chromebooks are also affected. This breaks the AGENTS "fail soft" rule |
| F2 | P1 | **Pushed pages show the hamburger (☰) instead of a back arrow.** The icon is announced as "Navigate up" and works as Back, but it looks like the menu. Affected: Receive, Issue, product detail, product form, Map, Characteristics, Scanner | [09](screenshots/r2/09-product-detail.png), [19](screenshots/r2/19-receive-camera-dark.png), [17](screenshots/r2/17-characteristics-dark.png), [32](screenshots/r2/32-scanner-lookup.png) | `AppShell.xaml.cs:42` sets `FlyoutIcon = hamburger`. Pages that have a `BackButtonBehavior` with a `Command` and no `IconOverride` inherit that icon on Android |
| F3 | P1 | **The status bar stays green in the dark theme.** With the drawer open, half of it is black and half green | [14](screenshots/r2/14-home-dark.png), [13](screenshots/r2/13-drawer-dark.png) | It comes only from `Platforms/Android/Resources/values/colors.xml` (`colorPrimaryDark`). Nothing follows the app theme |
| F4 | P1 | **The bottom action bar hides behind the keyboard.** This is decision 97 of revision 2. After a pick the keyboard stays open, so the line list and the *Receive N units* button stay covered until Back is pressed | [20](screenshots/r2/20-receive-suggestions-keyboard-dark.png) (the dump puts `CommitDocumentButton` at y = 2174, under the keyboard), [21](screenshots/r2/21-receive-one-line-dark.png) | `SafeAreaEdges="…, SoftInput"` on the page grid does not move the bar on Android with the default `WindowSoftInputModeAdjust`, and nothing hides the keyboard after a pick |
| F5 | P1 | **Settings shows an empty "Recent activity" card** (a header and nothing else) | [11](screenshots/r2/11-settings.png) | `SettingsPage.xaml:20` hosts `RecentActivityPanel`, and nothing loads it there. It does not belong on Settings at all |
| F6 | P1 | **The search has no tolerance.** "nutela" finds nothing, while the Android keyboard itself suggests "Nutella" | [16](screenshots/r2/16-products-search-typo-dark.png) | `TextMatch.Contains` / `StartsWith`: one whole substring, no tokens and no fuzziness (owner's request, §5) |
| F7 | P2 | Text bugs: "Receive 12 units of **1 products**?", "Issue **1 units**", "Draft · 1 line · **1 units**", "on hand 0 → **-1**", "Only **0** on hand", "+1" on *Issue*. The success message is only "Saved R-20261125-0001" | [25](screenshots/r2/25-receive-confirm-dialog-dark.png), [28](screenshots/r2/28-issue-over-stock.png), [26](screenshots/r2/26-receive-done-dark.png) | String building in `StockDocumentViewModel` (no plural helper) |
| F8 | P2 | After a line is removed there are **two Undo controls** (an inline link and a snackbar). The snackbar covers the bottom button and has a shadow. The status line still says "Heinz … +1" after the removal | [29](screenshots/r2/29-issue-removed-undo.png) | Undo is shown twice, and `DocumentStatus` is not cleared |
| F9 | P2 | Startup does map and Play Services work before the first page. Leaving the new-product form discards what was typed **without asking** | log in §0; form → Back | `UseMauiMaps` at start; `ProductFormViewModel.Cancel` has no dirty check |

### 1.2 Per screen: what looks wrong

**Title bar, on every screen (the owner's "crooked" first impression).** [03](screenshots/r2/03-home-manager.png), [08](screenshots/r2/08-products-manager.png)

- **Status bar and bar differ.** The status bar is saturated green and the bar under it is white or near-black. They read as two unrelated stripes.
- **The title floats.** It starts 72 dp from the edge (Android's `contentInsetStartWithNavigation`). The page content starts at 16 dp. So the title lines up with neither the hamburger nor the content: three different left edges.
- **The bar is not symmetric.** The left icon has a 48 dp touch area and a 24 dp glyph. The right *Scan* icon is a toolbar item, a different, bolder glyph that sits lower, at 1016,209 against 74,210.
- **Too much space under the bar.** The gap before the first content is 48–80 dp, and it differs per page: Settings 24, Home 48, Products 80, where the space is kept for a `SyncChip` that is hidden.
- **Login shows the app name twice:** in the bar and in the card. [01](screenshots/r2/01-login.png)

**Login** [01](screenshots/r2/01-login.png), [02](screenshots/r2/02-login-filled-keyboard.png), [33](screenshots/r2/33-login-error.png)

- The fields have underlines only. They are not the `OutlinedField` used elsewhere.
- The card is vertically centred, so the keyboard covers *Sign in* and the user must press Back to reach it.
- No "show password" toggle. The *Sign in* button does not read as a primary button: it is outlined and greyed until both fields are filled.

**Home** [03](screenshots/r2/03-home-manager.png), [14](screenshots/r2/14-home-dark.png), [34](screenshots/r2/34-home-driver.png)

- Good: the tiles are big, high-contrast and readable in both themes. Keep them.
- *Find a product* / *Add product* are two outlined buttons of different widths, left-aligned, under full-width tiles. This is the main symmetry break. The Driver has one lone left button.
- The tiles sit at the top and the bottom third is empty. Revision 2 placed the tiles mid-screen, low centre, for reach.
- In Recent activity, the last row puts the name and the "who, when" text on one line while the other rows use two lines, which looks broken. The ± numbers are not coloured. The dates mix "01.01 10:22" here with "2026-01-01 08:20 UTC" on the detail page.

**Drawer** [04](screenshots/r2/04-drawer-manager.png), [13](screenshots/r2/13-drawer-dark.png), [35](screenshots/r2/35-drawer-driver.png)

- Good: the header with the × button, the selected row, the footer with sync status and *Sign out*. Role filtering works: the Driver sees no *Issue* and no *Characteristics*.
- Light theme: the drawer's status-bar strip is white, so the white status icons disappear. Dark: see F3.

**Receive / Issue** [19](screenshots/r2/19-receive-camera-dark.png)–[29](screenshots/r2/29-issue-removed-undo.png)

- Good: camera first, the search with a quantity box, suggestions (name first, stock on the right, code last), a big bottom button that says how many units, the disabled state with a reason, the red over-stock warning, the details expander, *Discard draft* in the overflow menu.
- "Draft is empty" is said **twice**: in the summary line and in the empty state.
- The suggestion details show "0.4 · Ferrero". A bare number without its characteristic name or unit means nothing.
- Each line is `[−] ___12 [+] [×]` with an underline entry. It is not a stepper, the parts are spaced unevenly, and the × is in the danger colour right next to + (a mis-tap risk).
- *Supplier (optional) and note* looks like a link: no chevron, no frame.
- The confirm dialog is the plain, unthemed Android `AlertDialog` (grey, "1 products").
- *Issue* lets you pick an "Out of stock" product with no hint at pick time.
- The success state is a plain line of text. There is no green confirmation, no "+12 Nutella 400 g → 48 on hand", and no sound.

**Products table** [08](screenshots/r2/08-products-manager.png), [15](screenshots/r2/15-products-dark.png)

- Good (the owner agrees): a header band, the sort arrow, dividers, the barcode under the name, numbers aligned right.
- The **headers are centred while their numbers are right-aligned**, so "Qty" and "Weight (kg)" stand over nothing.
- "Out of stock" is text inside a numeric column. It widens the column and squeezes the names into awkward wraps ("Danone Skyr 140 / g").
- The search is the native `SearchBar`. It looks different from every other field and is indented differently.
- *Extra column* is a bare label next to an underlined picker, which reads like stray text.
- The note "A product with stock history cannot be deleted…" is always shown: noise, and it pushes the 7th row under the bar.

**Product detail** [09](screenshots/r2/09-product-detail.png), [10](screenshots/r2/10-product-detail-history.png): **the weakest screen.**

- Four buttons touch each other in one row with different widths: *Save* (outlined), *Record In/Out* (filled), *Receive*, *Issue*. A ghost *Back* sits under them.
- The characteristics are always-editable underlined entries, with "number" as a placeholder.
- There is no photo area, only two *Take photo* / *Pick photo* buttons at the top.
- History is shown in UTC.

**New product** [30](screenshots/r2/30-product-form.png), [31](screenshots/r2/31-product-form-duplicate.png)

- Good: camera first, then barcode, then name. The camera hides once a code is typed, and the bottom button gives its reason.
- The *Name* field shows a 🔍 search icon. It is the quick search reused, but to the user it is a name field.
- The duplicate warning ("Already in the catalog … [Open]") is under the drop-down and under the keyboard, so it is not seen.
- The photo buttons are left-aligned and of unequal width.

**Scanner, Characteristics, Map, Settings** [32](screenshots/r2/32-scanner-lookup.png), [17](screenshots/r2/17-characteristics-dark.png), [18](screenshots/r2/18-map-dark.png), [11](screenshots/r2/11-settings.png)

- These still use the step-10 style: underline entries, small left-aligned buttons, redundant *Back* buttons.
- The Settings switches sit right after their labels instead of at the right edge. The card top paddings differ.
- Map is an empty page with two sentences and no empty-state icon.

## 2. Debate: the counter clerk against the designer-engineer

**Clerk (Petro, at the counter, gloves, a driver waiting):** "I open the app 60 times a day. I need to see *Receive* and
*Issue* at once, scan, type a number, press one big button and see *done*. If I type 'nutela' I want Nutella, not
'nothing found'. When it beeps I don't want to look at the screen. And don't ask me 'Receive 12 units of 1 products?'
every time: if I'm wrong, let me undo."

**Designer-engineer:**
- "The beep and the instant search: agreed. Removing the confirmation: not as asked. `Operation` is never edited or deleted (AGENTS), so 'undo' after commit means a *reversing document*. That is a domain feature, not polish. Until then the confirmation is the only safety net. We can make it one tap cheaper: a themed bottom sheet that shows the lines, with the action on the same spot as the button you just pressed."
- "Fuzzy search: also agreed, but a fuzzy match must **never** be added by itself. A USB scanner types 13 digits and Enter and must hit the exact code, never 'the closest code'."

**Clerk:** "The top bar looks crooked, and on the product page I can't tell which button is the main one."

**Designer-engineer:** "That's the cheapest big win. One `AppBar` component, a symmetric 48 | title | 48 grid, the status
bar in the same colour, one 16 dp content edge everywhere. The product page gets *one* filled action (*Receive /
Issue* as a pair, equal width) and editing moves behind an *Edit* button."

**Clerk:** "Tables are fine, just make them neat. Do we need a big grid library?"

**Designer-engineer:**
- "No, for now (§4). Our table already has a sticky header, sorting by any characteristic, role-aware actions and `AutomationId`s on every cell, for about ten rows."
- "The libraries that do more either have no Windows target (DevExpress), need a registered licence key (Syncfusion), cost money (Telerik), or are pre-1.0 (KumikoUI). The one free classic, Maui.DataGrid, has not shipped a release since January 2025 and has no net10 build."
- "What the table lacks is alignment rules, not a library."

**Clerk:** "And when the camera doesn't work?"

**Designer-engineer:** "Today it **crashes** (F1). That is the first thing to fix. Checking camera hardware before creating the reader is the fix, and the 'Camera not available, type a name or code' banner already exists."

**Decisions**

1. F1 first. Then the shell polish: `AppBar`, status bar, back arrow, keyboard.
2. A symmetry rule set (§6), applied page by page.
3. Own table, polished. Grid libraries: no adoption now. A paid or registration licence is the owner's call.
4. Tolerant search with tokens, n-grams and an edit distance of 1 or 2, shared by the quick search and the table filter. Enter never picks a fuzzy match.
5. Scan sound in addition to the existing vibration. Grammar and number helpers. A themed confirm sheet.
6. Later, each needing a written domain decision: a reversing document (undo after commit), a minimum stock level, packs (one scan = N units), CSV export, label printing.

## 3. Must-have features from forums and reviews

These sources were read on 2026-11-25 by a research sub-agent. Reddit, Capterra, G2, GetApp and SoftwareAdvice refused
automated access, so anything taken only from their search snippets is marked **[unverified]**.

| # | Feature | What users say (source) | StockRoom today | When |
|---|---------|-------------------------|-----------------|------|
| M1 | Feedback by **sound and vibration**, including for errors | A driver with poor eyesight needed a tone ([Breww forum](https://community.breww.com/t/mobile-app-barcode-scan-sound-vibration/1920)). Error sound and vibration so "the user doesn't need to be viewing the mobile device screen" ([CartonCloud](https://www.cartoncloud.com/resources/error-notification-vibration-and-sound-for-android)). Scandit: sight, sound and haptics ([Scandit](https://www.scandit.com/blog/scanning-at-scale-ux-insights/)) | Vibration only (`MauiScanFeedback`) | **Now** (S5) |
| M2 | Enter the quantity after a scan, not one scan per unit | Thousands of identical items, edit the quantity with the keypad ([Odoo forum](https://www.odoo.com/ro_RO/forum/suport-1/is-there-a-way-to-manually-update-the-quantity-while-using-barcode-function-in-v-13-173336)) | Yes (Qty box and line stepper) | Have. Polish the stepper (S6) |
| M3 | A repeat scan adds to the same line | A roadmap item of a native Odoo 19 barcode module ([apps.odoo.com](https://apps.odoo.com/apps/modules/19.0/do_stock_barcode_native)) | Yes (`StockDocumentDraft` merges) | Have |
| M4 | Packs and cases: one scan = N units | "When I scan the Lot barcode it increments the quantity by one" with 20-unit packs ([Odoo forum](https://www.odoo.com/hi_IN/forum/help-1/understanding-inventory-transfers-with-barcode-app-using-lot-and-product-packaging-264670)) | No | Later (domain change) |
| M5 | Movement history and an audit trail | Inaccurate stock and "no real way of seeing what the issue could be" ([Shopify community](https://community.shopify.com/t/issues-with-shopify-inventory-looking-for-alternative-solution/676359)) | Per-product history; recent activity on Home | Now: local time and a consistent format (S6). Later: a full *History* page with filters |
| M6 | Undo as a reversing entry | "Creates a new inventory adjustment and links it to the original… does not erase history" ([EasyScan](https://easyscan.506.io/doc/undo-inventory-changes)) | Undo only inside the draft | Later (fits "Operation is immutable") |
| M7 | Reliable low-stock alerts | Sortly alerts are "very hit and miss" ([Trustpilot, Sortly](https://www.trustpilot.com/review/sortly.com?page=7)). inFlow sends daily alerts ([Business.org](https://www.business.org/finance/inventory-management/inflow-review)) | Only "Out of stock" | Later (minimum stock per product = new column) |
| M8 | Offline scanning, sync later | Sync problems between desktop and mobile ([App Store, Sortly](https://apps.apple.com/us/app/sortly-inventory-simplified/id529353551)). Offline scanning praised ([Business.org Sortly review](https://www.business.org/finance/inventory-management/sortly-review)). Zoho offline wishes **[unverified]** | Yes (local first, sync queue) | Have |
| M9 | Search and sort that always work, and fast | Search, sort and filter work "some of the time" (Trustpilot, Sortly, above) | Exact substring only | **Now** (S3, S4) |
| M10 | Photos on items | Praised in Sortly reviews (Trustpilot, above) | Yes, but hidden behind two buttons | Now: photo area on detail (S7) |
| M11 | Labels, QR, label printing | QR labels praised. Label-printing requests in GetApp / Zoho reviews **[unverified]** | Deep-link QR is parsed | Later |
| M12 | CSV export | "How to share as a CSV" ([App Store](https://apps.apple.com/us/app/id1210420949?l=en-us)). "Export good but not great" **[unverified]** | No | Later |
| M13 | A large scan target and quick confirmation | "A large, ergonomic touch area is a must"; a pause of about 150 ms before closing (Scandit, above) | Camera card plus a big button | Have. Add a short success banner on the preview (S5) |

## 4. Table and grid libraries for .NET 10 MAUI

Checked on nuget.org and GitHub on 2026-11-25 (sources at the end of the section).

| Package | Latest | net10 Android + Windows | Licence | Maintenance | Verdict |
|---------|--------|-------------------------|---------|-------------|---------|
| `akgul.Maui.DataGrid` | 4.0.6, 2025-01-02 | **No net10 build** (net8 / net9 only; net10 is "computed"); Android 24+ and Windows 10.0.19041 per the README | MIT | Commits on `main` in October 2026 (Dependabot bump to MAUI 10.0.110), but **no NuGet release since January 2025** | Possible, but a net9 binary on net10 needs a spike. Its features (sort, templates, row tap) are what we already have |
| `Syncfusion.Maui.DataGrid` | 35.2.13, 2026-11-23 | Yes (net10.0-android36.0, net10.0-windows10.0.19041) | Commercial EULA. The free **Community Licence** requires < $1 M revenue, ≤ 5 developers, ≤ 10 employees, and a sign-up form. Every NuGet build needs a **licence key registered in code**, otherwise a licence banner shows. Pulls about 12 other Syncfusion packages | Very active | The most capable free-for-eligible option. **The owner's decision** (registration, key handling, dependency weight) |
| `DevExpress.Maui.DataGrid` | 26.1.6, 2026-10-23 | **No Windows** (android and iOS only) | The free offer **ended on 2024-12-31**; current versions are paid | Active | Ruled out |
| `Telerik.UI.for.Maui` | 16.0.0, 2026-11-11 | Yes | Paid | Active | Ruled out (cost) |
| `KumikoUI.Maui` | 0.2.0, 2026-11-12 (1.0.0-preview.1) | Yes | MIT | New and very active; about 1.9 k downloads | Interesting (frozen columns, filtering), but pre-1.0, and it draws with SkiaSharp on one canvas. Our inference, not checked: that likely loses native accessibility and per-cell `AutomationId`s, which our UI tests and AGENTS §2a need |
| `Monsalma.Maui.Controls` | 1.0.7, 2025-03-20 | net9 only | MIT | Dormant | Ruled out |
| CommunityToolkit.Maui | — | — | — | — | No data grid **[unverified, as far as the research found]** |

**Honest conclusion: our own table stays.**

- With about ten rows we need: a sticky header, sort by any characteristic, one extra column on a phone, all columns on desktop, row tap, role-aware swipe or buttons, and `AutomationId`s. The current `CollectionView` table does all of that.
- What the owner sees as "needs polishing" are alignment and density problems (§1.2), not missing grid features.
- A library would add a dependency (and for Syncfusion a licence key) without fixing those, because they are our column-width and alignment choices.
- **Revisit** if the catalogue grows past a few hundred rows, or if the owner wants inline cell editing, column resizing or frozen columns on Windows. Then spike Syncfusion (if the owner accepts the Community Licence) against KumikoUI 1.0 (if it is released by then).

Sources:

- [nuget akgul.Maui.DataGrid](https://www.nuget.org/packages/akgul.Maui.DataGrid), [GitHub](https://github.com/akgulebubekir/Maui.DataGrid), [commits](https://github.com/akgulebubekir/Maui.DataGrid/commits/main)
- [nuget Syncfusion.Maui.DataGrid](https://www.nuget.org/packages/Syncfusion.Maui.DataGrid), [community licence](https://www.syncfusion.com/sales/communitylicense), [licensing overview](https://help.syncfusion.com/maui/licensing/overview)
- [nuget DevExpress.Maui.DataGrid](https://www.nuget.org/packages/DevExpress.Maui.DataGrid), [DevExpress free offer](https://devexpress.com/xamarin-free)
- [nuget Telerik.UI.for.Maui](https://www.nuget.org/packages/Telerik.UI.for.Maui)
- [nuget KumikoUI.MAUI](https://www.nuget.org/packages/KumikoUI.MAUI)
- [nuget Monsalma.Maui.Controls](https://www.nuget.org/packages/Monsalma.Maui.Controls/)

### 4.1 Own table: rules for the polish (slice S6)

| Rule | Now | Change to |
|------|-----|-----------|
| Header alignment = cell alignment | Headers centred, numbers right | Header text aligned like its column (`End` for numbers), sort arrow **before** the text in numeric columns ("▲ Qty") so the right edges line up |
| Fixed numeric widths | Width from `TableColumn.Width`, but "Out of stock" text widens it | Quantity is always a number. 0 shows as **0** in `Danger` with a small "Out" chip **under the name** (not in the number column). Widths: Qty 56, extra column 72, on a phone |
| Name column | Wraps at about 14 characters | `MaxLines=2`, `TailTruncation`; the barcode caption on one line in tabular digits |
| Row height | 98 px or more, varying | 56 dp minimum, 72 dp with two lines; the same padding (16 / 12) as `OutlinedField` |
| Search field | Native `SearchBar` | `QuickSearchBox`-style `OutlinedField` in filter mode (no drop-down), full width, same 16 dp edge |
| Extra column picker | Bare label + picker | A chip "Column: Weight (kg) ▾" right-aligned **in the header band**, above its own column |
| Permanent footnote | Always visible | Shown only in the delete refusal message (it already exists as a status) |
| Search highlight | None | Matched parts of the name in `TextBodyStrong` + `PrimaryText` (from the §5 matcher) |

## 5. Partial-match search

### 5.1 What the owner asked

"Code search matches too directly. Split into small parts like normal search: match runs of 2, 3, 4 consecutive
characters against what is in the database, with sensible ranking and highlighting."

### 5.2 Design: `ProductMatcher` (Core, pure, no I/O)

New files in `StockRoom.Core/Services/Search/`:

- `SearchText.cs`: normalisation.
- `SearchQuery.cs`: a parsed query.
- `ProductMatcher.cs`: scoring and highlight ranges.
- `MatchResult.cs` (record): `Product`, `Score`, `Tier`, `IsFuzzy`, `NameRanges`, `CodeRanges`.
- `TextRange.cs` (record struct): `Start`, `Length`.

`TextMatch` stays for the old callers until S4 removes them. `ProductSuggestions` and `ProductSearch` become thin
users of `ProductMatcher`, so there is one rule (DRY).

**Normalisation.**
- Lower-case invariant. Remove diacritics (`NormalizationForm.FormD` and drop `NonSpacingMark`).
- Every character that is not a letter or digit becomes a space. Collapse spaces.
- Keep a **map from normalised index to original index**, so highlights land on the original text.
- Product name words and the barcode are prepared once, when the list is loaded (`ProductIndex`, built in
  `QuickSearchViewModel.LoadAsync` / `ProductListViewModel`). With about ten products this costs nothing, and it still holds
  for thousands.

**Query.** The query is split into tokens on spaces. A product matches when **every token** matches somewhere (AND). The
product's score is the sum of its token scores. Each token is classified as one of:

- **digit token**: only digits, compared with the **barcode** (D tiers) and also with the name words by the exact
  tiers L0–L2 only, because "500" usually means "500 g";
- **letter token**: has a letter, compared with the **name words** (and, in the table only, with the characteristic values).

**Digit tokens: never fuzzy.** A code is either right or it is not, and a fuzzy code would add the wrong product.

| Tier | Rule | Minimum length | Score |
|------|------|----------------|-------|
| D0 | Barcode equals the token | any | 1000 |
| D1 | Barcode starts with the token | 1 | 800 |
| D2 | Barcode **ends** with the token (people read the last digits off a label) | 3 | 700 |
| D3 | Barcode contains the token | 4 | 500 |

**Letter tokens: exact first, then n-gram and edit distance.** "Fuzzy" in the table below means a typo-tolerant match.

| Tier | Rule | Minimum token length | Score |
|------|------|----------------------|-------|
| L0 | The whole name starts with the token | 1 (single letters only when the name starts with it) | 600 |
| L1 | A name **word** starts with the token | 2 | 500 |
| L2 | The name contains the token (substring) | 3 | 350 |
| L3 (fuzzy) | **n-gram overlap**: the share of the token's character n-grams found in one name word, n = 2 for tokens of 3–4 letters, n = 3 for 5+ letters, with one padding space at the word start (so "nu" weighs the first letters). Accepted when the share ≥ 0.6 | 3 | 100 + 200 × share |
| L4 (fuzzy) | **Damerau–Levenshtein distance** between the token and a word's prefix of the same length: ≤ 1 for tokens of 4–7 letters, ≤ 2 for 8+ | 4 | 150 − 40 × distance |

A token takes its best tier. L3 and L4 set `IsFuzzy`.

Examples on the seed data:

- "nutela": L4 (distance 1 to "nutell") and L3 (3-grams "nut", "ute", "tel", "ela"; 3 of 4 are in "nutella") → *Nutella 400 g* … fuzzy.
- "skyr": L1. "40": D1 for barcodes starting with 40 (`4008400402222`) and L1 for "Nutella 400 g"; barcode tiers score higher.
- "003": D2 (`3017620422003`). "3017": D1.

**Ranking.**
1. Score, descending.
2. Non-fuzzy before fuzzy, at equal score.
3. On *Issue*: in stock before out of stock.
4. Name, `OrdinalIgnoreCase`.

At most 5 rows on a phone and 8 on desktop (unchanged). Fuzzy rows appear **only when there are fewer exact rows
than the maximum**, and get a caption "Similar name" so the user sees why they are there.

**Thresholds for short queries.**
- 1 character: digits → D1 only; a letter → L0 only.
- 2 characters: no fuzzy, no substring.
- 3+: substring and n-gram.
- 4+: edit distance.

This keeps "a" or "1" from listing everything and keeps noise low.

**Highlight.**
- Exact tiers mark the matched span.
- L3 marks the union of the matched n-gram spans, with spans 1 character apart merged.
- L4 marks the word prefix that was compared.
- The ranges are mapped back to the original text.
- The view builds a `FormattedString`: matched spans in `TextBodyStrong` with `PrimaryText` colour, the rest normal. Colour is never the only signal; the weight changes too.
- A suggestion row highlights the name and the code. The table highlights the name.

**Enter and scanners (unchanged contract, made explicit).** `QuickSearchViewModel.Submit`:

1. A highlighted row → pick it.
2. The text parses as a barcode (`ScanPayloadParser`) → `SubmittedText`, the **exact** barcode path. A USB keyboard-wedge scanner always lands here, also for unknown codes.
3. Exactly one suggestion, and it is **not fuzzy** → pick it.
4. Otherwise nothing.

Typing never picks anything. **A fuzzy match is never added without a tap or an arrow selection.**

**Not now (YAGNI, recorded):**
- Cyrillic ↔ Latin keyboard-layout mistakes ("тгеуддф" → "nutella").
- Transliteration.
- Phonetic matching.
- Weighting by how often a product is used.
- The library `FuzzySharp` (MIT, fuzzywuzzy port). Not adopted: it returns scores but no match positions for highlighting, and the rules above are about 150 lines we can test. Its API was **not checked** in this round.

### 5.3 Tests (`Mobile_apps_L1_Model_view.Tests/Services/Search/`)

- `SearchTextTests`:
  - diacritics ("Café" → "cafe"), punctuation becomes a space;
  - the index map points back to the original text with a "ü" and a double space.
- `ProductMatcherTests` (seed-like data, one `[Theory]` per tier):
  - D0–D3, including "a 2-digit suffix does not match";
  - L0–L4: "nutela" finds Nutella as fuzzy, "nutnut" finds nothing, "sky" finds Skyr as L1;
  - AND across tokens: "nut 500" → only *Nutella 500 g*;
  - thresholds: "n" → only names starting with n; "nu" → no fuzzy rows;
  - ranking: exact before fuzzy at equal score; out of stock after in stock on Issue;
  - max rows; fuzzy rows only to fill the remaining places.
- Highlight ranges: "tel" in "Nutella" → (3, 3); a fuzzy match → merged ranges; ranges inside the original string with diacritics.
- `ProductSuggestionsTests` (adapted): `IsFuzzy` flows into `ProductSuggestion`; the caption "Similar name".
- `QuickSearchViewModelTests`:
  - Enter with one **fuzzy** suggestion → nothing;
  - Enter with one exact suggestion → picked;
  - 13 digits + Enter → `SubmittedText` even when a fuzzy name row is shown.
- `ProductSearchTests` (adapted): the table filter uses the matcher, also on characteristic values, and keeps the user's column sort.
- EndToEnd: *log in → Receive → type "nutela" → no auto add → tap the suggestion → line added*.

## 6. Visual system: symmetry and alignment rules

These are added to `design-system.md` in slice S2 and checked on every screen in the final acceptance.

1. **One content edge: 16 dp on a phone, 24 on desktop.** The title, fields, cards, table and bottom bar all start and end there.
2. **Top bar = symmetric grid `48 | * | 48`** (desktop `48 | * | auto`):
   - left: menu ☰ on top-level pages, back ← on pushed pages;
   - centre: the title, centred on a phone and left at the content edge on desktop;
   - right: one action (Scan) or an empty 48 dp slot, so the title stays centred;
   - height 56 dp. The bar colour equals the page `Background` and equals the status-bar colour. No shadow, a 1 px `Border` line only when the content scrolls under it.
3. **Pairs are equal.** Two buttons side by side always share the width 50 / 50 with an 8 dp gap (Home *Find* / *Add*; detail *Receive* / *Issue*; photo *Take* / *Pick*). A single secondary button is full width.
4. **One filled button per screen** (Home tiles excepted, D10). It sits in the bottom bar.
5. **Vertical rhythm** 8 / 16 / 24. The first content block starts 8 dp under the top bar on every page.
6. **Controls that belong together sit inside one frame:**
   - a stepper is `[ − | 12 | + ]` in one outlined pill, 48 dp high;
   - *Remove* is an icon at the far right, separated by a divider and 16 dp, in `TextSecondary`; it turns red on press or confirm.
7. **Numbers right, text left, headers like their column.**
8. **Empty pages** use `EmptyState`, centred in the remaining space (Map, a search with no match, an empty draft). Only **one** "empty" message per page.
9. **Dialogs and sheets** are themed. `alerts.ConfirmAsync` gets a bottom-sheet implementation (`IConfirmSheet`) on phones: its title, its lines and the filled action on the bottom-bar position.

## 7. Implementation slices (in order)

Each slice follows AGENTS §2b: contract first, tests with the slice, `dotnet test Mobile_apps_L1_Model_view.Tests`
green, commit `[Design] …`. Build Windows and Android before the last commit of a UI slice. Platform-free code goes
to `StockRoom.Core`. **After every UI slice, run the emulator check of §8 for the screens it touched.**

### S1: Camera never crashes the app (F1), first

**Files**

- **Core:**
  - `Services/ICameraAvailability.cs` (`Task<bool> HasCameraAsync()`), or extend `IBarcodeScanner` with `Task<CameraState> GetCameraStateAsync()`. Pick the smaller change: `IsCameraSupported` becomes a real check behind the interface.
  - `CameraMessages` gains "Camera could not start. Type a name or code. [Try again]".
- **App:**
  - `Services/CameraBarcodeScanner.cs`: on Android, `IsCameraSupported` = `PackageManager.HasSystemFeature(PackageManager.FeatureCameraAny)` **and** `CameraManager.GetCameraIdList().Length > 0`; inside a `try`, `false` on any exception, with a `Warning` log.
  - `Views/CameraScannerView.cs`: create the reader only when the check passes. On a start failure reported by ZXing (`CameraBarcodeReaderView` events, if any), raise a `Failed` command to the ViewModel.
  - `Platforms/Android/MainApplication.cs`: an `AndroidEnvironment.UnhandledExceptionRaiser` handler that marks **only** `AndroidX.Camera.Core.CameraUnavailableException` / `InitializationException` as handled, logs `Error` and tells the scanner state service. This is the last-resort net for a camera that disappears at run time. Everything else stays unhandled and goes to Sentry as before.

**Tests:** `CameraMessagesTests` (the new state); `StockDocumentViewModelTests` and `ProductFormViewModelTests` with a fake
scanner whose camera is unavailable → no `IsCameraActive`, the banner text, the search field focused;
`StartupGuardTests` unchanged.

**Acceptance:** on the emulator with `-camera-back none`, Receive, Issue and New product open, show the "Camera not
available" banner and work by typing. `adb logcat` has no `FATAL EXCEPTION`. With `-camera-back emulated` the preview
works as before.

### S2: AppBar, status bar, back arrow, keyboard (F2, F3, F4; the owner's first impression)

**Files**

- `Views/Components/AppBar.xaml(.cs)`: the `48 | * | 48` grid of §6.
  - The left button binds to `AppBarViewModel.IsRoot` → ☰ (`AutomationId="MenuButton"`, which also gives the UI tests a stable hamburger id) or ← (`BackButton`).
  - The title comes from a bindable property; the right action is a slot.
  - Every page sets `Shell.NavBarIsVisible="False"` and puts `<components:AppBar />` in row 0.
  - The Android hardware Back keeps working through Shell.
- `INavigationService.CanGoBack` (Core) for `IsRoot`.
- Status bar: `CommunityToolkit.Maui` `StatusBarBehavior` (the package is already referenced, 16.0.0) on each page, or once in a base page style. Colour `Background` and dark icons in light, `Background` and light icons in dark; it follows `ThemeService` changes.
- Keyboard:
  - `Application.Current.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize)` in `App`, with the .NET 10 note from the revision-2 sources (`SafeAreaEdges="All"` on the page grid if `Resize` alone is not enough). Verify on the emulator.
  - After a pick on a phone, the quick search calls a new Core `IKeyboard.Hide()` (app implementation: `Entry.HideSoftInputAsync` from CommunityToolkit, or `Unfocus`).
- `design-system.md`: §6 rules.
- `docs/design/tasks.md`: mark the replaced shell items.

**Tests:** `AppBarViewModel` (or `NavigationService` fake) `IsRoot` per route; `QuickSearchViewModelTests` (a pick hides
the keyboard through the fake `IKeyboard`); UiTests ids updated (`MenuButton`, `BackButton`).

**Acceptance:** screenshots of Home, Products, Receive and product detail in both themes:

- the status bar has the same colour as the bar;
- ☰ only on Home, Products and Settings; ← elsewhere;
- the title centred on a phone;
- the *Receive N units* button visible above the keyboard while typing;
- the keyboard closed after a pick.

### S3: Search core (F6, §5)

**Files**

- `StockRoom.Core/Services/Search/*` (§5.2);
- `ProductSuggestions` uses the matcher and gains `IsFuzzy`;
- `ProductSuggestion` gains `IsFuzzy`, `NameRanges`, `CodeRanges`;
- `QuickSearchViewModel.Submit` gets the "not fuzzy" rule.

**Tests:** §5.3, all but the table ones. Logging stays `Debug` only.

**Acceptance:** green tests; ViewModel coverage ≥ 60 %.

### S4: Search in the UI and in the table

**Files:**

- `Views/Components/HighlightedLabel.cs`: a `Label` building a `FormattedString` from text plus ranges;
- `QuickSearchBox.xaml` (highlight and the "Similar name" caption);
- `ProductSearch` → matcher (characteristic values included);
- `ProductListViewModel` keeps the sort, adds `NameRanges` to `ProductTableRow`;
- Products page: `SearchBar` replaced by an `OutlinedField` filter;
- remove `TextMatch` when nothing uses it (refactor commit).

**Tests:** §5.3 table tests; EndToEnd "nutela" flow; `ProductListViewModelTests` (highlight ranges present, sort kept).

**Acceptance:** typing "nutela" in Receive and in Products shows Nutella with "Similar name" and the matched letters
in bold. 13 digits + Enter adds the exact product. A fuzzy single row + Enter adds nothing.

### S5: Counter feedback and texts (F7, F8, M1, M13)

**Files**

- Core `Services/Plural.cs` (`Units(n)` → "1 unit" / "12 units", `Products(n)`). It is used by every document text: summary, button, confirm, status.
- The line shows "on hand 0 → 0, short by 1" (never a negative). "Out of stock" instead of "Only 0 on hand". The Issue status shows "−1".
- Success banner after commit: `ChipSuccess` style, "Received 12 units · R-20261125-0001", 3 s, also announced (`SemanticScreenReader`).
- **Scan sound:**
  - `IScanFeedback` implementation plays a short system click/beep: Android `ToneGenerator` (`TONE_PROP_BEEP`) for success and `TONE_PROP_NACK` for an error, behind the existing interface;
  - Windows: `SystemSounds`-like through `Microsoft.UI` **[to verify]**, else silent;
  - a Settings switch "Scan sound" (Preferences, non-secret).
- Remove the inline Undo link and keep the snackbar. Clear `DocumentStatus` on remove. The snackbar is placed above the bottom bar (`Snackbar` anchor).
- `IConfirmSheet` (Core) plus a bottom-sheet implementation (app), replacing `ConfirmAsync` for commit and discard.
- Issue suggestions show out-of-stock rows in `TextSecondary` with an "Out of stock" chip, still pickable.

**Tests:** `PluralTests`; `StockDocumentViewModelTests` (texts for 1 and many, no negative stock text, status cleared
on remove, success text); `ScanFeedback` fake verifies the sound call and the switch.

**Acceptance:** screenshots of Receive done, Issue over-stock and the confirm sheet in both themes; no "1 units".

### S6: Products table and Home symmetry (§4.1, §6)

**Files:**

- `ProductsPage.xaml` (the §4.1 rules);
- `ProductTableRow` / `TableColumn` (an alignment property, the stock chip moved to the name cell);
- `HomePage.xaml`: the two secondary buttons in a `Grid ColumnDefinitions="*,*"`; the Driver gets one full-width button; tiles and buttons in the vertical centre of the free space;
- `RecentActivityPanel`: one row template (amount coloured `Success` / `Danger` plus a sign; name; "who · local time" caption) and one date format: "25 Nov 17:58" today-relative ("Today 17:58").

**Tests:** `ProductListViewModelTests` (the stock chip instead of the text in the Qty cell, alignment metadata);
`RecentActivityItemTests` (local time, sign, format).

**Acceptance:**

- header text right edges equal the number right edges;
- names wrap at most once;
- 7+ rows visible on a Pixel 7 with the Manager bottom bar;
- Home buttons equal width.

### S7: Product detail and form

**Detail page** (`ProductDetailPage.xaml` and its ViewModel):

- **Header:** photo area (16:9 placeholder with "Add photo" when empty; Take / Pick in a sheet), then name, code, and a big "42 on hand" with a status chip.
- **Actions:** *Receive* / *Issue* as an equal pair in the bottom bar. They open Receive or Issue with this product as the first line. This replaces *Record In/Out*, *Receive*, *Issue* and *Back*.
- **Characteristics:** read-only rows with an *Edit* toolbar action (Manager) that switches to `OutlinedField`s and a *Save* bottom bar.
- **History:** local time, the same row template as Recent activity.

**Product form:**

- the name field without the 🔍 icon (a new `QuickSearchBox.ShowSearchIcon=false`);
- the duplicate warning moved **above** the drop-down, directly under the name;
- leaving with unsaved input asks "Discard the new product?" (`IConfirmSheet`).

**Tests:**

- `ProductDetailViewModelTests`: the edit mode toggles; Save is only in edit mode; *Receive* navigates with the barcode parameter; a non-Manager has no *Edit*, and the service still refuses;
- `ProductFormViewModelTests`: the dirty check.

### S8: Remaining old-style pages

- **Login:** `OutlinedField`s, a show-password toggle, the card anchored to the top third so *Sign in* stays above the keyboard; the app name only in the card (no bar on login).
- **Scanner:** a rounded camera card, an `OutlinedField`, and *Find product* in the bottom bar.
- **Characteristics:** a list in a card; a "New characteristic" `OutlinedField`, a type chip pair (*Text* / *Number*) and *Add* in the bottom bar; no *Back* button.
- **Map:** an `EmptyState`.
- **Settings:** remove `RecentActivityPanel` (F5); switches at the right edge, the same card padding everywhere.
- **Startup:** move `UseMauiMaps` initialisation off the start path if MAUI allows lazy Maps init **[to verify]**; otherwise record the measured cold start on the owner's phone.

**Tests:** `SettingsViewModelTests` (scan-sound switch), `CharacteristicsViewModelTests` (type chips).

### S9: Documentation and the final acceptance

- `docs/steps/12-design-revision-3.md` (template), README step log and decision log (rows below);
- `design-system.md` (§6, new tokens: `AppBarHeight` 56, `StepperHeight` 48);
- AppTests theme keys; UiTests ids;
- both builds; the §8 emulator check with **all** screenshots retaken into `docs/design/screenshots/r3/` and compared with `r2/` one by one in the step document.

## 8. Final acceptance: emulator screenshot check (repeatable)

These are the commands used for this revision; the helper wraps them. Windows PowerShell 5.1 or 7.

**One-time setup** (already done on this machine; it needs about 4 GB of disk and no new licence):

```powershell
$env:JAVA_HOME = 'C:\Program Files\Microsoft\jdk-17.0.21.102-hotspot'   # the JAVA_HOME of the user profile points to a removed JDK
$sdk = "$env:LOCALAPPDATA\Android\Sdk"
& "$sdk\cmdline-tools\latest\bin\sdkmanager.bat" "system-images;android-35;google_apis;x86_64"
& "$sdk\cmdline-tools\latest\bin\avdmanager.bat" create avd -n stockroom -k "system-images;android-35;google_apis;x86_64" -d pixel_7
# then in %USERPROFILE%\.android\avd\stockroom.avd\config.ini: hw.ramSize = 3072, hw.gpu.mode = swiftshader_indirect
```

**Every check:**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-apk.ps1        # newest APK into artifacts\apk
. .\scripts\emulator-screens.ps1                                     # helpers; screenshots go to docs\design\screenshots\r3
Start-StockRoomEmulator -Camera none                                 # first pass: no camera (S1 regression)
Install-StockRoom; Start-Sleep 30; Skip-Anr                          # a starved host shows ANR dialogs for a while
Show-Ui                                                              # nodes with centre x,y, text, content-desc, AutomationId
Tap-Ui LoginEntry; Send-Text olena; Tap-Ui PasswordEntry; Send-Text 'Manager#2026'; Send-Key 4; Tap-Ui LoginButton
Save-Screen 03-home-manager
Tap-Ui HomeReceiveTile; Save-Screen 06-receive-no-camera; Get-Crash  # must list no FATAL EXCEPTION
Stop-StockRoomEmulator
Start-StockRoomEmulator -Camera emulated                             # second pass: the whole list below
```

The raw adb equivalents, if the helper breaks:

- `adb shell uiautomator dump /sdcard/ui.xml` and `adb exec-out cat /sdcard/ui.xml`: coordinates.
- `adb shell input tap X Y`
- `adb shell "input text 'Manager#2026'"`: the quotes keep `#` from becoming a shell comment.
- `adb shell input keyevent 4`: Back, which also hides the keyboard.
- `adb shell screencap -p /sdcard/s.png` and `adb pull /sdcard/s.png`
- `adb logcat -d | Select-String 'FATAL EXCEPTION','AndroidRuntime','MonoDroid'`
- `adb emu kill`

**Screens to retake and compare with `r2/`:**

| Group | Screens |
|-------|---------|
| Sign-in and home | Login, the wrong password, Home (Manager, Driver) |
| Drawer | light, dark |
| Receive | camera, suggestions with the keyboard ("nut" and "nutela"), a line, details, the confirm sheet, done |
| Issue | over-stock |
| Products | light, dark, "nutela" |
| Product | detail, its edit mode, the new-product form with a duplicate |
| Other pages | Scanner, Characteristics, Map, Settings (both themes) |

**Pass criteria:**

- no `FATAL EXCEPTION` in either camera pass;
- every rule of §6 holds on every screenshot, with the AppBar and status bar checked on each;
- every finding F1–F9 is fixed or listed as deferred in the step document;
- each screenshot is **looked at** (opened), not only produced.

## 9. Proposed decision-log rows (README, written in S9)

| # | Decision | Why |
|---|----------|-----|
| 103 | Camera support is checked on the device (feature + camera ids) before a reader is created; CameraX "unavailable" exceptions are handled as a last resort | The app crashed on a device without a camera (seen on the emulator) |
| 104 | Our own `AppBar` (`48 \| * \| 48`) on every page, with the Shell nav bar hidden; status bar in the page background colour | The native bar gave three different left edges, a green status bar in the dark theme, and the hamburger on pushed pages |
| 105 | Own table kept and polished; no grid library | About ten rows; the gaps were alignment, not features. Free options lack a net10 build or Windows; Syncfusion needs a licence key (owner's call) |
| 106 | `ProductMatcher`: tokens with AND, digit tiers without fuzziness, letter tiers with n-gram overlap (≥ 0.6) and Damerau–Levenshtein (≤ 1 / ≤ 2), highlight ranges; one matcher for quick search and table | Owner request; typos must find products; one rule for both searches |
| 107 | Enter never picks a fuzzy match; a barcode always goes the exact path | A keyboard-wedge scanner must never add "the closest" product |
| 108 | Scan sound through `IScanFeedback`, with a Settings switch | Forums and Scandit: feedback without looking at the screen |
| 109 | Undo after commit, minimum stock, packs, CSV and labels deferred, each needing a domain decision | `Operation` is immutable; these change the data model |

## 10. Open questions for the owner

| # | Question | Proposal |
|---|----------|----------|
| Q1 | Title centred on the phone (iOS / Material "center-aligned" bar) or left as Android usually does? | **Centred.** It is what "symmetric" asks for; desktop keeps it left |
| Q2 | Syncfusion Community Licence (registration + key in code) for the table, now or never? | Not now (§4) |
| Q3 | Keep the commit confirmation (as a themed sheet) until a reversing document exists? | Yes |
| Q4 | Scan sound on by default? | On; a switch in Settings |
| Q5 | Should a later History page (all operations, filters by product / user / day) be planned as the next feature after this polish? | Yes, it is the most requested of the "later" items (M5) |
