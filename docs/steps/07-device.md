# Step 07 — Device capabilities: permissions, photo, location, map, shake, connectivity

- **Lab:** 7 (`docks/` lab 7 PDF: runtime permissions, an entity with a photo and GPS saved through a repository, handling of denial / GPS off / no camera / cancel with messages and logs, a map with pins and a Windows alternative, a shake gesture with the sensor lifecycle, a connectivity indicator with auto-sync)
- **Branch:** `lab7` (on top of `lab6`)
- **PR:** _to be added_

## 1. Before

The only hardware the app touched was the camera for barcode scanning (`IBarcodeScanner`, one `CAMERA` permission, request on the scanner page). A product was a name and a barcode; an operation had no place. There was no photo, no location, no map, no sensor. Connectivity was visible only on the Settings page (`NetworkText`, `SyncText`); the product list did not say that it was offline. `SyncService` already pushed the queue when the network came back (Lab 4), but nothing showed it. Permission denial was handled in one place (the scanner page).

## 2. What the lab asks for

Classroom (6 points): the permissions the feature needs in the manifest and at run time (camera, location) with an explanation; an entity (the lab's wagon inspection) with a photo and GPS coordinates, saved through a repository; correct handling of denial, missing hardware and cancel with a message and a log entry; a map with markers.
Self-study (4 points): the same on a platform without a Map control (Windows); a shake gesture that starts a new record, with the sensor started and stopped by the page lifecycle; a network indicator that shows offline and syncs by itself when the network returns.
Report: the list of permissions with a justification, screenshots, a log excerpt with device problems, answers to the six control questions (section 8). The lab's own example is a railway wagon inspection; the adaptation is in section 3.

## 3. Our interpretation

| Lab idea | In StockRoom | Status |
|----------|--------------|--------|
| Photo of the defect | `Product.PhotoPath`: a photo of the item (camera or gallery). File: `AppDataDirectory/photos/<barcode>_<yyyyMMdd_HHmmss>.jpg`; the database stores the **relative** path `photos/<barcode>_<yyyyMMdd_HHmmss>.jpg`. Thumbnail (`Image`, style `Thumbnail`) on the product form and the detail page; buttons *Take photo*, *Pick photo*, *Remove photo* (Manager). A photo is **staged** until Save (section 4) | done |
| GPS of the inspection | Optional `Operation.Location` (`GeoPoint?`, columns `Latitude` / `Longitude`): *Attach my location* / *Clear location* on the In/Out page (mainly a driver's drop-off point), the text "Location attached: 50.45010, 30.52340", saved by `IStockService.RecordAsync(..., location)`, kept in the operation draft | done |
| Entity saved through a repository | `0004_device.sql` + `ProductRow.PhotoPath`, `OperationRow.Latitude/Longitude`, both repositories map them, the sync payload (`SyncQueue.PayloadJson`) carries the path and the coordinates. **Photo bytes are not synced** (a real server would need a file upload, outside the stub) | done |
| Map of inspections | `MapPage` / `MapViewModel`: every operation with a location as a pin (`ObservableCollection<LocatedOperation>`, `ItemsSource` bound) and as a list row with **Open in maps** (`IMapLauncher`, `Map.Default.OpenAsync`). Link: *Map* button on the product list; every role | done in code; **Google Maps key is an owner action** |
| Windows alternative | `PinMapView` (a `ContentView`, section 4) shows the `Microsoft.Maui.Controls.Maps.Map` control only on Android and iOS; on Windows it hides itself and the page shows the list with *Open in maps* buttons (the system maps app). The product history also has *Open in maps* per located operation | done |
| Shake → new incident | `IShakeDetector` (accelerometer `ShakeDetected`) and `IShakeGesture` (one per page): on the product list a shake opens the scanner ("quick scan"); on the scanner a shake scans again. Started in `OnAppearing`, stopped in `OnDisappearing` through `IPageLifecycleAware`; 2 s debounce with `IClock`; UI work through `IMainThread`; not started when signed out; Settings switch *Shake to scan* (default on) | done in code; **a real shake needs a device (owner action)** |
| Connectivity indicator | `ConnectivityBannerViewModel` on the product list: "Offline", "Offline: N changes waiting for a connection", "Syncing...", "N changes waiting to sync", "All changes synced". Bound to the existing `IConnectivityMonitor`, `IOfflineSimulator` and `ISyncService` events (new: `IsSyncing`, `Started`); nothing polls. Auto-sync on reconnect is `SyncService` since Lab 4; Lab 7 adds the visible state and an end-to-end test | done |
| Denial / GPS off / no camera / cancel | `DeviceStatus` results, one message and one log line each (section 5, the matrix) | done |
| Permissions with a rationale | `IPermissionService` (`PermissionService` over `IPlatformPermissions`): the user reads why, then the system prompt appears (question 1) | done |

Not in the lab's text but needed: the product detail page and the form share one `ProductPhotoEditor` (DRY), a role rule (`RolePermissions.CanManagePhotos`, Manager), and a Windows fallback for every feature ("Windows must stay usable", AGENTS 2a): no camera → *Take photo* is hidden and *Pick photo* works, no accelerometer → the Settings switch is disabled with a note, no Map control → the list, no location sensor → a clear message.

## 4. Decisions

Also in the README decision log (65-74).

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| The photo is a file; the row has a **relative** path only, validated in the model, the service and a `CHECK` | Store the image as a BLOB; store an absolute path | A BLOB bloats the database that Settings shares and the sync queue (question 4). An absolute path breaks when the app data folder moves (reinstall, Android "restore", Windows user change). The path is checked three times (`PhotoPaths.IsValidRelative`, `ProductService`, `CHECK ... NOT IN ('/', '\') AND instr(...'..') = 0`) so a bad value can never point outside the photos folder |
| Photo bytes are not synced; the path and the coordinates are | Upload the file in `ISyncApi` | The stub server and the `ISyncApi` contract carry one JSON payload per entity. A file upload (multipart, resume, size limits) is a feature of its own and belongs with a real server. Documented limit: on another device the path resolves to no file and the thumbnail is empty |
| `Operations.Latitude` / `Longitude` as two `REAL` columns with a `CHECK` on each, the pair rule on the second | A `Locations` table; one text column "lat,lon" | 3NF is kept (a place is an attribute of the operation, not an entity); two numeric columns can be range-checked by the database and are sent as numbers in the sync payload. SQLite accepts `CHECK` in `ALTER TABLE ADD COLUMN` (verified: `DeviceMigrationTests` insert out-of-range and one-sided values on a migrated file and expect `SQLiteException`). The pair rule is on `Longitude` because it must refer to the already existing `Latitude` |
| `GeoPoint` is an immutable record that cannot be built invalid | Two `double?` on `Operation` | One place validates the range (`TryCreate`, constructor); `Operation.Location` is `null` or valid. `Operation` stays immutable |
| `DeviceStatus` results instead of exceptions between services and ViewModels; `IDeviceFeedback` turns a status into **one user message and one log line** | Each ViewModel catches `FeatureNotSupportedException` and friends and writes its own text | Eight outcomes x four commands would be 32 hand-written catch blocks and messages. The services (`MauiPhotoService`, `MauiLocationService`, `MauiMapLauncher`) translate platform exceptions once; the texts and levels live in `DeviceMessages` / `DeviceFeedback` (matrix below); ViewModels cannot forget the log line |
| `IPermissionService` always shows the rationale before the system prompt and asks `IPlatformPermissions` (the only class that touches `Permissions`) | Show it only when `ShouldShowRationale`; call `Permissions` from the service | `ShouldShowRationale` is Android-only and false on the first request, so the user would see an unexplained system dialog. An explanation before the first prompt is the lab's own advice and costs one dialog. The platform part is a 30-line class, so the flow is unit-tested with a fake |
| Photos need **no** runtime permission on Android and Windows | Request `READ_MEDIA_IMAGES` / `READ_EXTERNAL_STORAGE` | `MediaPicker.PickPhotoAsync` uses the system photo picker, which gives the app only the chosen file. A broad media permission would be refused by the Play review for no benefit. iOS has a real `Photos` permission, handled in `MauiPlatformPermissions` (`#if IOS`) |
| `PhotoStore` (plain `System.IO` copy) is separate from `MauiPhotoService` (picker + permission) | One class | `FileResult.OpenReadAsync` cannot be faked (it needs a platform file), so the copy, the naming and the deletion are tested in `PhotoStoreTests` on a temporary folder, and every other outcome of the service is tested with a fake `IMediaPicker`. The one thing not unit-tested is a real `FileResult` read: a device check |
| A photo is **staged** (`ProductPhotoEditor`): the page holds the new relative path until Save; Save deletes the replaced file, Cancel / back deletes the new one | Save the product at once on every photo; keep the bytes in memory | Matches the form: nothing reaches the database before Save. The path is in the draft (`ProductFormDraft.PhotoPath`, `ProductDetailDraft.PhotoChanged/PhotoPath`), so a killed app restores it. Known limit: leaving by a system gesture that bypasses `Cancel` leaves one orphan file in `photos/` (a few KB to MB, no data loss) |
| Photo commands are the Manager's (`CanManagePhotos`), guarded by `IPermissionGuard` and in `ProductService.SetPhotoAsync` | Everybody may photograph | A photo changes a product, and product changes are the Manager's (role table). The UI hides the buttons, the command refuses with an alert and a `Warning`, the service refuses again (the two mandatory layers, AGENTS 2a) |
| The map page = one `PinMapView` (`ContentView`) whose body exists only `#if ANDROID \|\| IOS`, plus a list that is always there; the package `Microsoft.Maui.Controls.Maps` is referenced for those two targets only | `OnPlatform` in XAML (still needs the type on Windows); two pages; `#if` in the page code-behind | The same pattern as `CameraScannerView` (ZXing): no map type is ever compiled on Windows, XAML and code-behind stay free of platform code, and the list gives Windows (and a device without a Maps key) a working page. `GeoPointToLocationConverter` keeps MAUI's `Location` out of the ViewModel |
| Google Maps API key as a placeholder in `AndroidManifest.xml` | Commit a key; read it from a file | A key in git is a leaked key. Without it the Android map shows no tiles but the page, the list and *Open in maps* work. Owner action below |
| `IShakeDetector` counts `Start` / `Stop`; `IShakeGesture` is one object per page with a debounce window of 2 s on `IClock` | One shared handler; `Start` / `Stop` as a flag | Navigation list → scanner can run `scanner.OnAppearing` before `list.OnDisappearing`; with a flag the second `Stop` would switch the sensor off under the scanner (tested: `ListThenScanner_TheSensorKeepsRunning`). Debounce: one physical shake raises several `ShakeDetected` events and a second movement follows at once; without it three scanner pages would stack |
| A shake on the scanner means "scan again", not "open the scanner" | Navigate to the scanner again | The page is already open; pushing it a second time would stack identical pages |
| Shake is off when nobody is signed in (`ICurrentUserService`) and when Settings switches it off | Always on | The scanner is for signed-in users (Lab 6); an accidental shake on the login page must not navigate |
| `ConnectivityBannerViewModel` (reusable) fed by events; `ISyncService` gets `IsSyncing` and `Started` | Properties on `ProductListViewModel`; poll the queue every few seconds | The Settings page needs the same states later (DRY), the events already exist, and polling wakes the radio and the database for nothing. Only "syncing" had no signal, so one event was added |
| Android: `CAMERA` (existing), `ACCESS_FINE_LOCATION` + `ACCESS_COARSE_LOCATION`, `uses-feature` `required="false"` for camera, location, GPS and accelerometer, `<queries>` for the camera and `geo:` intents | Mark the features required | `required="false"` keeps the app installable and working on a device or emulator without them (manual barcode entry, no location, no shake). `<queries>` are needed from Android 11 to find the camera and maps apps |

## 5. The error-handling matrix

Each row is one command outcome: **one message for the user and one log line**. Messages are in `DeviceMessages`, levels in `DeviceFeedback` (Information for something the user chose, Warning for something expected but unwanted, Error for an unexpected failure with the exception). Nothing here crashes the app.

| Situation | Where it is detected | Message the user reads | Log |
|-----------|----------------------|------------------------|-----|
| Camera permission refused in the system prompt, or the user answers "Not now" to the explanation | `PermissionService` → `PhotoResult(PermissionDenied)` | "Camera permission was denied. Allow the camera for StockRoom in the device settings, or pick a photo from the gallery." | Warning `Camera is not available: PermissionDenied` |
| Device has no camera (`IMediaPicker.IsCaptureSupported` false, `FeatureNotSupportedException`) | `MauiPhotoService` → `NotSupported` | "This device has no camera StockRoom can use. Pick a photo from the gallery instead." (the *Take photo* button is also hidden) | Warning |
| User closes the camera | `FileResult` is `null` → `Cancelled` | "No photo was taken." | Information |
| Gallery access refused (iOS) | `PermissionService` → `PermissionDenied` | "Access to your photos was denied. Allow it in the device settings, or take a photo with the camera." | Warning |
| Picking is not supported | `FeatureNotSupportedException` | "Picking a photo is not supported on this device." | Warning |
| User closes the gallery | `null` → `Cancelled` | "No photo was picked." | Information |
| Copy fails (disk full, locked file) or any other exception from the picker | `MauiPhotoService` → `Failed(exception)` | "The photo could not be taken. Try again." / "The photo could not be added. Try again." | **Error** with the exception |
| Non-Manager presses a photo button (stale screen, test) | `IPermissionGuard` | alert "Not allowed": "Your role (Driver) cannot change product photos." (also in the status line) | Warning `Denied: Driver (user ...) cannot change product photos` |
| Photo requested with a bad barcode in the form | `ProductFormViewModel` | "Enter a valid barcode first: the photo file is named after it." | Warning |
| Location permission refused | `PermissionService` → `PermissionDenied` | "Location permission was denied. Allow it in the device settings to attach your location." | Warning |
| GPS / location services are off (`FeatureNotEnabledException`) | `MauiLocationService` → `NotEnabled` | "Location services are off. Turn on GPS or location in the device settings and try again." | Warning |
| No location hardware (`FeatureNotSupportedException`) | → `NotSupported` | "This device cannot provide its location." | Warning |
| No fresh fix in 10 s and nothing remembered, or a PC without a location sensor | `null` from both calls → `Failed` | "Your location could not be determined. Check that location services are on and try again." | Error (no exception) |
| Unexpected exception from the sensor | → `Failed(exception)` | same | **Error** with the exception |
| No maps app (`FeatureNotSupportedException`) | `MauiMapLauncher` → `NotSupported` | "No maps app is available on this device." | Warning |
| The maps app cannot be opened | → `Failed` | "The maps app could not be opened." | Error |
| No accelerometer | `MauiShakeDetector.Start` returns `false` | none on the pages; Settings shows "This device has no accelerometer, so shaking is not available here." and disables the switch | Warning `Shake detection is not available` |
| The sensor fails to start | `MauiShakeDetector.Start` catches it | none (the feature just stays off) | Warning with the exception |
| The shake action throws (navigation failed) | `ShakeGesture.RunAsync` | none | Error with the exception; the gesture keeps working |
| The permission check itself throws | `PermissionService` catch | as "not supported" | Error with the exception |
| Offline | `ConnectivityBannerViewModel` | banner "Offline: 2 changes waiting for a connection" | `SyncService` already logs `Sync skipped: offline` (Warning) |

The permission-denied, GPS-off, no-camera and cancel lines are what the report's "log excerpt" should show. They look like this in the log file (`LogFormat`: timestamp, level, class, message); the real lines with real timestamps come from a device run (Owner actions), none is reproduced here:

- `[WRN] DeviceFeedback Location is not available: NotEnabled`
- `[WRN] DeviceFeedback Camera is not available: PermissionDenied`
- `[INF] DeviceFeedback Camera was cancelled by the user`
- `[WRN] PermissionGuard Denied: Driver (user <id>) cannot change product photos`

Coordinates, photo paths and the counterparty are never logged (the operation line only says `location: True`).

## 6. Permissions and their justification

| Permission | Platform | Why | Without it |
|------------|----------|-----|------------|
| `CAMERA` | Android (existing, now also for photos); iOS `NSCameraUsageDescription` | Scanning barcodes and QR codes (Lab 3) and taking a product photo (Lab 7) | Manual barcode entry; *Pick photo* from the gallery |
| `ACCESS_FINE_LOCATION` | Android; iOS `NSLocationWhenInUseUsageDescription` | A precise GPS position for "Attach my location" (a drop-off point). Asked only when the user presses the button, never in the background | The operation is saved without a location |
| `ACCESS_COARSE_LOCATION` | Android | Declared together with FINE: Android 12+ shows both and lets the user grant only the approximate one; the app works with it | same |
| gallery | iOS `NSPhotoLibraryUsageDescription`; **none** on Android and Windows (system photo picker) | Choose a photo of a product | Take a photo with the camera |
| `USE_BIOMETRIC` | Android (Lab 6) | Fingerprint / face unlock of the stored session | Password login |
| `ACCESS_NETWORK_STATE`, `INTERNET` | Android (Labs 3-4) | Connectivity status and the sync / Open Food Facts requests | Offline mode |
| *accelerometer* | none needed on any platform | Shake to scan; declared only as `uses-feature required="false"` | Shake is off |
| `uses-feature` camera, location, GPS, accelerometer | Android | All `required="false"`: the app must install and work on a device or emulator without them | n/a |
| `<queries>` `IMAGE_CAPTURE`, `geo:` | Android 11+ | Package visibility to find a camera app and a maps app | The pickers / maps may report "not supported" |
| Google Maps API key (`<meta-data>`) | Android | Draws the tiles of the embedded map | The map area is blank; list and *Open in maps* still work |

Windows (`WindowsPackageType` = `None`, unpackaged): no manifest capabilities are needed or declared; the camera and location dialogs of the OS (if any) are handled as in section 5.

## 7. Changes in the PR

- `Data/Migrations/0004_device.sql`; `Models/GeoPoint`, `Product.PhotoPath`, `Operation.Location`; `ProductRow`, `OperationRow`, both Sqlite repositories.
- `Services/`: `DeviceFeature`, `DeviceStatus`, `PermissionResult`, `IPlatformPermissions`, `MauiPlatformPermissions`, `IPermissionService`, `PermissionService`, `IDeviceFeedback`, `DeviceFeedback`, `DeviceMessages`, `IAppDataPathProvider`, `MauiAppDataPathProvider`, `PhotoPaths`, `PhotoStore`, `PhotoResult`, `IPhotoService`, `MauiPhotoService`, `LocationResult`, `ILocationService`, `MauiLocationService`, `IShakeDetector`, `MauiShakeDetector`, `IShakeSettings`, `ShakeSettings`, `IShakeGesture`, `ShakeGesture`, `IMapLauncher`, `MauiMapLauncher`, `IMainThread`, `MauiMainThread`, `LocatedOperation`, `ConnectivityState`; changed: `IStockService` / `StockService` (location), `IProductService` / `ProductService` (photo), `IProductCatalog` / `ProductCatalog` (`GetLocatedOperationsAsync`, photo path, history location), `ISyncService` / `SyncService` (`IsSyncing`, `Started`), `RolePermissions.CanManagePhotos`, `OperationHistoryItem`, `ProductSummary`, `OperationDraft`, `ProductFormDraft`, `ProductDetailDraft`, `Routes.Map`.
- `ViewModels/`: `ProductPhotoEditor`, `MapViewModel`, `ConnectivityBannerViewModel`; changed: `ProductFormViewModel`, `ProductDetailViewModel`, `OperationViewModel`, `ProductListViewModel`, `ScannerViewModel`, `SettingsViewModel`.
- `Views/`: `MapPage`, `PinMapView`, `Converters/GeoPointToLocationConverter`; photo, location, map and banner controls on the existing pages (all with `AutomationId`); `Thumbnail` style; `MauiProgram` (Essentials interfaces and Maui implementations registered, `UseMauiMaps()` under `#if ANDROID || IOS`); `ServiceCollectionExtensions`; csproj `Microsoft.Maui.Controls.Maps` for Android and iOS.
- Platform: `AndroidManifest.xml`, `Platforms/iOS/Info.plist`.
- Tests (1206 in total, 950 before): migration and constraints (`DeviceMigrationTests`), `GeoPointTests`, `PhotoPathsTests`, `PhotoStoreTests`, `PermissionServiceTests`, `DeviceFeedbackTests`, `MauiPhotoServiceTests`, `MauiLocationServiceTests`, `MauiShakeDetectorTests`, `MauiMapLauncherTests`, `ShakeGestureTests`, `ShakeSettingsTests`, `StockServiceLocationTests`, `ProductServicePhotoTests`, `ProductCatalogLocationTests`, `ProductPhotoEditorTests`, `ProductListBannerTests`, `ProductFormPhotoTests`, `ProductDetailPhotoTests`, `OperationLocationTests`, `MapViewModelTests`, `ShakeLifecycleTests`, `ConnectivityBannerViewModelTests`, `SettingsShakeTests`, `GeoPointToLocationConverterTests`, `EndToEnd/DeviceFlowTests` (sign in → attach a fake location → save → history → map; photo flow; role matrix), `EndToEnd/ConnectivityFlowTests` (offline → queued → online → auto-sync → banner). New fakes: `FakePlatformPermissions`, `FakePermissionService`, `FakeMediaPicker`, `FakeGeolocation`, `FakeAccelerometer`, `FakeMap`, `FakePhotoService`, `FakeLocationService`, `FakeMapLauncher`, `FakeShakeDetector`, `FakeShakeSettings`, `FakeMainThread`, `FakeAppDataPath`.
- Both targets build: Windows (`net10.0-windows10.0.19041.0`) and Android (`net10.0-android`).

## 8. Open questions / next steps

- Real device behaviour is not covered by tests: the system permission prompts, a real camera `FileResult` copy, GPS with the location switched off, a real shake, tiles of the Google map (owner actions).
- A photo is not uploaded: another device sees the path but not the picture (decision 66).
- A photo staged when the user leaves by a system gesture that skips *Cancel* stays on disk (orphan file).
- Location coordinates in an unsent operation draft are kept in `Preferences` in clear text (like the reason); they are not secret, the counterparty is the encrypted one.
- Shake sensitivity is the platform default of `Accelerometer.ShakeDetected`; no tuning.
- The Settings page still has its own sync text; it could use `ConnectivityBannerViewModel` (it was not changed to keep the diff small).
- iOS and MacCatalyst were not built (no Mac); the iOS strings and the `#if IOS` permission branch are untested.

## 9. Answers to the control questions

**1. Why must a permission be requested at run time and not only declared in the manifest? What is a rationale and when is it shown?**
The manifest only tells the system and the store what the app may ask for; since Android 6 and on iOS a dangerous permission (camera, location, photos) is granted by the user, per app, at the moment of use, and can be revoked in Settings at any time. So the code must check the status before every use (`IPermissionService.EnsureAsync` -> `IPlatformPermissions.CheckAsync`), ask when it is missing (`RequestAsync`) and handle "no". A rationale is the explanation of why the app needs the permission; it is shown **before** the system prompt, especially after the user has refused once (Android's `ShouldShowRequestPermissionRationale`), because the system dialog says what is asked but not why. StockRoom shows it every time the permission is not yet granted: `PermissionService` asks `IAlertService.ConfirmAsync("Permission needed", DeviceMessages.Rationale(feature), ...)` and only on "Continue" calls the system prompt; "Not now" is a `Denied` (tests: `PermissionServiceTests`).

**2. Why wrap `MediaPicker`, `Geolocation` and the sensors in services with interfaces instead of calling the static APIs from a ViewModel?**
Statics cannot be replaced, so a ViewModel that calls `MediaPicker.CapturePhotoAsync()` can only be tested on a device with a camera and a person who taps. Behind `IPhotoService`, `ILocationService`, `IShakeDetector`, `IMapLauncher` and `IMainThread` the ViewModel is tested with fakes (`FakePhotoService`, `FakeLocationService`, `FakeShakeDetector`, ...): each outcome - granted, denied, no camera, cancel, GPS off, failure - is a line of test code (`ProductPhotoEditorTests`, `OperationLocationTests`). The platform exceptions are translated once in the `Maui*` class (`FeatureNotSupportedException` -> `NotSupported`, ...), so no ViewModel repeats `try/catch` blocks and none depends on MAUI (which Lab 8 needs for the Core library). The Maui classes themselves take the Essentials **interfaces** (`IMediaPicker`, `IGeolocation`, `IAccelerometer`, `IMap`) from DI and are tested with fakes of those (`MauiLocationServiceTests`, `MauiShakeDetectorTests`). Dependency inversion also means Windows can have a different behaviour (no camera, no map) behind the same contract.

**3. How do `GetLastKnownLocationAsync` and `GetLocationAsync` differ and when is each right?**
`GetLastKnownLocationAsync` returns the position the OS cached from an earlier fix, immediately and without switching the GPS on, so it is fast and cheap but may be minutes or hours old, or `null` (no fix since boot, location switched off). `GetLocationAsync(new GeolocationRequest(accuracy, timeout))` asks the sensors for a fresh fix with the requested accuracy and waits up to the timeout, so it is accurate and current but slower and costs battery, and may time out (`null`). Use the last known position for a rough "where am I" or as a fallback when the fresh one is unavailable; use a fresh one when the value is recorded as a fact. `MauiLocationService` does exactly that: a fresh `Medium` fix with a 10-second timeout, and only if that gives nothing, the last known position (`MauiLocationServiceTests.NoFreshFix_FallsBackToTheLastKnownPosition`); when both are empty the user gets "Your location could not be determined".

**4. Why store photos in the app's file system and only the path in the database?**
A photo is hundreds of kilobytes to several megabytes. As a BLOB it makes every row read heavier, grows the database file that Settings shares and backs up, and would go into the sync queue payload on each change. The file system is made for large binary files (streaming, caching by the image control, the OS can free cache), while the database keeps what it is good at: small, queryable, constrained values. The path must be **relative** to the app data folder because the absolute folder differs per user, device and reinstall (`photos/4006381333931_20260115_093000.jpg` stays valid, `C:\Users\...` does not). The cost is two things to keep consistent, which the app handles: the file is deleted when the photo is replaced or removed (`ProductPhotoEditor.Commit/Discard`), and the path is validated so it cannot point outside the photos folder (`PhotoPaths`, the `CHECK` in `0004_device.sql`).

**5. What happens if a sensor is not stopped when the user goes to another page, and how is it avoided?**
The accelerometer keeps running and delivering events: it drains the battery, the handler (and the page that owns it) is kept alive and cannot be collected (a leak), and the handler fires while the page is not visible, so a shake can navigate or change state behind another screen, or throw because the page is gone. In StockRoom the sensor is started in `OnAppearing` and stopped in `OnDisappearing` of `ProductListViewModel` and `ScannerViewModel` (`PageLifecycleBehavior` forwards the page events to `IPageLifecycleAware`); `ShakeGesture.Stop` unsubscribes the handler and `MauiShakeDetector.Stop` unsubscribes from `ShakeDetected` and calls `Accelerometer.Stop()` when the last user has stopped it (counted, so two pages cannot switch it off under each other). `ShakeLifecycleTests` asserts start on appearing, stop on disappearing, no handler left, and that a shake after `OnDisappearing` does nothing.

**6. Why must the UI be updated from a sensor event handler through `MainThread.BeginInvokeOnMainThread`?**
Sensor, location and camera callbacks arrive on a background thread, and UI controls (and anything bound to them: `ObservableCollection` changes, navigation) may only be touched on the UI thread: on Android a view touched from another thread throws `CalledFromWrongThreadException`, on Windows WinUI throws a marshalling error, and elsewhere you get races and lost updates. `BeginInvokeOnMainThread` queues the action to the UI thread's message loop. In StockRoom no ViewModel touches the static: `ShakeGesture` receives `Shaken` on the sensor thread, applies the debounce there, and runs the action through `IMainThread.BeginInvoke` (`MauiMainThread` -> `MainThread.BeginInvokeOnMainThread`); the test double `FakeMainThread` runs it inline, so the behaviour is testable. Property changes on a ViewModel raised from a background thread (the connectivity banner, sync events) are marshalled by the MAUI binding engine.

## Owner actions

These cannot be done by the agent and are **not** claimed as verified:

1. **Android device (or emulator with a camera and a location set in Extended controls):**
   - Product form (sign in as `olena`): enter a valid barcode, **Take photo** -> the explanation dialog, then the system camera prompt. Screenshot of the prompt and of the thumbnail. Then **Pick photo**.
   - **Denial:** Settings -> Apps -> StockRoom -> Permissions -> deny Camera, press **Take photo** again; screenshot of the message "Camera permission was denied..." (permanent denial on Android 11+ answers at once without a dialog).
   - **Location:** sign in as `ivan` (Driver), open a product, **Record In/Out**, **Attach my location** -> prompt -> "Location attached: ..." screenshot; Save. Then switch device location off and press it again: "Location services are off..." screenshot (GPS-off case).
   - **Map:** the *Map* button on the list. Put a real key in `Platforms/Android/AndroidManifest.xml` (`com.google.android.geo.API_KEY`, Google Cloud Console -> Maps SDK for Android, restrict it to the package name and the debug SHA-1) to see the tiles; screenshot of the map with pins and of the list; **Open in maps** from the list and from a product history line.
   - **Shake:** on the product list shake the phone (emulator: Extended controls -> Virtual sensors -> Move, "Shake") -> the scanner opens; Settings -> *Shake to scan* off -> nothing happens.
   - **Offline banner:** Settings -> *Simulate offline* (or airplane mode), record an operation, return to the list: "Offline: 1 change waiting for a connection"; back online: "Syncing..." then "All changes synced".
2. **Windows:** run the exe: *Pick photo* works through the file dialog, *Take photo* is hidden without a camera, the map page shows the list and **Open in maps** opens the Maps app, Settings explains that shake is unavailable.
3. **Log excerpt for the report:** Settings -> Diagnostics -> Open logs (as `olena`), filter Warning, screenshot or copy of the lines `Camera is not available: PermissionDenied`, `Location is not available: NotEnabled`, `Camera was cancelled by the user` (the device log file is in the app data folder, `logs/`).
4. Report items from the lab: the repository link (branch `lab7`), the permission list (section 6), the screenshots above, the answers (section 9).
5. Review the PR; the `lab7` -> `lab6` pull request is opened by the owner (no `gh` on this machine).
