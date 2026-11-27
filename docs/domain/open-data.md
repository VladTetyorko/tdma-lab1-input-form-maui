# Open data: Open Food Facts

StockRoom uses real product data from [Open Food Facts](https://world.openfoodfacts.org)
(OFF) in two places: the **seed** (the demo products shipped in
`0002_seed.sql`) and the optional **lookup** of an unknown barcode.

## Source and licence

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

`IProductLookup` (`OpenFoodFactsLookup`) asks the same API for an **unknown**
barcode. Only a Manager can use the result, to prefill the product form (name
and barcode). Rules:

- online only; no cache; the result is never stored unless the Manager saves the product;
- one `HttpClient`, 5 s timeout, `User-Agent: StockRoom/1.0 (teaching project)`
  (no personal data);
- no certificate pinning: it applies to our own server only (`AGENTS.md` 2a);
- offline, a timeout or a bad response never crashes: the user sees a clear message.
- be a good citizen: the API is rate limited (HTTP 429); StockRoom makes one
  request per explicit user action.
