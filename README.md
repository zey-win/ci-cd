# Plinko SDK APK Build

Unity project for building Plinko APKs with ZeyWin Ads SDK and CrashGuard SDK through GitHub Actions.

The workflow is reusable for new apps: run `Build Android APK` manually and enter a new Android package name, app name, version, and the matching ZeyWin API key. AdMob IDs stay the same.

## GitHub Actions secrets

Set these repository secrets before running `Build Android APK`:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`
- `UNITY_SERIAL` only for Unity Pro/Plus
- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASS`
- `ANDROID_KEYALIAS_NAME`
- `ANDROID_KEYALIAS_PASS`

Default values:

- version: `1.0.6`
- version code: `6`
- alias: `social casino`
- keystore password: `12345654321`
- AdMob App ID: `ca-app-pub-6988952582458184~2578339758`
- AdMob banner: `ca-app-pub-6988952582458184/7966397807`
- AdMob interstitial: `ca-app-pub-6988952582458184/6653316139`
- AdMob rewarded: `ca-app-pub-6988952582458184/3302497559`

## SDK pins

- `com.zeywin.ads`: `https://github.com/zey-win/ZeyWinAdsSDK-Unity.git#c3280e6d7dea932a25f7b2406d209b739c22c67d`
- `com.crashguard.sdk`: `https://github.com/zey-win/CrashGuardSDK-Unity.git#2b3947155206bc445e2d6088ac51cdf2760f921d`
