# Use cases and features

## Features

Module of **variant 8 (spare-parts warehouse)** of the final lab: Product = part, Operation In / Out = receipt / issue,
roles Storekeeper, Driver and Manager, camera and accelerometer (plus scanner, location, map).

| Area | What the app does | Where to read |
|------|-------------------|---------------|
| Products | list with quantity on hand and colour state; **search** by name, barcode or characteristic value; sort by name, quantity or any characteristic; add, edit, delete (Manager) | steps 01, 02, 09 |
| Counter | A bottom bar (Home, Products, + Receive / − Issue, Search, Activity); scan, then type the quantity; a review sheet with the last suppliers / recipients | step 13 |
| Daily tools | Operation table (the Driver sees only his own), *Correct quantity*, Manager's stock count, add a product in three steps | step 13 |
| Operations | scan or type a barcode, choose **In** or **Out**, enter the number of units, optionally attach the location; Out never exceeds the stock; operations are never edited | steps 01, 02, 07 |
| Roles | Storekeeper (In and Out), Driver (In only), Manager (everything, photos, logs, diagnostics); checked in the UI, in every command and in the service | step 06, [access matrix](steps/09-final.md) |
| Offline first | SQLite file, every change queued in `SyncQueue` and pushed to a stub server; last write wins | step 04 |
| Lifecycle | drafts survive minimising and restarts, signed-in session kept (token in secure storage), System / Light / Dark theme, deep link `stockroom://product/{barcode}` | step 03 |
| Device | camera scanner (1-D and QR), product photo, operation location, map, shake to scan, connectivity banner | step 07 |
| Diagnostics | Serilog file log, in-app log viewer, global exception handler, optional Sentry | step 05 |
| Quality | Core library, 1226 + 42 automated tests, GitHub Actions with a coverage gate and an APK artifact | steps 08, 09 |

The design material (ER diagram, screen map, role matrix, layers, DI lifetimes), the demo script and the answers to the
control questions are in [`docs/steps/09-final.md`](steps/09-final.md); the release steps are in [`docs/RELEASE.md`](RELEASE.md).
Rules for contributors: [`AGENTS.md`](../AGENTS.md).

## Accounts and roles

The demo logins and passwords are in the [README](../README.md); the database stores only their PBKDF2 hashes (`Data/Migrations/0003_auth.sql`). They are demo data for the course, not secrets; real accounts would be created by an administrator and the demo ones removed.

Five wrong passwords for a login lock it for 30 seconds. The session is kept for 30 days; **Sign out** removes it.
Settings has **Biometric unlock** (only on a device with a fingerprint / face sensor or Windows Hello): with it on, the app
asks for the biometric check after a restart instead of opening straight away; the password always works.

## Device features (Lab 7)

| Feature | Where | Needs |
|---------|-------|-------|
| Product photo (camera or gallery) | product form and detail page, Manager | camera permission for *Take photo*; none for *Pick photo* on Android / Windows |
| Location of an In / Out | *Attach my location* on the operation page | location permission, GPS on |
| Map of located operations | *Operations map* in the menu: embedded map on Android and iOS, list + *Open in maps* everywhere | a Google Maps API key for the tiles on Android |
| Shake to search | Home and the product list open Search; Settings switch | an accelerometer |
| Connectivity banner | top of the product list | nothing |

Every refused permission, missing sensor, cancel or failure shows one message and writes one log line; the table of
situations is in [`docs/steps/07-device.md`](steps/07-device.md).

The keys some of these need (Google Maps) and the optional services (Sentry, a real sync server) are in
[Configuration](configuration.md).
