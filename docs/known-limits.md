# Known limits

- **Local accounts only.** Three fixed demo users, no registration or password recovery; roles are read from the local database, so someone who controls the device can edit it. A real deployment would check on a server.
- **Sync is push only** and goes to an in-memory stub server unless `Sync:BaseUrl` is configured; the app never pulls changes of other devices, so after a lost conflict the phone keeps its own value (step 09, section 6).
- **Photos are not synced** (only the relative path is); a file upload needs a real server.
- **The APK is debug-signed**, not trimmed and not AOT-compiled: it sideloads, it is not a store build.
- **Windows has no camera scanner** (manual entry), no shake and no embedded map; Android and iOS have them. iOS and Mac Catalyst are configured but were never built or run.
- **Google Maps tiles need a key** (owner action) and **Sentry needs a DSN**; without them the map area is blank and the test crash is disabled.
- **The Appium UI tests and the Android and hardware behaviour were not executed by the authoring tooling**; they are checked by hand with the smoke checklist in step 09.
- The Driver sees only his own operations; the other roles see all. Quantities on hand are visible to every role.
- A correction is linked to its operation by the note only; two operations without a document recorded in the same minute for the same product would share that note.
- A stock count is a draft of one device: it is not synced, and two people cannot count together.
