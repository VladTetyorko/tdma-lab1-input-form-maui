# Research — what to build and with what

Date: 2026-11-24. Scope: inputs for the StockRoom UI redesign ([`design-system.md`](design-system.md),
[`ux-plan.md`](ux-plan.md), [`tasks.md`](tasks.md)). Everything below was checked on that date; items marked
**not verified** come from search-result summaries or blocked pages and must not be quoted as fact.

## 1. Method and limits

- Product documentation and store pages of comparable apps were opened and read.
- Review sites (Capterra, G2, Software Advice) and Square's help/community pages answered `403` to automated access;
  Reddit threads did not surface in search. The review evidence is therefore thinner than planned.
- Libraries were checked through the NuGet v3 API (package TFMs, dates, downloads), the GitHub REST API (licence,
  activity, stars) and Microsoft Learn. Nothing was installed. Agent-skill texts were downloaded to a temporary folder
  and scanned as text; nothing from them was executed.

## 2. Comparable apps — sources

| Source | What it says (paraphrased) |
|--------|----------------------------|
| [NN/g — Data tables](https://www.nngroup.com/articles/data-tables/) | Make sorting obvious; show active filters; keep header row fixed; inline row actions only for 1–2 actions; edit in a non-blocking panel |
| [Scandit — Scanning at scale UX insights](https://www.scandit.com/blog/scanning-at-scale-ux-insights/) | Feedback must be unmissable (visual + sound + haptic: floors are loud); short pause after feedback builds confidence; big scan button (pressed hundreds of times a day); avoid accidental scans |
| [Scandit SparkScan docs](https://docs.scandit.com/sdks/android/sparkscan/advanced/) | Single vs continuous mode; distinct error state (colour, sound, message) for duplicate / wrong codes; error timeout < 2 s keeps the flow; hardware trigger for gloved users |
| [Scanbot — barcode scanner features](https://scanbot.io/blog/6-top-barcode-scanner-features/) | Batch scanning without restarting; "scan & count" running list with totals; immediate visual confirmation; restrict symbologies to the task |
| [Odoo Barcode — receipts and deliveries](https://www.odoo.com/documentation/master/applications/inventory_and_mrp/barcode/operations/receipts_deliveries.html) | Scan → lines collect → +1/−1 or "fill expected" → *Validate* (partly read) |
| [Odoo Barcode — adjustments](https://www.odoo.com/documentation/18.0/applications/inventory_and_mrp/barcode/operations/adjustments.html) | Repeat scan adds to the count; keypad with ±1; review; *Apply*; green confirmation |
| [inFlow mobile guide](https://www.inflowinventory.com/support/cloud/inflow-inventory-mobile-app-full-guide) | Same barcode again = +1; many products per session; type or ± quantity; *Done* commits; backing out discards |
| [Sortly — Quick Actions](https://help.sortly.com/hc/en-us/articles/360012367432) | Choose action + quantity once, each scan applies it; optional confirm per scan; beep and vibration switchable |
| [Shopify Stocky reviews](https://apps.shopify.com/stocky/reviews) | "Painfully slow"; scanner failing in counts; label printing failures; receiving stalls; some long-term users satisfied |
| [BoxHero on the App Store](https://apps.apple.com/us/app/id1325512157) | Scan in/out, in/out drafts, low-stock alerts, flashlight; praised as easy; complaints about wrong totals across locations |
| [Sortly on Trustpilot (p. 7)](https://www.trustpilot.com/review/sortly.com?page=7) | Praise: easy, clean, QR lookup. Complaints: billing, slow search, inconsistent sort/filter, stale low-stock alerts |

**Not verified** (summaries only): Capterra/G2 on Sortly (scanner phone-only, duplicates on scan-add, tedious label
reprints); Software Advice on BoxHero (wants faster in/out shortcuts); Zoho community (asks for continuous scanning —
scan frame closes after each item); Square help (bulk receive: second scan adds to the list; saved counts stay on one
device) and community (scan-to-receive stopped working); Odoo forum (counts slow at 70–80 lines); claims by
Ventor/Cleverence that Odoo lacks offline mode (competitor marketing, not evidence); NN/g "Mobile tables" (lock
headers, choose a subset of columns, signpost horizontal scroll).

## 3. Findings

**What good apps do** — repeat scan increments the existing line; collect → review → commit in one step
(Odoo *Validate*, inFlow *Done*, Square bulk receive, BoxHero drafts); several ways to change a quantity (±1, keypad);
set the action once then scan many (Sortly Quick Actions); unmissable feedback (beep, vibration, banner); flashlight
and keyboard-wedge / hardware scanners; drafts that survive leaving the screen.

**What users complain about** — slowness (Stocky; Sortly search; Odoo large counts — not verified); scanner that is
not continuous or needs too many taps (Zoho, BoxHero — not verified); scanning that silently stops working
(Stocky; Square — not verified); wrong numbers and duplicate entries (BoxHero multi-location; Sortly — not verified);
unreliable sort and filter (Sortly); sync drift (Stocky); price and billing (out of our scope).

**What this means for StockRoom** (each item is traced into ux-plan.md):

| # | Recommendation | Where |
|---|----------------|-------|
| 1 | Separate *Receive* and *Issue* pages: direction chosen once, not per scan | ux-plan §2, §4.4 |
| 2 | Continuous camera scanning; repeat scan = +1 on the same line, line highlighted, no duplicates | ux-plan §6.2 |
| 3 | Feedback on every scan: sound + vibration + banner; distinct error state, < 2 s | ux-plan §6.2, design-system §8 |
| 4 | Big ±1 steppers (48 dp) and tap-to-type quantity (gloves) | design-system §6 |
| 5 | Review then one *Receive N units* commit, one DB transaction; *Discard?* on leaving a non-empty draft | ux-plan §6.3 |
| 6 | Undo before commit (snackbar); after commit operations stay immutable (correction = opposite operation) | ux-plan §6.2 |
| 7 | Issue: show on hand per line and flag excess while scanning, re-check at commit | ux-plan §4.5, §6.3 |
| 8 | Unknown barcode keeps the session; Manager can create the product (Open Food Facts) | ux-plan §6.2 |
| 9 | Table: fixed header, explicit sort arrow, visible active search, right-aligned quantity, few columns on phone | design-system §8 |
| 10 | Drafts survive minimise / kill (Lab 3 mechanism) and are visible as tab badges | ux-plan §3 |
| 11 | Keyboard-wedge scanners work on Windows (scan box keeps focus, Enter adds) | ux-plan §4.5, §9 |
| 12 | Fast, predictable sort/search: keep it in memory (already the case, ~10 products) | unchanged |

Visual direction (owner's request) is applied in design-system.md: white + green (own accessible green, Fora-like lime
only as decoration — Q2), dark + violet, borders not shadows, no glow, one filled button per screen.

## 4. Libraries, icon fonts and guidance — verified

The app resolves `Microsoft.Maui.Controls` **10.0.0** (from `obj/project.assets.json`); Core uses
CommunityToolkit.Mvvm 8.4.2.

| Candidate | Licence | Latest / date | .NET 10 MAUI | Activity / popularity | Verdict | Reason |
|-----------|---------|---------------|--------------|-----------------------|---------|--------|
| [CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui) | MIT | 17.0.0 (2026-11-10, **net11 only**); **16.0.0 (2026-11-09)** | 16.0.0: net10.0-android / -windows, needs Controls ≥ 10.0.90 | .NET Foundation, ~10.9 M downloads, 2.7 k stars | **Accept, pin 16.0.0** after a MAUI servicing bump (Q11) | Snackbar/Toast, StatusBarBehavior, IconTintColorBehavior, Popup — official and maintained |
| [CommunityToolkit.Maui.Markup](https://github.com/CommunityToolkit/Maui.Markup) | MIT | 9.0.0 (net11 only) | No | active | Reject | C# markup; we use XAML |
| Own header + `CollectionView` | — | — | yes | — | **Accept** | Sorting already lives in `ProductSorter`; no dependency; fully themable |
| [Maui.DataGrid](https://github.com/akgulebubekir/Maui.DataGrid) (`akgul.Maui.DataGrid`) | MIT | 4.0.6 (2025-01-02) | net8/net9 TFMs only | 338 stars, ~97 k downloads; no NuGet release in 22 months | Reject | No net10 build; own header is simpler |
| [UraniumUI](https://github.com/enisn/UraniumUI) | Apache-2.0 (repo; nuspec has no licence field) | 3.0.0 (2026-06-14) | yes | 1.5 k stars, ~311 k downloads | Reject (optional) | Pulls InputKit.Maui + Plainer.Maui and takes over styling: heavy for a restyle |
| [HorusStudio Maui.MaterialDesignControls](https://github.com/HorusSoftwareUY/Maui.MaterialDesignControls) | MIT | 10.0.1 (2026-02-12) | **no Windows TFM** | 85 stars | Reject | Breaks the Windows target |
| [Syncfusion.Maui.Toolkit](https://github.com/syncfusion/maui-toolkit) | MIT (repo) | 1.0.12 (2026-11-12) | TFMs **not verified** | 752 stars | Not now | Open source, but no data grid; we need nothing it alone offers; vendor pull toward commercial controls |
| Syncfusion DataGrid / DevExpress / Telerik | commercial | — | — | — | Reject | Not free / permissive (owner's rule) |
| [AathifMahir MauiIcons](https://github.com/AathifMahir/MauiIcons) | MIT | 6.0.0 (2026-02-22) | yes | ~168 k downloads | Not needed | Typed enums over a font; a TTF + `FontImageSource` is simpler |
| [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) | MIT | repo active 2026-11-24 | font file (static TTF + code-point JSON) | Microsoft, 11 k stars | **Accept** | Static TTF, official code-point map, fits Windows and Android |
| [Lucide](https://github.com/lucide-icons/lucide) | ISC (LICENSE text checked) | 1.79.0 (2026-11-24) | font file via npm `lucide-static` | 26 k stars | Fallback | Good outline set; TTF only from npm |
| [Tabler Icons](https://github.com/tabler/tabler-icons) | MIT | 3.53.0 (2026-11-24) | font file via npm webfont | 22.8 k stars | Fallback | Good alternative |
| [Material Symbols / Icons](https://github.com/google/material-design-icons) | Apache-2.0 | repo 2026-10-23 | variable TTF (axes on MAUI **not verified**); classic set "no longer updated" | 54 k stars | Reject for now | Variable-font rendering risk |
| [Phosphor](https://github.com/phosphor-icons/core) | MIT | core 2.0.8 (2024-02) | font file | 407 stars (core) | Reject | Older releases, smaller community |
| Open Sans (already bundled) | SIL OFL 1.1 | — | yes | — | Keep | No change needed (Q10) |

**Microsoft guidance used** (pages reachable on the date): [Respond to system theme changes (AppThemeBinding)](https://learn.microsoft.com/dotnet/maui/user-interface/system-theme-changes),
[Theme an app (ResourceDictionary swap + DynamicResource)](https://learn.microsoft.com/dotnet/maui/user-interface/theming),
[Shell flyout](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/flyout), [Shell tabs](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/tabs),
[Fonts and FontImageSource](https://learn.microsoft.com/dotnet/maui/user-interface/fonts),
[CollectionView](https://learn.microsoft.com/dotnet/maui/user-interface/controls/collectionview). No Microsoft
"Material/Fluent style template" for .NET 10 was found (**not verified** that none exists).

## 5. Agent skills and AI guidance — checked

| Skill / guidance | Publisher, licence | Content | Safety review | Verdict |
|------------------|--------------------|---------|---------------|---------|
| [davidortinau/maui-skills](https://github.com/davidortinau/maui-skills) | David Ortinau (MAUI PM at Microsoft), MIT, 181 stars | ~40 `SKILL.md`: `maui-theming` (AppThemeBinding, DynamicResource, Android `ConfigChanges.UiMode` gotcha), `maui-collectionview`, `maui-shell-navigation`, `ux-mobile`, `ux-desktop` | Text scan clean (no `curl \| bash`, no exfiltration, no encoded payloads, no hidden Unicode; links only to GitHub / Microsoft Learn). One read-only helper script. **The repo commits its author's `.claude/settings.local.json` allowing `Bash(python3:*)`, `Bash(do:*)`, `Bash(open:*)`** — harmless unless you open a session inside a clone | **Accept as reading material.** Copy the text of individual skills into the implementing agent's prompt if useful; do not clone it and work inside it; install nothing globally |
| [mallibone/maui-ai-design-skill](https://github.com/mallibone/maui-ai-design-skill) | Mark Allibone, **no licence** (all rights reserved), 5 stars | One `maui-app-design` skill: palette (60-30-10, dark variants, contrast check), Fluent icons via code-point JSON, Colors/Styles.xaml | Clean; asks the user to fetch the TTF, runs nothing. Sample app uses MauiReactor | Ideas only — **do not copy** (no licence); MauiReactor not wanted |
| [github/awesome-copilot `dotnet-maui.instructions.md`](https://github.com/github/awesome-copilot) | GitHub, MIT | General MAUI coding rules | Clean | Optional reading; AGENTS.md already covers it |
| dotnet/maui `.github/copilot-instructions.md`, `.github/skills/*` | Microsoft, MIT | For contributors to the framework (CI, Helix, PR review) | Clean | Not relevant to app UI |
| syncfusion `maui-ui-components-skills`, `maui-ui-builder` | Syncfusion | Steer to Syncfusion controls | — | Reject (licensing) |
| `Postpartum-genushyacinthus29/dotnet-skills` | random-word account created 2026-03-29; copy of managedcode/dotnet-skills | README rewritten to push a Windows binary from Releases and to click SmartScreen "Run anyway" | **Suspicious — typical malware clone.** No asset was downloaded | **Reject; avoid.** Use the original `managedcode/dotnet-skills` if that catalogue is ever wanted |

No skill was installed into the user's or the project's agent configuration.

## 6. Open points from research

- Variable-font rendering in MAUI (Material Symbols) — not verified; avoided by choosing a static TTF.
- Syncfusion.Maui.Toolkit TFMs — not verified; not used.
- Whether `Maui.DataGrid` 4.0.6 would run on net10 through compatibility — not verified; not used.
- WinUI rendering of Shell tabs inside a locked flyout — to be checked by the slice 5 spike (tasks.md).
