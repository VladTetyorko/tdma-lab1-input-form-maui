# Configuration (optional, untracked)

Secrets and keys never go into git. They live in the untracked `Mobile_apps_L1_Model_view/appsettings.local.json` or in the Android manifest on the owner's machine.

## Crash reporting (Sentry)

Crash reporting uses Sentry when a DSN is configured; without one a null reporter is used and nothing is sent.
Create the untracked file `Mobile_apps_L1_Model_view/appsettings.local.json` (it is git-ignored) and rebuild:

```json
{ "Sentry": { "Dsn": "https://<key>@<org>.ingest.sentry.io/<project>" } }
```

or set the environment variable `STOCKROOM_SENTRY_DSN` (desktop only). Details: [`docs/steps/05-logging.md`](steps/05-logging.md).

## Sync server

Without configuration the app syncs with a local stub. To use a real server over https with
certificate pinning add this to the untracked `Mobile_apps_L1_Model_view/appsettings.local.json` (next to the Sentry DSN) and rebuild:

```json
{ "Sync": { "BaseUrl": "https://sync.example.com/", "Pins": ["<base64 SHA-256 of the server public key>"] } }
```

The pin is `openssl s_client -connect host:443 | openssl x509 -pubkey -noout | openssl pkey -pubin -outform der | openssl dgst -sha256 -binary | base64`.
Open Food Facts lookups never use pinning.

## Google Maps key (Android only)

Create an Android-restricted key for the *Maps SDK for Android*
(package `com.companyname.mobile_apps_l1_model_view` and the SHA-1 of the signing key) and replace `YOUR_GOOGLE_MAPS_API_KEY`
in `Mobile_apps_L1_Model_view/Platforms/Android/AndroidManifest.xml`. Do not commit the real key to a public repository.
Without a key the map area is blank; the pin list and *Open in maps* still work. Photos are stored under
`photos/` in the app data folder (only the relative path is in the database).
