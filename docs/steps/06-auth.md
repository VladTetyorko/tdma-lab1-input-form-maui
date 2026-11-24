# Step 06 — Authentication, roles, security

- **Lab:** 6 (`docks/` lab 6 PDF: login screen with a token in `SecureStorage`, session service with the role, role-based UI, role check in a command, biometrics, certificate pinning)
- **Branch:** `lab6` (on top of `lab5`)
- **PR:** _to be added_

## 1. Before

"Authentication" was a list of the three seeded users: the login page showed their names and a tap signed one in. The app saved
the **id** of the signed-in user in `Preferences` (`state.user`) and trusted it at the next start. Roles were already enforced twice
(UI bindings `CanManage...` and refusals in `ProductService`, `CharacteristicService`, `StockService`, `DiagnosticsService`,
`LogsViewModel`), but the ViewModels showed the refusal in different ways and only some logged it. Nothing was hashed,
nothing was signed, there was no biometric check and the only HTTP client was the one for Open Food Facts.

## 2. What the lab asks for

Classroom (6 points):
1. A login screen (login and password) that calls an authentication service and stores the token in `SecureStorage`.
2. A session service with the current role, registered as a singleton.
3. A control hidden by role through a ViewModel binding.
4. A role check inside a critical command, with an explanation to the user when refused.

Self-study (4 points):
5. Biometric sign-in as an alternative for a user who already has a stored token.
6. Certificate pinning for the network requests of authentication.

Control questions 7-12 (section 7). The lab's own example is a railway ERP with a server that returns a JWT; StockRoom has no server
accounts (scope decision, `docs/plan.md` Lab 6), so the adaptation is in section 3.

## 3. Our interpretation

| Lab task | In StockRoom | Status |
|----------|--------------|--------|
| Login screen | `LoginPage` / `LoginViewModel`: login entry, masked password entry, error label, **Sign in** (enabled only when both are filled), **Unlock with biometrics**. AutomationIds `LoginEntry`, `PasswordEntry`, `LoginButton`, `ErrorLabel`, `BiometricButton`. The "pick a user" list is gone. `IAuthService.LoginAsync(login, password)` returns `Result<User>` | done |
| Accounts and hashes | Migration `0003_auth.sql`: `Users.Login` (unique index, NOCASE) and `Users.PasswordHash`; the three seeded users get the logins `olena`, `petro`, `ivan`. **Passwords are never stored**: `IPasswordHasher` / `Pbkdf2PasswordHasher` (PBKDF2-HMAC-SHA256, 16-byte random salt, 210 000 iterations, constant-time compare, `NeedsRehash`). The hash is `[JsonIgnore]` on `UserRow`, so it is not in the sync payload. Demo passwords are in the README | done |
| Brute force | 5 failed attempts per login lock it for 30 s (in memory, injected `IClock`); unknown and wrong logins are indistinguishable (same message, dummy-hash verify, both counted) | done |
| Token in secure storage | The lab's token comes from a server. Ours is made on the device: `ITokenService` / `HmacTokenService` signs `userId.expiry` (30 days) with a key kept in `ISecretStore` (`SecureStorage`); `ISessionStore` / `SessionStore` stores the token under one key and removes it on sign-out. At start `AppLifecycleService.RestoreAsync` validates the token instead of trusting a saved id; a bad, expired or unreadable token means the login page | done |
| Session service | `ICurrentUserService` is the session service (`CurrentUser`, **`CurrentRole`**, **`IsInRole`**, `SignIn`, `SignOut`): decision 59, no second class | done |
| Hide by role | The existing `CanManageProducts`, `CanAddProduct`, `CanManageCharacteristics`, `CanEdit`, `CanCreateProduct`, `AvailableDirections`, `ShowDiagnostics` bindings (all from `RolePermissions`) | done (already there, kept) |
| Role check in the command | `IPermissionGuard` / `PermissionGuard`: Add / Edit / Delete / Open characteristics in `ProductListViewModel`, `ProductFormViewModel.Save`, `CharacteristicsViewModel.Add`, `ProductDetailViewModel.Save`, `OperationViewModel.Save` (Out as a Driver), `ScannerViewModel.CreateProduct` / `CreateFromLookup`, `LogsViewModel.Refresh`. A refusal shows an alert ("Not allowed" + reason), writes a `Warning`, fills the page status line and calls nothing. The services refuse too | done |
| Biometrics | `IBiometricAuthenticator` / `MauiBiometricAuthenticator` (`Plugin.Maui.Biometric` 0.1.2: BiometricPrompt on Android, Windows Hello on Windows), `IBiometricSettings` (switch in Settings, default off). The button is shown only with a valid token, the switch on and hardware available. Success calls `ISessionStore.LoadUserAsync`, which validates the token again; failure or cancel stays on the password form with a message and a `Warning`. With the switch on a restart waits on the login page (state and drafts are kept) | done in code; **success on a real device is an owner action** |
| Certificate pinning | `ICertificatePinValidator` / `CertificatePinValidator` (SHA-256 of the SubjectPublicKeyInfo against `Sync:Pins`), `PinnedHandlerFactory` (accepts only when the normal chain check passed **and** the key is pinned; no pins = refuse), `HttpSyncApi : ISyncApi` (POST `sync/changes`). `StubSyncApi` stays the default; `HttpSyncApi` is used only when `appsettings.local.json` has an https `Sync:BaseUrl`. Open Food Facts keeps the normal handler | done in code and unit tests; **a real server is optional and an owner action** |

## 4. Decisions

Also in the README decision log (55-64).

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| Own PBKDF2 hasher over `Rfc2898DeriveBytes.Pbkdf2` | BCrypt.Net / Argon2 package | No dependency; PBKDF2-HMAC-SHA256 with 210 000 iterations is the OWASP-listed baseline for FIPS-friendly hashing; the format carries the iteration count so it can be raised |
| `Login` / `PasswordHash` nullable in the table, non-null in the model | Recreate `Users` to add `NOT NULL` | SQLite cannot add a `NOT NULL` column without a default; recreating a table that every other table references is risky for no gain. The repository stores "no login" as `NULL`, so users without a login never collide in the unique index |
| `[JsonIgnore]` on `UserRow.PasswordHash` | Strip it in `SyncJournal`; send it | The sync payload is `JsonSerializer.Serialize(row)`; one attribute keeps the hash on the device with no extra code. Test: `AddingAUser_KeepsLoginAndHash_AndTheSyncPayloadHasNoHash` |
| The session is a token the app signs itself | Keep the user id; store the password; fake a server token | A saved id is a claim anybody can edit; a stored password is the thing the hash exists to avoid; an HMAC token with an expiry is checked, not trusted. It protects against a changed value, not against someone who can read the secure storage (then they hold the key too) |
| `SessionStore` (new) next to `ITokenService` | Put storage inside `HmacTokenService` or `AuthService` | Single responsibility: signing, checking credentials and keeping the token are three reasons to change. `AppLifecycleService` and `LoginViewModel` depend on the small `ISessionStore` |
| Biometric unlock on = no silent restore | Always restore silently and offer biometrics nowhere; require biometrics every time | The lab says biometrics are a convenient re-entry for a user with a stored token. With the switch off nothing changes for the user; with it on the app is locked after a restart until the biometric check or the password |
| `Plugin.Maui.Biometric` | `Plugin.Fingerprint`, own platform code | It lists `net10.0` for Android and Windows (Windows Hello), builds in both targets (checked), MIT. Cost: `minSdk` 23 (was 21) and "cancelled" is not told apart from "did not match" |
| `IPermissionGuard` (alert + `Warning` + message, then stop) | Repeat the check in each command; throw an exception | Eight commands need the same three lines; a guard keeps them identical and testable once. It returns the message so each page also shows it in its own status label |
| Pinning compares the SPKI hash and **also** requires `SslPolicyErrors.None` | Compare the certificate thumbprint like the lab sample; accept a pin match alone | Thumbprints change at every renewal; the lab's sample ignores `errors`, which would accept an expired or wrong-host certificate with the right key. Pinning only narrows what TLS already accepts. Consequence: a self-signed development server is refused; it needs a certificate the device trusts |
| `LocalSettings` shared by Sentry and Sync | A second reader class | DRY (decision 64) |

## 5. Changes in the PR

- `Data/Migrations/0003_auth.sql`; `Models/User` (`Login`, `PasswordHash`); `UserRow`, `SqliteUserRepository` (`GetByLoginAsync`), `IUserRepository`.
- `Services/`: `IPasswordHasher`, `Pbkdf2PasswordHasher`, `IAuthService`, `AuthService`, `ITokenService`, `HmacTokenService`, `ISessionStore`,
  `SessionStore`, `ICurrentUserService` (`CurrentRole`, `IsInRole`), `ISecretStore.RemoveAsync`, `IPermissionGuard`, `PermissionGuard`,
  `IBiometricAuthenticator`, `MauiBiometricAuthenticator`, `BiometricOutcome`, `IBiometricSettings`, `BiometricSettings`,
  `ICertificatePinValidator`, `CertificatePinValidator`, `PinnedHandlerFactory`, `HttpSyncApi`, `SyncSettings`, `LocalSettings`;
  `SentryDsn` now delegates to `LocalSettings`; `AppLifecycleService` restores from the token; `AppStateService` no longer keeps a user id
  (dead code removed; the old `state.user` key is deleted at the next save).
- `ViewModels/`: `LoginViewModel` (rewritten), `SettingsViewModel` (Biometric unlock), the guard in `ProductListViewModel` (also clears the token on
  sign-out), `ProductFormViewModel`, `CharacteristicsViewModel`, `ProductDetailViewModel`, `OperationViewModel`, `ScannerViewModel`, `LogsViewModel`.
- `Views/LoginPage.xaml` (rewritten), `Views/SettingsPage.xaml` (Security section); `MauiProgram` (biometrics, sync server, shared settings file).
- `Mobile_apps_L1_Model_view.csproj`: `Plugin.Maui.Biometric` 0.1.2, Android `SupportedOSPlatformVersion` 23; `AndroidManifest.xml`: `USE_BIOMETRIC`.
- Tests (950 in total, 775 before): `Pbkdf2PasswordHasherTests` (known vector, format, wrong password, malformed hashes, rehash), `AuthServiceTests`
  (lockout with a fake clock, same message, no secrets in logs), `HmacTokenServiceTests` (tamper, other key, expiry, malformed), `SessionStoreTests`,
  `AppLifecycleServiceTests` (token restore, tampered / expired / unknown user / unavailable storage, biometric lock), `LoginViewModelTests`,
  `BiometricSettingsTests`, `PermissionGuardTests`, `RoleRefusalTests` (every restricted command as the wrong role: alert, warning, repository untouched),
  `RolePermissionsTests.RoleMatrix` (`[Theory]` over role x action), `CertificatePinningTests` (certificates generated in the test: match, other
  certificate, no pins, chain / host errors), `HttpSyncApiTests`, `SyncSettingsTests`, repository tests for logins and the sync payload,
  `EndToEnd/AppLifecycleFlowTests` (login -> token stored -> restart -> restored -> sign-out clears; biometric lock and unlock). Every earlier end-to-end test
  now signs in through `LoginViewModel` with the demo passwords (`DemoAccounts`).
- Verified by running the Windows exe (driven through UI Automation): a wrong password shows "Wrong login or password.", `olena` /
  `Manager#2026` reaches the product list as Olena Kovalenko (Manager), the next start restored the session from the token, **Sign out** removed it.

## 6. Open questions / next steps

- **Client-side authorization is not a security boundary** (question 12). Roles are enforced in the app only. Anyone who can modify the
  app, or read its database file (Settings can even share it), can ignore every check. The hashed passwords and the token key protect against
  casual misuse of a lost phone, not against a determined attacker with the device. A real deployment needs a server that authenticates,
  issues the token with the role claim and re-checks every request (decision 59 and question 9).
- `NeedsRehash` exists but nothing re-hashes at login yet; when the iteration count is raised, `AuthService` should update the hash after a successful login.
- No password change or recovery, no user administration, no registration (scope).
- Locking the app after a restart (biometric switch on) keeps the drafts of the previous user; a password sign-in as someone else would resume them. Acceptable for three users on one device; revisit with several devices.
- The lockout counters live in memory: restarting the app resets them (stated limit of a local-only design).
- Pinning of the sync client was unit-tested with generated certificates; it has not been run against a real server (owner action).

## 7. Answers to the control questions

**7. Why must an access token not be stored in ordinary `Preferences`, and how does `SecureStorage` differ?**
`Preferences` is a plain key/value file (the XML / shared-preferences store of the app), readable by anyone with a backup, a rooted device
or a file explorer, and it travels in backups. A token is a credential: whoever has it is the user. `SecureStorage` keeps values
encrypted with a key held by the OS (Android Keystore, iOS Keychain, Windows data protection), bound to the app and the device, so a
copied file is useless. In StockRoom the session token and the signing key live only behind `ISecretStore` (`MauiSecretStore` ->
`SecureStorage`); `Preferences` (`IPreferencesStore`) holds the theme, the last route and the biometric switch, none of them secret
(AGENTS 2a). Even there, `SecureStorage` can fail (a restored backup): `AppLifecycleService` catches it, logs a `Warning` and shows the login page.

**8. Why is hiding a UI element not enough to restrict access by role?**
A hidden or disabled control only changes what is drawn. The command behind it can still be invoked: a stale screen after the role
changed, a deep link, an automation tool, a test, another ViewModel calling it, a binding changed in a modified app. Access control has to be
where the action happens. StockRoom has two mandatory layers (AGENTS 2a): the UI binding (`CanManageProducts`, `CanEdit`, ...) and the
command, which asks `IPermissionGuard` and then calls a service that checks `RolePermissions` again. `RoleRefusalTests` invokes each restricted
command as the wrong role (`ExecuteAsync` ignores `CanExecute`) and asserts the alert, the `Warning` and an untouched repository;
`ServiceLayer_RefusesToo_WhenTheViewModelIsBypassed` does the same without any ViewModel.

**9. How is the user's role normally passed from the server to the client?**
The server decides the role at login and puts it in the response: usually as a claim inside the signed access token (a JWT with a
`role` claim, which the client can read but not change without invalidating the signature) or as a field of the profile returned with
the token. The client stores it in its session service and uses it for the UI; the server re-reads the role from the token on every request.
StockRoom has no server accounts: the role is a column of `Users` read from the local database after the password check, and the session
token (`userId.expiry`, HMAC) deliberately carries only the user id, so the role is always re-read from the database at restore and a role
change takes effect at the next start.

**10. What limits biometric authentication as the only protection?**
It proves that the device owner (or someone who enrolled a finger or face on the device) is present, not who the user is in the
application, and it cannot be the root of trust by itself: the stored secret it unlocks must exist first, which needs a full login once.
It can be bypassed by a false accept, a coerced or sleeping user, a sibling's enrolled finger or a compromised OS. The device may have no sensor, the user may change
the biometric set, the sensor can lock out and then the password is needed, so a fallback is mandatory. Biometric data are not
replaceable like a password. StockRoom therefore treats biometrics only as a way to open a stored token: a valid, unexpired signed token and the
biometric switch are required, the token is validated again after the check, and the password form is always available.

**11. What is a man-in-the-middle attack and how does certificate pinning help?**
An attacker who sits between the app and the server (a rogue Wi-Fi, a proxy, a compromised router) presents his own certificate, reads and changes the traffic.
Normal TLS stops this unless the attacker holds a certificate that the device trusts for that host, which happens with a compromised or
coerced certificate authority, or a CA the user or an employer installed on the device. Pinning adds a second condition: the server's public key must be
one the app was shipped with, so a certificate for the same host from another key is refused even if the chain is valid. StockRoom's
`PinnedHandlerFactory` accepts a connection only if the normal chain and host check passed **and** `CertificatePinValidator` finds the SHA-256 of the key among
`Sync:Pins`; with no pins it refuses. The cost is operational: when the key changes the app needs an update (so a backup pin should be configured), and it
is used only for our own server, never for Open Food Facts.

**12. What risks appear if the role is checked only on the client, without confirmation on the server?**
The client is under the control of its user. They can patch the app, hook the method that returns the role, edit the local database or
the stored session, or call the server's API directly with a tool, skipping the app, so every restriction disappears and the server
accepts the request. Data can be deleted, stock changed and other users' records read. Hiding and client checks are about honest users and good
messages, not security. In StockRoom the checks are a real boundary only against mistakes, and, since the whole database is on the device (and can be shared
from Settings), they are not a boundary against a person with the device. The `ISyncApi` contract is where a server would re-authorize each change by
the token's role and reject, for example, an Out operation from a Driver; the stub server does not.

## Owner actions

These cannot be done by the agent and are **not** claimed as verified:

- Sign in with each demo account (see README, "Demo accounts") on **Windows** and on an **Android device**; a screenshot of the login page, of the product list for each of the three roles
  (what is visible differs: Manager has Add / Edit / Delete / Characteristics / Diagnostics, Driver can record In only). A forbidden action cannot be triggered from the
  normal UI because the control is hidden; the refusal alert ("Your role (Driver) cannot record Out operations.") is shown in the tests (`RoleRefusalTests`). To see it on screen,
  a debug build with a forced command call is needed; if the report needs the screenshot, say so and a small debug switch can be added.
- **Biometrics on a real device:** Settings -> Biometric unlock on, close the app, start it again, press **Unlock with biometrics**, screenshot of the system prompt and the product list.
  Windows Hello needs a configured PIN / face / fingerprint; without hardware the switch and button are hidden by design.
- Optional: set up a https server with a certificate chain the device trusts and put `Sync:BaseUrl` and `Sync:Pins` into `appsettings.local.json` to see the real client
  and a refused connection when a different key is presented; the pin can be computed with the command in the README.
- Review the PR; the `lab6` -> `lab5` pull request is opened by the owner (no `gh` on this machine).
