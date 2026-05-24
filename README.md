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
   - `ZEYWIN_API_KEY` for non-interactive releases, or enter `zeywin_api_key` manually when dispatching the workflow
2. In GitHub Actions, run **Build Android Play Release**.
3. The workflow defaults are set for Play upload:
   - `build_format`: `aab`
   - `publish_to_google_play`: `true`
   - `google_play_track`: `production` for production updates, or `internal` for test uploads
   - `google_play_status`: `completed` for immediate rollout, or `draft` to leave a draft in Play Console
   - `require_google_play_upload`: `true`

When `require_google_play_upload=true`, the workflow fails if Google Play upload permission is missing or the upload cannot be completed. The built AAB is still saved to the GitHub run artifacts and repository metadata, but a failed run means the app update was not uploaded to Play.

Before the first real release for a package, run **Check Google Play Access** with the package name. If it fails, grant the service account from `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` access in Play Console **Users and permissions**, with release permissions for that app.

## Automatic call from another GitHub workflow

Game repositories can trigger the central release workflow after their own checks or build preparation:

```yaml
name: Release to Google Play

on:
  workflow_dispatch:
  push:
    branches:
      - main

jobs:
  release:
    uses: zey-win/ci-cd/.github/workflows/build-apk.yml@main
    with:
      game_repository: zey-win/plinko
      game_ref: ${{ github.sha }}
      package_name: com.playsocialgames.plinko
      app_name: Plinko
      build_format: aab
      publish_to_google_play: 'true'
      google_play_track: production
      google_play_status: completed
      require_google_play_upload: 'true'
    secrets: inherit
```

This path builds the Unity project in GitHub Actions, signs the AAB, uploads it to Google Play, and fails the run if Play upload is not completed.

## Dispatch after an external build

If the game repository already has its own build workflow, add this step after the successful build job to trigger Google Play upload through `zey-win/ci-cd`:

```yaml
- name: Upload update to Google Play
  env:
    GH_TOKEN: ${{ secrets.CI_CD_DISPATCH_TOKEN }}
  run: |
    gh api repos/zey-win/ci-cd/dispatches \
      --method POST \
      --field event_type=google-play-release \
      --raw-field client_payload='{
        "game_repository": "zey-win/plinko",
        "game_ref": "${{ github.sha }}",
        "package_name": "com.playsocialgames.plinko",
        "app_name": "Plinko",
        "google_play_track": "production",
        "google_play_status": "completed"
      }'
```

`CI_CD_DISPATCH_TOKEN` must be a GitHub token that can dispatch workflows in `zey-win/ci-cd`. The central workflow still builds and signs the final AAB, then uploads it to Google Play.
