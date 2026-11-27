# Overview

StockRoom is a .NET MAUI (C#, MVVM) mobile app for running a small warehouse.
It is also a **teaching project**: it is built one lab at a time for the
*Mobile Development (.NET MAUI)* course, and the repository records every step
of building mobile software, from a single MVVM form to an offline-first app
backed by a database.

## What we are building

A deliberately **simple** warehouse app: one warehouse, three people, three
roles, about ten products. It does four things:

- shows the **product list** with characteristics and quantity on hand;
- **sorts** the list by any characteristic;
- lets the manager **add products and characteristics**;
- **scans a barcode**, then the user chooses **In** or **Out** and enters the
  **number of units**. A barcode identifies a product (a kind of unit), so
  one scan can record many units.

Users **sign in with a login and a password** (stored only as a salted hash) and the role decides what they may do.
Around these it behaves like a real mobile app: it **keeps unsaved forms and the
signed-in session** when minimised or killed, has a **System/Light/Dark theme**,
opens a product from a **deep link** (`stockroom://product/{barcode}`), and
scans with the **camera** on Android and iOS (manual entry on Windows).
Since Lab 7 it also uses the device: a Manager can attach a **photo** to a product (camera or gallery),
an In/Out can carry the **location** where it was recorded, a **map page** shows those operations (embedded map on
Android and iOS, a list with *Open in maps* everywhere), **shaking** the phone opens Search, and a **banner**
on the product list shows offline / syncing / changes waiting.

Entities use UUID keys, `CreatedAt`/`UpdatedAt` and an owner (`CreatedByUserId`),
and are designed in third normal form. Since Lab 4 data lives in **SQLite**, a
single file (`stockroom.db3`) with versioned migrations, that the app can
share from Settings. It works **offline first**: every change is saved locally
and queued, and the queue is sent to a (stub) server when a network is
available. The counterparty of an operation (personal data) is stored
**encrypted**; products can be **edited and deleted** (deleted only while they
have no operations).

Details: [`docs/domain/domain-model.md`](domain/domain-model.md) (overview, roles)
and [`docs/domain/entities.md`](domain/entities.md) (every entity, its fields and relations);
[`docs/domain/open-data.md`](domain/open-data.md) (Open Food Facts seed, open product and book lookup, attribution).

## Why a warehouse

The course's sample domains (a student list, a railway ERP) only show the
techniques. A small warehouse gives every lab a real job to do without
growing into a big system:

| Lab | Course topic | What it becomes in StockRoom |
|-----|--------------|------------------------------|
| 1 | MVVM, data binding, `INotifyPropertyChanged`, validation | Login, product list with quantities, add-product form with validation (name, EAN-13, unique barcode) |
| 2 | Shell navigation, `ICommand`, routes, passing parameters | Product list → details → edit characteristics and return; add characteristics; sort; In/Out page |
| 3 | App and page lifecycle, DI, saving state, theme, deep links | App state service, drafts survive backgrounding, reload on `OnAppearing`, theme switch, `stockroom://product/{barcode}`, **barcode scanner** (camera stops on `OnDisappearing`) |
| 4 | SQLite, repository pattern, CRUD, migrations, sync, encryption | Entities mapped to SQLite tables, repositories, schema migrations via `PRAGMA user_version`, database export |
