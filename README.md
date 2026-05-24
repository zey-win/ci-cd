# ZeyWin CI/CD

ZeyWin releaser, Ubuntu Actions builder.

This public repository contains only GitHub Actions release automation. Game and SDK sources are fetched at build time from private source repositories.

Latest APK versions and next manual version values are listed in [builds/index.md](builds/index.md).

Google Play API publishing can be connected through the operator portal in [google-play-connect](google-play-connect/). The public entry link is `https://zey-win.github.io/connect/`.

## Google Play updates from GitHub

Use `.github/workflows/build-apk.yml` for Play updates:

1. Add repository secrets:
   - `PRIVATE_REPO_TOKEN`
   - `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`
   - `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`
   - `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON`
2. In GitHub Actions, run **Build Android Play Release**.
3. The workflow defaults are set for Play upload:
   - `build_format`: `aab`
   - `publish_to_google_play`: `true`
   - `google_play_track`: `production` for production updates, or `internal` for test uploads
   - `google_play_status`: `completed` for immediate rollout, or `draft` to leave a draft in Play Console
   - `require_google_play_upload`: `true`

When `require_google_play_upload=true`, the workflow fails if Google Play API access is missing or the upload cannot be completed. The built AAB is still saved to the GitHub run artifacts and repository metadata, but a failed run means the app update was not uploaded to Play.

Before the first real release for a package, run **Check Google Play Access** with the package name. If it fails, grant the service account from `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` access in Play Console API access settings, with release permissions for that app.
