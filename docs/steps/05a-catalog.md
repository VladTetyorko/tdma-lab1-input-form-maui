# Step 05a — Catalog: real data, QR scanning, product lookup, quick flow

- **Lab:** none (not a course lab; it prepares the data every later lab uses)
- **Branch:** `catalog` (on top of `plan-labs-5-9`)
- **PR:** _to be added_

## 1. Before

After Lab 4 the app stored data in SQLite, seeded by `0002_seed.sql` with ten
invented products (`4820001…` barcodes, tools and paint). The scanner read 1-D
barcodes with the camera, or typed EAN-13 text. An unknown barcode only offered
a Manager an empty product form with the barcode filled in. The operation page
had a stepper for the quantity.

## 2. What this step asks for

From [`docs/plan.md`](../plan.md), section "Catalog":

1. Replace the invented seed with about 12 real Open Food Facts products.
2. Read QR codes too; turn any scan into a barcode in one pure parser.
3. Look an unknown barcode up in Open Food Facts; a Manager can create the
   product from the answer.
4. After a scan, open the operation page with the quantity ready to type.
5. Tests for the parser, the lookup, the scanner ViewModel and the seed.

## 3. Our interpretation

| Task | In StockRoom | Status |
|------|--------------|--------|
| Real seed | 12 products, characteristics `Weight (kg)`, `Volume (l)`, `Brand`, `Category`; source and licence in [`open-data.md`](../domain/open-data.md) | done |
| QR | `CameraScannerView` reads `OneDimensional \| QrCode`; `ScanPayloadParser` accepts EAN-13, UPC-A (padded to EAN-13) and `stockroom://product/{barcode}` | done |
| Lookup | `IProductLookup` / `OpenFoodFactsLookup`, result type `LookupResult` (`Found`, `NotFound`, `Offline`, `Failed`) | done |
| Create from lookup | Manager only: **Create from Open Food Facts** opens the product form with name, barcode and the attribution text | done (name and barcode only) |
| Quick flow | `FocusOnAppearingBehavior` focuses a numeric quantity entry (text selected) when the operation page appears; no code-behind logic | done |
| Tests | see section 5 | done |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| Edit `0002_seed.sql` in place; delete a dev database | New migration `0003` that replaces the demo rows | Pre-release: no real data exists; a migration would keep dead rows and make later migrations (auth) harder to read |
| `Brand` replaces the demo `Color` characteristic (same id slot); products carry `Weight (kg)` **or** `Volume (l)`, not both | Keep `Color`; fill both sizes | Open Food Facts has no colour; drinks have a volume and solids a weight, so the list shows real, sortable values |
| `ScanPayloadParser` is a static pure class returning `Result<string>` | Interface + injected service | Pure function, nothing to fake (KISS); reuses `DeepLinkParser` and `Ean13` |
| UPC-A is accepted by padding with `0` and checking the EAN-13 digit | Reject non-EAN codes | The padded code is the same product in EAN-13 systems and works with the existing validator |
| `IProductLookup` returns a `LookupResult` with a status instead of throwing | `Result<T>` with an error string; exceptions | Four outcomes (found, unknown, offline, bad answer) need different messages; `Result<T>` has only success/failure |
| One `HttpClient` created by `OpenFoodFactsLookup.CreateClient` over an `HttpMessageHandler` registered in DI | `IHttpClientFactory` (extra package) | No new dependency; tests replace the handler (`FakeHttpMessageHandler`) so no test reaches the internet |
| Lookup runs only when the Manager taps the button (one request per tap), never automatically and never cached | Look up on every unknown scan | Rate limit (HTTP 429), privacy of what is scanned, and a Manager is the only role that can use the answer |
| The form is prefilled with name and barcode only | Also prefill brand and category | The product form has no characteristic values yet; adding them is a **next step** (the data is already in `LookupResult`) |
| The attribution text `Data: Open Food Facts (ODbL)` is shown next to the lookup button and on the prefilled form | Only in documentation | ODbL requires attribution where the data is used |
| The quantity gets a numeric `Entry` next to the stepper | Focus the stepper | A stepper cannot take typed input; the entry makes "scan, type 3, Save" possible |

## 5. Changes in the PR

- `Data/Migrations/0002_seed.sql` (12 real products), `docs/domain/open-data.md`,
  links from `README.md` and `docs/domain/domain-model.md`.
- `Services/`: `ScanPayloadParser`, `IProductLookup`, `OpenFoodFactsLookup`,
  `LookupResult`, `LookupStatus`; `NavigationKeys` (`ProductName`, `DataSource`).
- `ViewModels/`: `ScannerViewModel` (parser for camera and manual entry,
  `CreateFromLookupCommand`, `LookupAttribution`), `ProductFormViewModel`
  (name and `DataSource` from the lookup).
- `Views/`: `CameraScannerView` (QR), `ScannerPage` (button, attribution),
  `ProductFormPage` (attribution), `OperationPage` (quantity entry),
  `FocusOnAppearingBehavior`.
- `ServiceCollectionExtensions`: lookup and `HttpMessageHandler` registration.
- Tests: `ScanPayloadParserTests` (theories), `OpenFoodFactsLookupTests` (canned
  JSON, malformed JSON, HTTP errors, `HttpRequestException`, timeout, container
  wiring), `ScannerViewModelTests` (QR/UPC-A/deep link, Manager create path,
  non-manager, offline/not found/failed), `ProductFormViewModelTests` (prefill),
  `SeedMigrationTests` (12 valid unique barcodes, characteristic values point to
  existing ids, stock per product); the tests that used the old demo products
  now use the real ones. Fakes: `FakeHttpMessageHandler`, `FakeProductLookup`.

## 6. Open questions / next steps

- Prefill `Brand` and `Category` (and weight/volume parsed from the quantity
  text) in the product form once it supports characteristic values.
- Logging of the lookup uses `ILogger` already; Lab 5 adds the sinks.

## Owner actions

These cannot be done by the agent and are **not** claimed as verified:

- **Camera QR scan on Android.** Generate a QR code whose text is
  `stockroom://product/3017620422003` (any online QR generator), install the
  app on a device, open **Scan**, point the camera at the QR code: the operation
  page of *Nutella 400 g* must open with the quantity field focused. Repeat
  with a printed EAN-13 label and a UPC-A label.
- **Lookup on a device.** Sign in as Manager, scan or type a valid EAN-13 that is
  not in the seed (for example `3175680011480`), tap **Create from Open Food
  Facts**: the form opens with the name filled in and the attribution below it.
  Switch the device to flight mode and repeat: a message, no crash.
- **Delete the old dev database** (see README, "Database") so the new seed is applied.
