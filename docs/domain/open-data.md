# Open data: product and book databases

StockRoom uses open data in two places: the **seed** (the demo products shipped in
`0002_seed.sql`, from [Open Food Facts](https://world.openfoodfacts.org), OFF) and the optional **lookup** of an
unknown barcode (the sources below). No database is bundled with the app: they are far too large.

## Seed: source and licence

- Open Food Facts is a collaborative, free database of food products.
- The database is published under the **Open Database Licence (ODbL) 1.0**; the
  individual records are under the Database Contents Licence (DbCL).
- ODbL requires **attribution** and share-alike for derived databases. We use a
  handful of facts (name, brand, quantity, category) and attribute the source.
- Product photos are CC BY-SA and are **not** used by StockRoom.

**Attribution text** (shown wherever looked-up data is displayed, and in the
seed header): `Data: Open Food Facts (ODbL)`.

## How the seed was produced

For each barcode the public API was called once, by hand, in 2026:

```
GET https://world.openfoodfacts.org/api/v2/product/<barcode>?fields=code,product_name,brands,quantity,categories_tags
```

Rules applied to the response:

- `Name`: `product_name` shortened and written in plain English with the pack
  size (`Nutella 400 g`), because OFF names are user-contributed and noisy.
- `Brand` (Text): first brand of `brands`.
- `Category` (Text): one readable category derived from `categories_tags`.
- `Weight (kg)` or `Volume (l)` (Number): parsed from `quantity` (`400 g` -> `0.4`,
  `330 ml` -> `0.33`). A product has one of them, not both.
- Every barcode was checked with `Ean13.IsValid` (a test enforces it) and has
  a distinct value.
- Stock movements, users and ids are demo data, not from OFF.

| Barcode | Product | Brand | Category |
|---------|---------|-------|----------|
| 3017620422003 | Nutella 400 g | Ferrero | Chocolate spreads |
| 4008400402222 | Nutella 500 g | Ferrero | Chocolate spreads |
| 5449000000996 | Coca-Cola Original 330 ml | Coca-Cola | Sodas |
| 5449000011527 | Fanta Orange 330 ml | Coca-Cola | Soft drinks |
| 8076809513326 | Barilla Ragu Bolognese 400 g | Barilla | Pasta sauces |
| 7622210449283 | Prince Chocolate Biscuits 300 g | LU | Biscuits |
| 3168930010265 | Quaker Cruesli Nut Mix 450 g | Quaker | Breakfast cereals |
| 8000500310427 | Nutella Biscuits 304 g | Ferrero | Biscuits |
| 5000159461122 | Snickers 50 g | Mars | Chocolate bars |
| 3033490004743 | Danone Skyr 140 g | Danone | Skyr |
| 9002490100070 | Red Bull Energy Drink 250 ml | Red Bull | Energy drinks |
| 5000157024671 | Heinz Baked Beanz 415 g | Heinz | Canned beans |

## Lookup at run time

`IProductLookup` (`ProductLookupChain`) asks the databases one after another for an **unknown** barcode until one
knows it. Local products are always matched first; a lookup never creates a product. Checked against the official
pages on 2026-11-26:

| Order | Source | Asked for | Request | Licence | Published limits |
|------:|--------|-----------|---------|---------|------------------|
| 1 | [Open Library](https://openlibrary.org/developers/api) (`OpenLibrarySource`) | codes starting 978 / 979 | `openlibrary.org/api/books?bibkeys=ISBN:{code}&format=json&jscmd=data` | The Internet Archive asserts no new rights over the data ([licensing](https://openlibrary.org/developers/licensing)); some records may carry rights of their contributors. No attribution text is demanded; we name the source | 1 request/s without contact details; no bulk download |
| 2 | [Open Food Facts](https://world.openfoodfacts.org) | every code | `world.openfoodfacts.org/api/v2/product/{code}` | ODbL 1.0, contents DbCL ([API](https://openfoodfacts.github.io/openfoodfacts-server/api/)) | 15 product reads/min per IP |
| 3 | [Open Beauty Facts](https://world.openbeautyfacts.org/data) | every code | the same API on `openbeautyfacts.org` | ODbL, DbCL | no number published; "1 API call = 1 real scan by a user" |
| 4 | [Open Pet Food Facts](https://world.openpetfoodfacts.org/data) | every code | the same on `openpetfoodfacts.org` | ODbL, DbCL | as above |
| 5 | [Open Products Facts](https://world.openproductsfacts.org/data) | every code | the same on `openproductsfacts.org` | ODbL, DbCL | as above |

Sources 2 to 5 are one class, `OpenFactsSource`, parametrised by `OpenFactsSite`. None needs a key or an account.

Rules:

- one `HttpClient`, 5 s per request, `User-Agent: StockRoom/1.0 (teaching project)`. The services ask for a contact
  e-mail in it; we send none (no personal data), so the anonymous limits apply;
- no certificate pinning: it applies to our own server only (`AGENTS.md` 2a);
- one request per source per unknown code, one source at a time, and only after a scan or a confirmed typed code
  (never while typing). There is no client-side throttle: a 429 or 503 is a failed source;
- a failing or slow source never stops the next one. Nothing found: `NotFound` when everyone answered, `Offline`
  when nobody was reachable, else `Failed` ("not every open database answered"). Worst case, with a network that
  hangs: 5 requests x 5 s = 25 s, shown as "Looking it up in open databases"; nothing waits for it;
- a newer code or leaving the screen cancels a running lookup;
- **cache:** hits are kept in memory until the app is closed. Nothing is stored on disk unless the Manager saves the
  product;
- attribution shown with every result: `Data: Open <...> Facts (ODbL), <host>` or `Data: Open Library, openlibrary.org`.
  It is plain text, not a link.

Used from a result: name (cut to 100 characters), and in the wizard the characteristics that exist with these names:
`Brand`, `Category` (`Book` for books), `Weight (kg)`, `Volume (l)`, `Author`, `Publisher`. Authors and publisher are
also shown in the lookup text, so they are visible without such characteristics.

Who sees it: Search and the lines of Receive / Issue show what an unknown item is to every role; only a Manager can
create the product.

## Barcode forms

Stored and compared form: 13 digits (`BarcodeNormalizer`). Existing barcodes are unchanged.

| Read or typed | Becomes |
|---------------|---------|
| EAN-13 | itself |
| UPC-A (12) | a leading 0 |
| EAN-8 | five leading zeros (the check digit stays valid) |
| UPC-E (8 digits starting 0, with check digit) | expanded to UPC-A, then a leading 0 |
| ISBN-10 (check digit may be X) | 978 + nine digits + new check digit |

Spaces and hyphens are ignored; a wrong check digit is refused. An 8-digit code starting with 0 that is a valid UPC-E
is read as UPC-E (EAN-8 numbers starting with 0 are for in-store use). The product form's barcode box has a numeric
keyboard, so an ISBN-10 ending in X can be typed on Search and Receive / Issue, not there.
