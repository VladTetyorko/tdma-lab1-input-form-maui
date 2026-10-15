# Step 03 — App and page lifecycle, DI, saved state, theme, deep links, scanner

- **Lab:** 3 (`docks/` — "Життєвий цикл компонентів застосунку")
- **Branch:** `lab3` (on top of `lab2`)
- **PR:** _to be added_

## 1. Before

After Lab 2 the app had six pages (login, list, add-product form, detail,
characteristics, operation) wired with `INavigationService`, DI for services,
pages and ViewModels, and in-memory data seeded at start. Nothing reacted to
the app being minimised: the signed-in user, the open form and every typed
character lived only in memory and in the page. Pages loaded their data in
`OnAppearing` overrides repeated in each code-behind. Two known gaps:

- characteristic edits on the detail page were lost when the user went to the
  operation page and came back (the detail page reloads in `OnAppearing`);
- the "scan a barcode" feature promised in the vision had no code at all.

There was one fixed light/dark behaviour (follow the device), one hard-coded
error color that was too dark on the dark background, and no way to open a
product from outside the app.

## 2. What the lab asks for

Translated from the assignment (the lab uses `RailwayErp` / `TrainListPage`):

**In class (6 points)**

1. Handle the `Stopped`, `Resumed` and `Destroying` events of the app/window
   and log them.
2. Save the state of an edit form (for example the current route) when the app
   is minimised and restore it when the user comes back.
3. On the list page call the load command from `OnAppearing` and log the
   transition in `OnDisappearing`.
4. Register an app-state service (`IAppStateService`) in `MauiProgram.cs` as a
   singleton and use it to keep the current session state.

**Independent (4 points)**

5. A light/dark theme switch on a settings screen, applied to every page and
   saved with `Preferences`.
6. Deep link that opens one record (a train's details) from an outside link.

Theory sections the report must cover: app states, page lifecycle, DI
registration, saving state, Android background limits, themes, deep linking.

## 3. Our interpretation

The sample domain is a railway ERP; ours is a warehouse. The "train" is a
product, the "route being edited" is the form the user is filling in. The
plan adds one more feature to this lab, the **barcode scanner**, because it is
the natural user of the page lifecycle (the camera must stop in
`OnDisappearing`) and of deep links (a barcode identifies a product).

| Lab task | In StockRoom | File |
|----------|--------------|------|
| 1. `Stopped` / `Resumed` / `Destroying` handlers + log | `App.CreateWindow` subscribes to all six window events (`Created`, `Activated`, `Deactivated`, `Stopped`, `Resumed`, `Destroying`) and only forwards them to `IAppLifecycleService`, which logs each one with `ILogger`; `Stopped` and `Destroying` save the state | `App.xaml.cs`, `Services/IAppLifecycleService.cs`, `Services/AppLifecycleService.cs` |
| 2. Save a form on minimise, restore on return | Three drafts: add-product form, operation form, and the unsaved characteristic edits on the detail page. A draft is kept in `IAppStateService` as the user types and written to preferences on `Stopped`/`Destroying`. After a cold start the user is put back on the page they were on | `ViewModels/ProductFormViewModel.cs`, `OperationViewModel.cs`, `ProductDetailViewModel.cs`, `Services/*Draft.cs`, `Services/StartupNavigator.cs` |
| 3. `OnAppearing` → load command, `OnDisappearing` | `IPageLifecycleAware` on the ViewModel + `PageLifecycleBehavior` attached to the page in XAML: `Appearing` → `OnAppearing()` (runs the load command), `Disappearing` → `OnDisappearing()` (the scanner stops the camera). No lifecycle code is left in code-behind | `ViewModels/IPageLifecycleAware.cs`, `Views/PageLifecycleBehavior.cs`, `Views/*Page.xaml` |
| 4. `IAppStateService` singleton | Registered in `AddStockRoomCore` as a singleton; holds the signed-in user id, the last route and the drafts; persists through `IPreferencesStore` (JSON for drafts) | `Services/IAppStateService.cs`, `Services/AppStateService.cs`, `Services/IPreferencesStore.cs`, `Services/MauiPreferencesStore.cs` |
| 5. Theme switch | Settings page with a System / Light / Dark picker; `IThemeService` saves the choice in preferences and applies it through `IThemeApplier` (`Application.Current.UserAppTheme`). Styles already used `AppThemeBinding`; the error color got a lighter dark-theme variant | `Views/SettingsPage.xaml`, `ViewModels/SettingsViewModel.cs`, `Services/ThemeService.cs`, `Services/MauiThemeApplier.cs`, `Resources/Styles/*.xaml` |
| 6. Deep link | `stockroom://product/{barcode}` is parsed by the pure `DeepLinkParser`; `DeepLinkHandler` finds the product and navigates to the detail page through `INavigationService`, or shows a friendly message. Android: intent filter + `OnCreate`/`OnNewIntent`. Windows: command-line argument. Anywhere: an "Open a link" box on the settings page | `Services/DeepLinkParser.cs`, `Services/DeepLinkHandler.cs`, `Platforms/Android/MainActivity.cs`, `Platforms/Windows/App.xaml.cs` |
| (plan) Background work | Nothing runs in the background; see decision 11 | this file |
| (plan) Barcode scanner | `IBarcodeScanner` + scanner page: scan or type a barcode → known product opens the operation page; unknown barcode → a Manager can add the product (form prefilled), others see a message. Camera (ZXing.Net.Maui) on Android/iOS, manual entry on Windows | `Services/IBarcodeScanner.cs`, `ViewModels/ScannerViewModel.cs`, `Views/ScannerPage.xaml`, `Views/CameraScannerView.cs` |
| (plan) Everything through DI | Verified: pages, ViewModels and services are only created by the container (no `new` of a page or ViewModel anywhere, no `BindingContext` in XAML). `AppShell` is the one object created with `new`; it holds no dependencies | `MauiProgram.cs`, `ServiceCollectionExtensions.cs` |

## 4. Decisions

| # | Decision | Alternatives considered | Why |
|---|----------|-------------------------|-----|
| 1 | `App` only wires window events to `IAppLifecycleService` | Logic in `App.xaml.cs` | The service is a plain class: testable with fakes, no `Application` needed |
| 2 | `IAppStateService` keeps state in memory while the app runs and writes to preferences only on `Stopped`/`Destroying` (`Save`), reads it once at start (`Restore`) | Write to preferences on every keystroke | Fewer writes; the lab's model (save on stop, restore on start). Warm resume needs nothing: the process is alive and the memory is intact, so `Resumed` only logs |
| 3 | `IPreferencesStore` (string key/value) over `Preferences`; drafts are one JSON dictionary under one key | Typed `Get<T>` API; `Preferences` called directly | ViewModels and services never touch a platform static; fake is a dictionary |
| 4 | One draft per draft **type**, keyed by the type name (`GetDraft<ProductFormDraft>()`) | String keys | No key constants to keep in sync; DRY |
| 5 | Operation and detail drafts name the product by **barcode**, not by id | Product id | The in-memory data is rebuilt on each start, ids change, a barcode does not (and is the same after Lab 4) |
| 6 | Seeded users get fixed ids (`DataSeeder.ManagerId`…) | Random ids; restore the user by name | The saved session names the user by id; without fixed ids no session could ever be restored while data is in memory. Lab 4 seeds the same ids |
| 7 | Drafts are cleared by Save, Cancel/back and sign-out; they stay when the page is left for another page that returns (detail → operation → detail) and when the app is stopped | Clear on `OnDisappearing` | `OnDisappearing` also fires on minimise, which is exactly when the draft must stay. This is also the fix for the Lab 2 gap: the detail page's reload now re-applies the draft |
| 8 | The form pages bind the back arrow and the hardware back button to Cancel (`ProductFormPage` now does too) | Default back | Back must clear the draft like Cancel does |
| 9 | Cold-start resume: `LastRoute` is saved on stop; at the next start `StartupNavigator` reopens the form/operation/detail page **only if a draft for it exists**, after the product list has appeared | Navigate in `Window.Created`; restore every page | Shell is not ready in `Created`; a page without a draft has nothing to restore (the detail and operation pages need a product, which the draft's barcode provides) |
| 10 | The session is restored before the first page: `AppShell(startRoute)` selects `products` or `login` | Navigate away from the login page after it appears | No flash of the login page, no race with `LoginViewModel` (which signs out on load) |
| 11 | No background work. The scanner is the only resource that must be released; the list does not poll | `Task.Run` timer, WorkManager periodic refresh | Data is in memory and has one writer, there is nothing to refresh. The first real background job is Lab 4's sync queue, which will be a lifecycle-aware loop that pauses on `Stopped`, and will say whether WorkManager is needed. See question 10 |
| 12 | `IPageLifecycleAware` + `PageLifecycleBehavior` | A `LifecyclePage` base class; overrides in each code-behind | The behavior is attached in XAML, keeps code-behind free of lifecycle code, and needs no change of the page's base type (DRY) |
| 13 | `IThemeApplier` is separate from `IThemeService` | One class calling `Application.Current` | The service (parse, save, load) is testable; only the 10-line applier touches `Application` |
| 14 | Theme is applied at start in `RestoreAsync` (before the window exists) | Apply in `Window.Created` | No flash of the wrong theme |
| 15 | Deep link `HandleAsync` (app running) vs `Defer` (app starting) vs `HandlePendingAsync` (product list shown) | Navigate at once everywhere | At start there is no Shell yet, and a signed-out user must sign in first; the link is kept and opened when the list appears |
| 16 | No `Application.OnAppLinkRequestReceived` override | Use it for Android/iOS | The explicit `OnNewIntent` forwarding is deterministic; having both would open the product twice |
| 17 | iOS deep link is not implemented (only the camera usage text is added) | `CFBundleURLTypes` + `OpenUrl` | Not requested, and cannot be compiled or tested on this machine |
| 18 | `IBarcodeScanner` = capability + permission (`IsCameraSupported`, `RequestCameraAccessAsync`); the camera picture is a `CameraScannerView` control that calls a command with the text it read | A service that raises events | The camera library is control-based, not headless. The ViewModel stays testable: tests call `BarcodeDetectedCommand` with a string |
| 19 | ZXing.Net.Maui.Controls 0.10.4, referenced **only** for android/ios TFMs; the Windows target has no reference and compiles the camera code out (`#if ANDROID \|\| IOS`) | BarcodeScanning.Native.Maui 3.1.0 | Both support net10. The other needs `Microsoft.Maui.Controls` 10.0.20 (a version bump of the whole app) and also pulls a Windows native dependency. ZXing works with the SDK's 10.0.0 |
| 20 | The camera view creates the reader when switched on and drops it when switched off | Keep the reader and toggle `IsDetecting` | Removing the reader from the page disconnects its handler, which releases the camera; `IsDetecting=false` would leave the preview running |
| 21 | After the first barcode is read the camera is switched off (`IsCameraActive=false`); an unknown barcode shows "Scan again" | Keep scanning | The camera reports the same code many times per second; one scan = one action |
| 22 | Scanner → operation/form pages return to the scanner; the scanner then sends `ProductsChanged` to the list, which reloads on its next `OnAppearing` | Pass one `UpdatedProduct` back | The scanner can touch several products in one visit; a reload of an in-memory list is cheap |
| 23 | `INavigationService.CurrentRoute` | Read `Shell.Current` in the lifecycle service | Keeps the platform static in the one navigation class |
| 24 | `Routes.Leaf` is a static pure helper | Parse in `ShellNavigationService` | Testable without Shell |

## 5. Changes in the PR

- **Services:** `IPreferencesStore`/`MauiPreferencesStore`, `IAppStateService`/`AppStateService`, `IAppLifecycleService`/`AppLifecycleService`, `IThemeService`/`ThemeService`/`ThemeSetting`/`IThemeApplier`/`MauiThemeApplier`, `DeepLinkParser`/`ProductDeepLink`/`IDeepLinkHandler`/`DeepLinkHandler`, `IStartupNavigator`/`StartupNavigator`, `IAlertService`/`ShellAlertService`, `IBarcodeScanner`/`CameraBarcodeScanner`/`ManualBarcodeScanner`, `ProductFormDraft`/`OperationDraft`/`ProductDetailDraft`; `INavigationService.CurrentRoute`, `IProductCatalog.GetSummaryByBarcodeAsync`, `Routes.Leaf`, `Routes.Settings`/`Scanner`, `NavigationKeys.Barcode`/`ProductsChanged`.
- **Data:** `IUserRepository.GetByIdAsync`; fixed seeded user ids.
- **ViewModels:** `IPageLifecycleAware` on Login, Products, Detail, Operation, Characteristics, Product form, Scanner; drafts in the product form, operation and detail ViewModels; `SettingsViewModel`, `ScannerViewModel`; the list gets `ScanCommand`, `OpenSettingsCommand`, clears the saved state on sign-out, reloads on `ProductsChanged`, and runs `IStartupNavigator`.
- **Views:** `PageLifecycleBehavior`, `SettingsPage`, `ScannerPage`, `CameraScannerView`; the code-behind of all pages lost its `OnAppearing`; `ProductFormPage` handles back as Cancel; the list's buttons wrap (`FlexLayout`).
- **Platforms:** Android intent filter, `OnCreate`/`OnNewIntent` link forwarding, `CAMERA` permission; Windows command-line link; iOS `NSCameraUsageDescription`.
- **Project:** conditional `ZXing.Net.Maui.Controls` reference; `AddLogging()` in `AddStockRoomCore`.
- **Styles:** `DangerDark` color; `ErrorLabel` uses `AppThemeBinding`. The red "out of stock" quantity (`Colors.Red`) was checked on both backgrounds and kept.
- **Tests:** the 310 existing tests pass. Constructors that gained `IAppStateService`/`IStartupNavigator` were updated; `FakePreferencesStore`, `FakeAlertService` and `FakeNavigationService.CurrentRoute` were added. The full Lab 3 suite is written separately.
- **Not compiled on this machine:** everything under `#if ANDROID || IOS`, `Platforms/Android/MainActivity.cs`, the Android manifest and the iOS plist (no Android SDK platform). The camera API calls were compiled against the library's `net10.0` assembly in a scratch project.

## 6. Open questions / next steps

- Real process death is only partly useful until Lab 4: the session, route and drafts come back, but the in-memory operations recorded in the last run do not. SQLite fixes that.
- `AppLifecycleService.RestoreAsync` is waited on synchronously in `CreateWindow` (like the seeding). With SQLite this should become an async start-up step.
- Screenshots for the report: minimise/restore with an open form, theme switch (light and dark), deep link on Android, scanner.
- A Windows camera could be added later with the same `IBarcodeScanner` (the library `BarcodeScanning.Native.Maui` supports Windows).

## 7. How to test manually

**Window events and logging.** Run in Debug and watch the Output/Logcat window: `Window created`, `Window activated`, `Window deactivated`, `Window stopped: saving state`, `Window resumed`, `Window destroying: saving state`.

**Draft survives minimising.**
1. Sign in as the Manager, open *Add product*, type a name and a barcode.
2. Minimise the app (Android: Home; Windows: minimise) and return: the text is there.
3. Kill the process while minimised (Android: swipe the app away from Recents, or `adb shell am kill com.companyname.mobile_apps_l1_model_view`), start the app: you are signed in, the product list opens and the *Add product* form opens on top with your text. Cancel clears the draft.
4. Repeat for the operation page (direction, units, counterparty, reason) and for characteristic edits on the detail page: edit a value, tap *Record operation*, go back: the edit is still there (the Lab 2 gap).
5. *Sign out* clears the saved session; restarting shows the login page.

**Theme.** *Settings* → Theme: Light, Dark, System. All pages change at once; error text (for example an invalid EAN in the add form) and the red `0 pcs` stay readable in both. Restart: the choice is kept.

**Deep link, Android.**
```
adb shell am start -a android.intent.action.VIEW -d "stockroom://product/4820001000016"
```
Signed in: the detail page of the Cordless Drill opens. Signed out (cold start): sign in, the product opens when the list appears. Unknown barcode (`stockroom://product/1`): "Product not found" message. A malformed link: "Link not supported".

**Deep link, Windows.** Either type the link into *Settings* → *Open a link*, or start the exe with the link as an argument:
```
Mobile_apps_L1_Model_view.exe stockroom://product/4820001000016
```
To let other programs open the link, register the scheme once (current user):
```
reg add HKCU\Software\Classes\stockroom /ve /d "URL:stockroom" /f
reg add HKCU\Software\Classes\stockroom /v "URL Protocol" /d "" /f
reg add HKCU\Software\Classes\stockroom\shell\open\command /ve /d "\"C:\path\to\Mobile_apps_L1_Model_view.exe\" \"%1\"" /f
```
Then `start stockroom://product/4820001000016` launches the app (a second instance is not merged into the first).

**Scanner.**
- Android/iOS: *Scan* → allow the camera → point it at an EAN-13 (the seeded codes are `4820001000016` … ; print one or show it on a screen). A known product opens the operation page; save returns to the scanner, the camera restarts. Deny the permission: a message appears and manual entry still works. Press Home or open another page: the camera is released.
- Unknown barcode as Manager: "Add this product" opens the form with the barcode filled; as Storekeeper/Driver: a message only.
- Windows: *Scan* shows the manual-entry box (no camera); type `4820001000016` and *Find product*.

## 8. Control questions

**7. Which main states does a .NET MAUI app go through during its life?**
A MAUI app lives in a `Window`, and the window raises an event at each change of state: `Created` (the window exists, resources are initialised), `Activated` (it is in the foreground and has focus), `Deactivated` (it lost focus but may still be visible: a call, a dialog, another window), `Stopped` (it went to the background; the process may be killed at any time without another event), `Resumed` (it came back from `Stopped`), and `Destroying` (the window is being closed). The usual order is `Created → Activated → (Deactivated → Activated)* → Deactivated → Stopped → (Resumed → Activated …)* → Destroying`. Mapping to platforms: Android `onCreate/onResume/onPause/onStop/onRestart/onDestroy`, iOS `DidBecomeActive/WillResignActive/DidEnterBackground/WillEnterForeground`. In StockRoom `App.CreateWindow` subscribes to all six events and passes them to `AppLifecycleService`, which logs them, saves the session, route and drafts on `Stopped` and `Destroying`, and restores them at the next start (`RestoreAsync`).

**8. What is the difference between `OnAppearing` and `OnNavigatedTo`?**
`OnAppearing` (event `Appearing`) belongs to the page's visibility: it fires every time the page becomes visible, including when the user comes back to it with the back button, when a modal page closes and when the app returns from the background on some platforms. It carries no information about where the user came from. `OnNavigatedTo` / `OnNavigatedFrom` (events `NavigatedTo` / `NavigatedFrom`, with `NavigatedToEventArgs` that include the previous page) belong to the navigation stack: they fire only when a navigation to or from this page has completed, so they are not raised when the app is merely minimised and restored. In StockRoom we use `Appearing` (through `PageLifecycleBehavior`) because the lists must refresh when the user returns from the operation page, and the camera must stop whenever the page is hidden. The data a page needs when it is opened arrives earlier and separately, through `IQueryAttributable.ApplyQueryAttributes`.

**9. Why register services in the DI container, and how do Singleton and Transient differ?**
With a container, a class asks for what it needs in its constructor (`IProductCatalog`, `INavigationService`) and does not know how it is built. That gives loose coupling (dependency inversion), makes it possible to replace an implementation (in-memory repositories now, SQLite in Lab 4; `ShellNavigationService` vs a fake in tests), lets one place control lifetimes, and lets MAUI build pages with their ViewModels automatically. A **singleton** is created once and shared by everyone for the life of the app: the right choice for state that must be common (`IAppStateService`, `ICurrentUserService`, the in-memory repositories, `IThemeService`). A **transient** is created anew on every request: the right choice for pages and ViewModels, so each opened page has its own fields and unsaved input does not leak from one visit to the next (`ProductFormViewModel`, `OperationViewModel`). (A third lifetime, *scoped*, gives one instance per scope; MAUI has no per-page scope, so we do not use it.) A transient must not be injected into a singleton as if it were fresh, and a singleton holding page state would survive navigation, which is why the drafts live in `IAppStateService` and the ViewModels stay transient.

**10. What limits does Android put on apps running in the background?**
Android protects battery and memory: when the screen is off and the device idle, **Doze** postpones jobs, alarms, network and wake locks into rare maintenance windows; **App Standby / standby buckets** restrict apps the user has not opened for a while; from Android 8 (**background execution limits**) a background app cannot start or keep ordinary services, and long work must be a **foreground service** with a visible notification (and, from Android 14, a declared service type); exact alarms and notifications need runtime permissions on recent versions; the system may kill a background process at any moment to free memory, without a callback after `onStop`. Periodic or deferrable work should use **WorkManager** (constraints such as network, minimum period 15 minutes, survives restarts), while `Task.Run` is for short work and stops with the process. Consequences for StockRoom: all state worth keeping is saved in `Stopped`, not in `Destroying`; the camera is released as soon as the page is hidden; and we start no background loop (decision 11). Lab 4's sync will be a loop that pauses on `Stopped`, with WorkManager as the option if sync must continue in the background.

**11. How is light and dark theme supported in MAUI?**
MAUI follows the device's theme through `Application.Current.RequestedTheme` (read-only, with the `RequestedThemeChanged` event). Resources are written for both themes in two ways: the `AppThemeBinding` markup extension (`{AppThemeBinding Light=..., Dark=...}`) on any property, or separate resource dictionaries swapped at run time. The user can override the device with `Application.Current.UserAppTheme = AppTheme.Light | Dark | Unspecified` (`Unspecified` means follow the system). Everything bound with `AppThemeBinding` restyles itself when either value changes. In StockRoom all styles in `Resources/Styles/Styles.xaml` use `AppThemeBinding` with colors from `Colors.xaml`; page and Shell backgrounds are themed; the error color has a lighter variant for the dark background. `SettingsPage` offers System/Light/Dark, `ThemeService` saves the choice in preferences and `MauiThemeApplier` sets `UserAppTheme`; the choice is applied at start before the first page is created.

**12. What is deep linking, and which mechanisms implement it on Android?**
A deep link is a URI that opens a specific screen of an app from outside it (a browser, a notification, another app, a QR code). On Android: the activity declares an **intent filter** in the manifest with action `VIEW`, categories `DEFAULT` and `BROWSABLE`, and a `<data>` element (scheme, host, path). A **custom scheme** such as `stockroom://product/{barcode}` is the simplest; **App Links** are `https://` links bound to the app by `android:autoVerify="true"` and a `assetlinks.json` file on the site, so they open the app without a chooser and cannot be hijacked by another app. The system starts the activity with the link in `Intent.Data`: a new activity gets it in `OnCreate`, a running one (`LaunchMode.SingleTop`) in `OnNewIntent`; the app reads it and navigates. In MAUI the link can be turned into a Shell route (`GoToAsync`). On iOS the counterparts are URL types and universal links, on Windows protocol activation. In StockRoom `MainActivity` has `[IntentFilter]` for `stockroom://product`, forwards the link to `IDeepLinkHandler` (`Defer` from `OnCreate`, `HandleAsync` from `OnNewIntent`), `DeepLinkParser` extracts the barcode, and `DeepLinkHandler` navigates to `productdetail` through `INavigationService` or shows "Product not found". Test with `adb shell am start -a android.intent.action.VIEW -d "stockroom://product/<barcode>"`.
