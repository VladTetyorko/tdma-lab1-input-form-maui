# Releasing StockRoom

How to cut `v1.0` (and later versions). The build produces a **debug-signed Android package** for sideloading. It is
not a Google Play build: Play needs a release keystore, which is deliberately **not** in this repository.

## What CI does

`.github/workflows/ci.yml` runs on every push and pull request:

1. `test` - the Core tests with coverage and the 60 % ViewModel gate (ubuntu).
2. `build-android` - a Release build of the app (windows, after `test`).
3. `apk` - `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk`, then uploads the `.apk` as the
   artifact **`stockroom-apk`** (windows, after `build-android`). Nothing is signed with a secret: the Android SDK signs
   with its per-machine debug key.

The Android Release settings that make this work (no trimming, no AOT, no marshal methods) are in
`Mobile_apps_L1_Model_view.csproj`; why is in decision 84 of the [decision log](decision-log.md).

## Cutting v1.0 (owner)

1. Open the pull request `lab9` -> `lab8` (retarget to `master` once `lab8` is merged) and wait for a **green** run of all
   three jobs. Screenshot it for the report.
2. Merge. Make sure the merge commit on `master` is green too.
3. Tag the merge commit and push the tag (the agent that prepared this never creates a tag):

   ```bash
   git checkout master && git pull
   git tag -a v1.0 -m "StockRoom 1.0: final module (Labs 1-9)"
   git push origin v1.0
   ```

4. Download the artifact `stockroom-apk` from the run of the tagged commit (Actions -> the run -> Artifacts) and, if you
   want it on the GitHub **Releases** page, create a release from the tag and attach the `.apk`.
5. The version shown on the device comes from `ApplicationDisplayVersion` (`1.0`) and `ApplicationVersion` (`1`) in the
   csproj. For a later release raise both before tagging.

## Installing the package

```bash
adb devices
adb install -r com.companyname.mobile_apps_l1_model_view-Signed.apk
```

Or copy the file to the phone and open it (allow "install unknown apps"). Minimum Android is 6.0 (API 23). Camera,
location and the accelerometer are optional (`required="false"`), so the app also installs on a device without them.

## Building the package locally

Use the script (Windows PowerShell 5.1 or PowerShell 7, from any folder):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-apk.ps1
```

It finds the Android SDK (`-SdkPath`, `ANDROID_HOME`, `ANDROID_SDK_ROOT`, `%LOCALAPPDATA%\Android\Sdk`), publishes,
copies the signed package to `artifacts/apk/` (`-OutDir` changes it; `-dirty` in the name means uncommitted changes)
and prints its size and SHA-256. `-Install` and `-Launch` use `adb` (`-Serial` when several devices are connected). An
installed package signed with another key (the CI artifact, another computer) must be uninstalled first, which deletes
its data, so the script never does that itself.

The same by hand:

```bash
dotnet publish Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-android -c Release \
  -p:AndroidPackageFormat=apk -p:AndroidSdkDirectory="$LOCALAPPDATA/Android/Sdk"
```

The installable file is `Mobile_apps_L1_Model_view/bin/Release/net10.0-android/publish/com.companyname.mobile_apps_l1_model_view-Signed.apk` (about 72 MB: arm64-v8a and x86_64 libraries, not trimmed, no AOT; the unsigned twin next to it is not installable). Restoring the packages needs network
access; the build needs the `maui-android` workload, JDK 17 and the Android SDK.

## Before tagging: checklist

- [ ] CI is green on the commit to be tagged.
- [ ] The smoke checklist in `docs/steps/09-final.md` (section 13) was run on a device or emulator with the downloaded APK.
- [ ] No `appsettings.local.json`, keystore, Sentry DSN or Maps key is committed (`git status`, `git log -p` for the words `Dsn` and `API_KEY`).
- [ ] [Step log](step-log.md) and [decision log](decision-log.md) are up to date.

## Not done on purpose

- A release-signed package, `.aab` for Play, store listing: out of scope for the course.
- Automatic tagging or a GitHub Release from CI: the tag is a decision of the owner.
