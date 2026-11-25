# Design system — StockRoom

Status: **implemented in part** (green and dark only, see [`../steps/10-design.md`](../steps/10-design.md) and
[`../steps/11-design-revision.md`](../steps/11-design-revision.md)). Tokens are the contract between this document, the future Figma file and
`Resources/Styles`. Names below are the XAML resource keys to use. Screens and flows: [`ux-plan.md`](ux-plan.md).
Revision 2 ([`revision-2.md`](revision-2.md)) changed sizes, the drawer, tiles, the bottom action bar and disabled buttons.

## 1. Principles

1. **Calm and flat.** Elevation is shown by surface tone and a 1 px border, never by shadow or glow. No gradients.
2. **One filled button per screen** — the main action (green). Everything else is outlined or ghost. Exception
   (revision 2): Home has two equal main actions, *Receive goods* and *Issue goods*, both filled tiles.
3. **Red means "this deletes or discards"**, nothing else. Issuing goods is normal work, so it is green.
4. **Numbers first.** Quantities are large, right-aligned, tabular, and never coloured as the only signal.
5. **Tokens, not values.** Pages use semantic keys (`TextPrimary`, `Border`, …) only; palettes change in one file.

## 2. Palette primitives

Primitives are written as literal values inside the theme dictionaries (`Resources/Styles/Themes`), named in comments;
a MAUI `Color` element cannot alias another resource, so there is no separate `Colors.xaml`. Pages never use them.

### Green (light theme, default)

| Key | Hex | Use |
|-----|-----|-----|
| `Green50` | `#E7F4EA` | selected row, soft chip background |
| `Green100` | `#CFE8D6` | pressed soft background |
| `Green500` | `#2E9E52` | dark theme "In / in stock" accents only |
| `Green600` | `#1F7A3A` | **primary** (fill, text on white, focus) |
| `Green700` | `#196631` | primary hover |
| `Green800` | `#14592A` | primary pressed; strong text on `Green50` |
| `Lime400` | `#8CC63F` | decorative only (thin brand stripe, illustration); **never text or a control boundary** (2.05:1 on white) |

The lime nods to the "fresh grocery green" the owner likes (Fora-like) without copying a brand colour; the working
green is darker so that white text on it passes AA (see §4).

### Violet (dark theme)

| Key | Hex | Use |
|-----|-----|-----|
| `Violet300` | `#B49CFF` | **primary on dark** (text, outline, icons, focus) |
| `Violet600` | `#6D4FD1` | filled primary button background (white text) |
| `Violet700` | `#5E41BF` | filled hover / pressed |
| `Violet900` | `#2B2342` | selected row, soft chip background |

### Blue (secondary light option, only if owner decision Q1 = yes)

| Key | Hex | Use |
|-----|-----|-----|
| `Blue50` | `#E6F0FB` | soft |
| `Blue600` | `#1565C0` | primary |
| `Blue700` | `#0F4F99` | hover |
| `Blue800` | `#0B3D78` | pressed |

### Neutrals and status

| Key | Light hex | Dark hex |
|-----|-----------|----------|
| `Background` | `#FFFFFF` | `#121018` |
| `Surface` (cards, panel) | `#F6F8F6` | `#1B1824` |
| `SurfaceAlt` (table header, hover row) | `#EEF3EF` | `#24202F` |
| `Border` (dividers, card edge — decorative) | `#D5DED7` | `#342E42` |
| `BorderStrong` (inputs, outlined buttons — must be ≥ 3:1) | `#7C8C7F` | `#6E6582` |
| `TextPrimary` | `#1A2B1E` | `#ECE9F2` |
| `TextSecondary` | `#536357` | `#A9A2B8` |
| `TextDisabled` | `#9AA59C` | `#6B6478` |
| `Danger` | `#C62828` | `#FF8A85` |
| `DangerSoft` | `#FDECEC` | `#3A1C22` |
| `Warning` | `#A15C07` | `#F5B95A` |
| `WarningSoft` | `#FFF4E0` | `#3A2E17` |
| `Info` | `#1D5FA8` | `#8DB8F2` |
| `Success` | = `Primary` light | `#6FD08F` |

## 3. Semantic tokens (what pages use)

Defined twice — in `Themes/LightGreen.xaml` and `Themes/Dark.xaml` (and `Themes/LightBlue.xaml` for Q1) — with
**identical keys**:

`Background`, `Surface`, `SurfaceAlt`, `Border`, `BorderStrong`, `TextPrimary`, `TextSecondary`, `TextDisabled`,
`Primary`, `PrimaryHover`, `PrimaryPressed`, `PrimarySoft`, `OnPrimary`, `PrimaryText` (primary used as text/outline),
`Danger`, `DangerSoft`, `OnDanger`, `Warning`, `WarningSoft`, `Info`, `Success`, `FocusRing`, `Overlay` (dialog scrim, 40 % black).

| Semantic | Light (green) | Dark (violet) |
|----------|---------------|---------------|
| `Primary` (fill) | `Green600` | `Violet600` |
| `PrimaryHover` | `Green700` | `Violet700` |
| `PrimaryPressed` | `Green800` | `Violet700` + `Overlay` 8 % |
| `PrimarySoft` | `Green50` | `Violet900` |
| `PrimaryText` | `Green600` | `Violet300` |
| `OnPrimary` | `#FFFFFF` | `#FFFFFF` |
| `FocusRing` | `Green600` | `Violet300` |

## 4. Contrast checks (WCAG 2.2)

Computed with the WCAG relative-luminance formula (script kept out of the repo; values rounded down).
AA: normal text ≥ 4.5, large text (≥ 18.66 px bold / 24 px) and UI boundaries ≥ 3.0.

| Pair | Ratio | Needed | Result |
|------|------:|-------:|:------:|
| L `TextPrimary` on `Background` | 14.91 | 4.5 | pass (AAA) |
| L `TextPrimary` on `Surface` | 13.97 | 4.5 | pass |
| L `TextSecondary` on `Background` | 6.38 | 4.5 | pass |
| L `TextSecondary` on `SurfaceAlt` (table header) | 5.68 | 4.5 | pass |
| L `TextSecondary` on `PrimarySoft` (selected row) | 5.63 | 4.5 | pass |
| L `OnPrimary` on `Primary` (filled button) | 5.38 | 4.5 | pass |
| L `OnPrimary` on `PrimaryHover` | 7.03 | 4.5 | pass |
| L `PrimaryText` on `Background` (outlined button label) | 5.38 | 4.5 | pass |
| L `PrimaryText` on `PrimarySoft` (chip) | 4.75 | 4.5 | pass (use `Green800` 7.43 for small 12 px chip text) |
| L `Danger` on `Background` | 5.62 | 4.5 | pass |
| L `Danger` on `DangerSoft` | 4.92 | 4.5 | pass |
| L `OnDanger` (white) on `Danger` | 5.62 | 4.5 | pass |
| L `Warning` on `Background` / on `WarningSoft` | 5.19 / 4.76 | 4.5 | pass |
| L `Info` on `Background` | 6.45 | 4.5 | pass |
| L `BorderStrong` on `Background` (input edge) | 3.55 | 3.0 | pass |
| L `Border` on `Background` (divider) | 1.38 | — | decorative only, never the sole boundary of a control |
| L `Lime400` on `Background` | 2.05 | — | decorative only |
| L `TextDisabled` on `Background` | 2.55 | exempt | disabled text is exempt; still readable |
| D `TextPrimary` on `Background` | 15.73 | 4.5 | pass (AAA) |
| D `TextPrimary` on `Surface` | 14.55 | 4.5 | pass |
| D `TextSecondary` on `Background` / `SurfaceAlt` | 7.68 / 6.46 | 4.5 | pass |
| D `TextSecondary` on `PrimarySoft` | 6.01 | 4.5 | pass |
| D `PrimaryText` on `Background` / `Surface` / `PrimarySoft` | 8.21 / 7.60 / 6.43 | 4.5 | pass |
| D `OnPrimary` on `Primary` (`Violet600`) | 5.69 | 4.5 | pass |
| D `OnPrimary` on `PrimaryHover` | 7.04 | 4.5 | pass |
| D `Primary` fill on `Background` (button shape) | 3.31 | 3.0 | pass |
| D `Success` / `Danger` / `Warning` on `Background` | 9.97 / 8.29 / 10.75 | 4.5 | pass |
| D `Danger` on `DangerSoft` | 6.74 | 4.5 | pass |
| D `BorderStrong` on `Background` | 3.46 | 3.0 | pass |
| D `TextDisabled` on `Background` | 3.34 | exempt | — |
| Blue `OnPrimary` on `Blue600` / `Blue600` on white / on `Blue50` | 5.75 / 5.75 / 4.99 | 4.5 | pass |

Rule for implementers: a new colour pair is added to this table (with the ratio) in the same commit.

## 5. Typography

Font: **Open Sans** (already in the app, SIL OFL 1.1) Regular + Semibold. Sizes in device-independent units;
`OnIdiom` gives phone/desktop values where they differ. `LineHeight` is a multiplier in MAUI.

| Style key | Phone | Desktop | Weight | LineHeight | Use |
|-----------|------:|--------:|--------|-----------:|-----|
| `TextDisplay` | 28 | 28 | Semibold | 1.2 | big quantity on detail |
| `TextTitle` | 22 | 22 | Semibold | 1.25 | page title |
| `TextHeading` | 17 | 16 | Semibold | 1.3 | section / card title |
| `TextBody` | 16 | 14 | Regular | 1.4 | default label, cells |
| `TextBodyStrong` | 16 | 14 | Semibold | 1.4 | product name in a row |
| `TextCaption` | 13 | 12 | Regular | 1.35 | barcode, hints, secondary info (`TextSecondary`) |
| `TextOverline` | 12 | 11 | Semibold, CharacterSpacing 0.6, upper case | 1.3 | table headers, section labels |
| `TextNumeric` | 16 | 14 | Semibold, `HorizontalTextAlignment=End` | 1.4 | quantities |

Open Sans digits are proportional by default in some builds — verify in slice 2 that a column of quantities aligns;
if not, use a fixed `WidthRequest` cell with end alignment (good enough) — Q10 keeps the font question open.

## 6. Spacing, size, radius, border

| Token | Value | Use |
|-------|------:|-----|
| `Space1`…`Space8` | 4, 8, 12, 16, 20, 24, 32, 40 | 4-pt grid; page padding phone 16, desktop 24 |
| `RadiusS` | 6 | inputs, buttons, chips (small) |
| `RadiusM` | 10 | cards, dialogs, camera frame |
| `RadiusPill` | 999 | chips, round add button |
| `BorderThin` | 1 | every border and divider |
| `BorderFocus` | 2 | keyboard focus, selected outline |
| `ControlHeight` | 48 phone / 40 desktop (was 36; Windows touch target 40 epx, revision 2) | buttons, inputs, steppers |
| `ActionHeight` | 56 phone / 48 desktop | the main action in the bottom bar (never smaller than an input) |
| `HomeTileHeight` | 120 phone / 112 desktop | Receive / Issue tiles on Home |
| `RowHeightTable` | 56 phone (two lines) / 40 desktop | table rows (min; grow with font scale) |
| `HeaderHeightTable` | 40 phone / 36 desktop | table header |
| `IconSize` | 20 (24 in tab bar) | font icons |
| `TouchTargetMin` | 48 | Android; Windows ≥ 32 |
| `DrawerWidth` | 320 (phone: at most 85 % of the width) | drawer (replaces the 240 locked side panel, revision 2) |
| `ContentMaxWidth` | 1200 | desktop pages centre beyond this |

**Shadows: none.** The template's `Shadow` style is removed; `Border.StrokeShape="RoundRectangle {RadiusM}"` +
`Stroke={DynamicResource Border}` gives cards their edge.

### Symmetry and alignment rules (revision 3)

1. One content edge: 16 on a phone, 24 on desktop. Title, fields, cards, table and bottom bar start and end there.
2. Top bar (`Views/Components/AppBar`, `AppBarHeight` 56): a `48 | * | 48` grid; menu on root pages, Back elsewhere;
   title centred on a phone; one action or an empty slot on the right. Same colour as the page and the status bar.
   The Shell navigation bar is hidden everywhere.
3. Pairs of buttons share the width 50 / 50 with an 8 gap; a single secondary button is full width.
4. One filled button per screen (the Home tiles excepted), in the bottom bar.
5. Rhythm 8 / 16 / 24; the first content block starts 8 under the top bar.
6. Controls that belong together sit in one frame (a stepper is `[ - | 12 | + ]`, 48 high); Remove stands apart.
7. Numbers right, text left, headers aligned like their column.
8. One empty-state message per page.
9. Confirmations are themed (a bottom sheet on phones).

Keyboard (Android): the window resizes (`adjustResize`), page grids use `SafeAreaEdges="Container"`, so the bottom
bar stays above the keyboard and above the system navigation bar.

## 7. States

Implemented with `VisualStateManager` in styles (`Normal`, `PointerOver`, `Pressed`, `Focused`, `Disabled`) — MAUI
supports `PointerOver` on Windows; on Android hover never happens, `Pressed` carries the feedback.

| State | Filled primary | Outlined | Ghost | Danger (outlined) | Input |
|-------|----------------|----------|-------|-------------------|-------|
| Normal | bg `Primary`, text `OnPrimary`, no border | bg transparent, border `BorderStrong` 1, text `PrimaryText` | no border, text `PrimaryText` | border `Danger`, text `Danger` | border `BorderStrong`, bg `Background` |
| Hover | bg `PrimaryHover` | bg `PrimarySoft` | bg `SurfaceAlt` | bg `DangerSoft` | border `TextSecondary` |
| Pressed | bg `PrimaryPressed` | bg `PrimarySoft`, border `PrimaryText` | bg `PrimarySoft` | bg `DangerSoft`, border 2 | — |
| Focused (keyboard) | + 2 px `FocusRing` outline (outer `Border`) | border `FocusRing` 2 | border `FocusRing` 2 | border 2 | border `FocusRing` 2 |
| Disabled | bg `SurfaceAlt`, 1 px `BorderStrong`, text `TextDisabled`, reason caption below (revision 2) | border `Border`, text `TextDisabled` | text `TextDisabled` | border `Border`, text `TextDisabled` | bg `Surface`, text `TextDisabled` |
| Error | — | — | — | — | border `Danger` 1, caption below in `Danger` with an icon |

Disabled buttons with a reason (e.g. delete of a product with history) also get `ToolTipProperties.Text` (Windows) and
the reason in a caption on phone — never a silent grey button.

## 8. Components

### Button variants (`StyleClass`)

| Class | Look | Example |
|-------|------|---------|
| `Primary` | filled green / violet, white text, radius 6, height 48/36, Semibold | *+ Add product*, *Receive 37 units*, *Sign in* |
| `Outlined` (default for secondary) | 1 px `BorderStrong`, `PrimaryText` label | *Map*, *Characteristics*, *Scan*, *Add* |
| `Danger` | 1 px `Danger` border, `Danger` text; filled red only inside a destructive confirm dialog | *Discard draft*, *Delete* |
| `Ghost` | no border, `PrimaryText` | *Show all*, *Undo* |
| `Icon` / `IconDanger` | 40×40 (48 on phone) ghost with a font icon, `SemanticProperties.Description` required | row edit / trash |
| `ActionTile` | filled `Primary`, radius 10, height `HomeTileHeight`, icon 32 + `TextTitle` + caption (revision 2) | Home *Receive goods*, *Issue goods* |
| `BottomActionBar` | `Surface` bar, 1 px top `Border`, one full-width `Primary` button of `ActionHeight` (desktop: right-aligned, min 320), stays above the soft keyboard | *Receive 37 units*, *Add product* |

The round `Fab` is removed in revision 2: *Add product* (Manager) lives in the Products bottom bar.

Labels are verbs + object ("Receive 37 units", not "OK"); sentence case.

### Data table

- Header row: `SurfaceAlt` background, `TextOverline` in `TextSecondary`, bottom border `BorderStrong` 1.
- Header cell = tappable (`TapGestureRecognizer` + `PointerGestureRecognizer`), whole cell is the target (≥ 40 px high).
  Sorted column: label `TextPrimary` Semibold + arrow icon (`IconArrowUp` / `IconArrowDown`, 16 px, `PrimaryText`).
  Unsorted: no arrow; on hover (desktop) a faint `IconArrowSort` in `TextDisabled`.
  `SemanticProperties.Description`: "Name, sorted ascending. Activate to sort descending."
- Rows: `Background`; divider 1 px `Border`; hover `SurfaceAlt`; pressed/selected `PrimarySoft`; no zebra stripes
  (borders already separate rows; zebra fights the selection colour).
- Columns: text left, numbers right, actions right (fixed width). Missing value shows `—` in `TextDisabled`.
- Header stays visible (it is outside the `CollectionView`, the list scrolls under it).
- Narrow (< 600): Name (+ barcode caption), Qty, one chosen characteristic column (`▾` picker in its header).
- Loading: 3 skeleton rows (`SurfaceAlt` blocks), no spinner over the table; refresh uses `RefreshView`.

### Chips (`Border` + `Label`, `StyleClass="Chip…"`)

Height 24, radius pill, padding 8×2, `TextCaption` Semibold.

| Class | Background | Text | Example |
|-------|------------|------|---------|
| `ChipNeutral` | `SurfaceAlt` | `TextSecondary` | *Storekeeper*, *Online · all synced* |
| `ChipSuccess` | `PrimarySoft` | `Green800` / `Violet300` | *+12 In* |
| `ChipWarning` | `WarningSoft` | `Warning` | *3 changes waiting*, *Offline* |
| `ChipDanger` | `DangerSoft` | `Danger` | *Out of stock*, *Only 4 on hand* |
| `ChipInfo` | `SurfaceAlt` | `Info` | *Syncing…* |

Every chip has text; an icon is optional and never alone.

### Empty states

Centered in the list area: icon 40 px `TextDisabled`, `TextHeading` title, `TextCaption` message, optional outlined
action. Texts: *No products yet — Add product* (Manager) / *No products yet. A manager adds them.* (others);
*Nothing matches "xyz" — Clear search*; *Draft is empty — Scan a barcode or type it above*.

### Cards and sections

`Border`, `Surface` background, 1 px `Border`, radius 10, padding 16, `TextHeading` title. Used on Settings, detail.

### Dialogs, toasts

Confirm dialogs use `IAlertService.ConfirmAsync` (native dialog — consistent and accessible on both platforms).
Toast/snackbar: bottom, `TextPrimary` on `Surface` with 1 px `BorderStrong`, max 2 lines, optional ghost action (*Undo*),
4–5 s. No custom modal pages in this pass.

### Scanner frame

Camera preview radius 10, 1 px `BorderStrong`; result banner inside the frame bottom: success = `PrimarySoft` +
check icon + product name; error = `DangerSoft` + error icon + reason; 1.5 s; torch toggle (ghost icon) top-right.

## 9. Icons

**Choice: Fluent UI System Icons, Regular** (`FluentSystemIcons-Regular.ttf`, Microsoft, MIT) as an icon font, used
through `FontImageSource` / `Label` glyphs — one static TTF, crisp at any size, tinted by colour tokens, no SVG
pipeline. It ships an official code-point JSON, so glyph codes are looked up, not guessed, and its outlined style
matches the calm, bordered look on both Windows and Android. Use `FluentSystemIcons-Filled.ttf` only for the selected
tab icon (optional). The font file and its `LICENSE` go to `Resources/Fonts/`; glyph code points are named once in
`Resources/Styles/Icons.xaml`: `IconAdd`, `IconDelete`, `IconEdit`, `IconScan` (barcode scanner), `IconArrowUp`,
`IconArrowDown`, `IconArrowSort`, `IconBox` (products), `IconReceive` (arrow into box / inbox), `IconIssue`
(arrow out of box), `IconSettings`, `IconSync`, `IconCloudOff`, `IconSearch`, `IconDismiss`, `IconCheckmark`,
`IconWarning`, `IconMap`, `IconPerson`, `IconSignOut`, `IconFlashlight`, `IconSubtract`.
Fallbacks if a glyph is missing: Lucide (ISC) or Tabler (MIT) TTF — same mechanism, swap the font file and code points.
Material Symbols was **not** chosen: it is a variable font and how MAUI renders its axes on Windows/Android is not
verified; classic Material Icons is no longer updated. Details: [`research.md`](research.md) §4. The full TTF is
large (several MB); subsetting to the ~25 used glyphs (e.g. `pyftsubset`, done once by the implementer, outside the
build) is an optional size optimisation, not required.

## 10. Theme switching

- **Setting**: existing `ThemeSetting` (System / Light / Dark) in `IThemeService`, persisted via `IPreferencesStore`
  (key `theme`, unchanged). Optional accent (Q1) = new `AccentSetting` (Green / Blue), key `accent`.
- **Mechanism: resource dictionary swap.** `App.xaml` merges one *theme dictionary*
  (semantic keys) and `Styles.xaml`. Styles and pages refer to semantic keys with **`DynamicResource`**. The app's
  `IThemeApplier` implementation removes the current theme dictionary and adds `LightGreen` / `LightBlue` / `Dark`,
  and also sets `Application.UserAppTheme` so native parts (status bar, pickers, WinUI chrome) follow.
  For `System` it resolves the current OS theme and re-applies on `Application.RequestedThemeChanged`.
- **Why not `AppThemeBinding` everywhere** (what the template does): it handles only two variants and its values
  cannot be `DynamicResource`, so a second light palette (blue) would need every binding duplicated. One swap point
  keeps the logic in one class (AGENTS DRY) and is the documented MAUI approach for runtime themes
  (see research.md §4, Microsoft Learn "Theme an app").
- **Pure logic in Core**: `ThemePalette.Resolve(ThemeSetting, AccentSetting, bool systemIsDark)` → palette key —
  unit-tested; the app class only performs the swap.
- **Android system bars**: status bar colour = `Background`, dark icons in light theme (CommunityToolkit
  `StatusBarBehavior` if the toolkit is accepted, else `Platforms/Android` styles); Windows title bar follows `UserAppTheme`.

## 11. Motion

Minimal: 150 ms fade for toasts and the scan result banner, 120 ms background change on row press. No page-transition
customisation; respect the OS "remove animations" setting where MAUI exposes it.
