# Build and run

Requires the .NET 10 SDK with the MAUI workloads (`dotnet workload install maui`). When the SDK lives in `%USERPROFILE%\.dotnet`,
global tools such as ReportGenerator need `DOTNET_ROOT` set to that folder (PowerShell: `$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"`),
otherwise they report ".NET location: Not found".

```bash
# Windows (unpackaged exe)
dotnet build Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-windows10.0.19041.0
dotnet run --project Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-windows10.0.19041.0

# Android: emulator or a phone with USB debugging, Android SDK installed
dotnet build Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-android -p:AndroidSdkDirectory="$LOCALAPPDATA/Android/Sdk"
dotnet build Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-android -t:Run     # install and start on the device

# A package you can send to a phone (debug-signed APK, see docs/RELEASE.md)
dotnet publish Mobile_apps_L1_Model_view/Mobile_apps_L1_Model_view.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

**Tests.**

```bash
dotnet test Mobile_apps_L1_Model_view.Tests                       # 1309 tests of Core, any OS (the UI tests are a separate project)
dotnet test Mobile_apps_L1_Model_view.Tests --filter "Category!=UI" --collect:"XPlat Code Coverage" --results-directory ./TestResults   # what CI runs
python .github/scripts/check_coverage.py TestResults/<guid>/coverage.cobertura.xml Mobile_apps_L1_Model_view.ViewModels. 60            # the 60 % ViewModel gate
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:CoverageReport -reporttypes:Html
dotnet test Mobile_apps_L1_Model_view.AppTests                    # 42 tests of MAUI-bound classes, Windows only, not in CI
dotnet test Mobile_apps_L1_Model_view.UiTests                     # 2 Appium tests: need Appium and a device, never run by CI (step 08)
dotnet build Mobile_apps_L1_Model_view.slnx                       # every project
```

**CI.** [`ci.yml`](../.github/workflows/ci.yml) runs on every push and pull request: `test` (ubuntu, coverage report, ViewModel gate
at 60 %), `build-android` (Release build) and `apk` (debug-signed package uploaded as the artifact `stockroom-apk`).

**Database.** The first start creates and seeds `stockroom.db3` in the app data
folder (Windows, unpackaged: `%LOCALAPPDATA%/User Name/com.companyname.mobile_apps_l1_model_view/Data`).
**To reset it:** close the app and delete that file (on Android: clear the storage of the app). The Settings page shows the
sync queue, has the **Simulate offline** switch and **Share database**.
Tests never touch it: each test container uses its own temporary file.

### APK for a phone

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-apk.ps1                    # artifacts/apk/StockRoom-<version>-<commit>.apk
powershell -ExecutionPolicy Bypass -File scripts\build-apk.ps1 -Install -Launch   # phone with USB debugging on
```

The package is a debug-signed Release build (git-ignored). Options and the manual way: [`docs/RELEASE.md`](RELEASE.md).
