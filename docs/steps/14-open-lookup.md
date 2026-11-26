# Step 14 — Open product and book lookup, short barcode forms, tighter app bar

- **Lab:** none (owner request)
- **Branch:** `design`
- **PR:** not opened

## 1. Before

An unknown barcode was asked at Open Food Facts only (step 05a), so books, cosmetics, pet food and household items
stayed unknown. Only EAN-13, UPC-A and deep links were accepted. The phone app bar was 56 high, and Receive / Issue
repeated every scan in a line such as "Nutella 400 g +1".

## 2. What was asked

1. Remove the large empty gap above the title in the app bar, keeping the status bar inset safe.
2. Remove the redundant line above the list on Receive / Issue.
3. "Add barcode lists from open sources so I can scan any book or product and find it in my app."

## 3. Our interpretation

| Task | In StockRoom | Status |
|------|--------------|--------|
| App bar | `AppBarHeight` 48 on a phone, 56 on desktop. The inset itself is single and left alone | done, see 6 |
| Scan line | no message for a good or an unknown scan; the line, the sound and `IScanFeedback.Announce` say it; problems still show | done |
| Open barcode lists | online lookup chain instead of bundled lists (too big): [open-data.md](../domain/open-data.md) | done |
| Short forms | EAN-8, UPC-E, ISBN-10, hyphens and spaces, normalised to 13 digits | done |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| Sources asked one after another, stop at the first hit | all in parallel | gentle on rate limits; one request in the usual case |
| One `OpenFactsSource` per host | `product_type=all` on API v3 | the same small v2 code, no redirect handling |
| Open Library Books API (`jscmd=data`) | Search API; `/isbn/{code}.json` | one request gives the edition's title, authors and publishers; the others give work-wide lists or author keys. It is marked legacy and may be phased out |
| Memory-only cache of hits | disk cache; none | simple; Search, the line and the wizard share one answer |
| No contact e-mail in the User-Agent | send one for a higher limit | no personal data |
| Not added: UPCitemdb, Google Books, barcode APIs with keys | | keys or restrictive terms |

Decision log 123-127.

## 5. Changes

- `Services/Lookup/`: `ILookupSource`, `HttpLookupSource`, `OpenFactsSource` + `OpenFactsSite`, `OpenLibrarySource`,
  `ProductLookupChain`, `LatestLookup`, `LookupHttpClient`, `LookupParameters`. `OpenFoodFactsLookup` removed.
- `BarcodeNormalizer`; `ScanPayloadParser` uses it.
- Search and Receive / Issue show what an unknown item is, for every role; the wizard is prefilled and shows the source.
- Tests: `Services/Lookup/` with a fake `HttpMessageHandler` (no network), parser forms, view models.

## 6. Checked and not checked

Live API calls on 2026-11-26 (by hand, generic User-Agent):

| Code | Source | Answer |
|------|--------|--------|
| 9780140328721 | Open Library | Fantastic Mr. Fox, Roald Dahl, Puffin |
| 3017620422003 | Open Food Facts | Nutella, Ferrero, "400 g e" |
| 4005808890507 | Open Beauty Facts | Nivea soft, Nivea, "200 ml" |
| 3600523614523 | Open Beauty Facts | a L'Oréal foundation; the other three hosts answer 404 "different product type" |

- **App bar:** on the emulator the status bar is 52 dp (camera cut-out) and was padded once. What is left above the
  title is that system area plus the margin of the 48 px buttons; the app does not draw into it.
- In the app on the emulator (typed codes): Open Library found the book from a hyphenated ISBN-10 on Receive and the
  wizard was prefilled; Open Beauty Facts found the cosmetic on Search. Screenshots:
  [`../design/screenshots/r5`](../design/screenshots/r5) (the grey tint in 05 is Android's keyboard focus highlight).
- Once, opening Settings ended in a native crash (SIGSEGV inside `libmonosgen`) while the host had 0.3 GB of free
  memory. It did not come back on the next try; the cause is not known.
- Emulator only, not a phone; no real barcode was scanned by the camera; the screen-reader announcement was not heard.
- Windows was built, not run. Appium tests not run.
- Open Library has records under made-up ISBNs; a found name is a suggestion the Manager checks.
- The wizard's barcode box has a numeric keyboard (no X of an ISBN-10).
- Attribution is text, not a link, although the "Facts" data pages ask for links.

## Owner actions

- On a phone: scan a real book, a food and a cosmetic; judge the app bar spacing.
- Add `Author` / `Publisher` characteristics if book details should be stored and sorted.
