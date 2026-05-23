# Google Play release report: Plinko 1.0.1 (2)

Date: 2026-05-23

## App

- App name: Plinko
- Package: `com.socialapps.plinko`
- Google Play developer account: `5922122594113010907`
- Google Play app id: `4975569208087582209`
- Previous production release: `1 (1.0.0)`
- Submitted release: `2 (1.0.1)`
- Rollout: full production rollout
- Google Play status after submission: changes in review

## Build

- CI repository: `zey-win/ci-cd`
- GitHub Actions run: https://github.com/zey-win/ci-cd/actions/runs/26340011231
- Run number: `80`
- Build artifact: `AAB_com.socialapps.plinko_v2.aab`
- Local verification: `jarsigner` reported `jar verified`
- Signing certificate subject: `O=Play Max Solutions`

## Evidence

- [Google Play publishing overview](screenshots/play-publishing-overview.png)
- [Google Play production releases](screenshots/play-production-releases.png)
- [GitHub Actions run](screenshots/github-actions-run.png)
- [Phone reference screenshot](device/phone-dev-options-reference.png)

## Phone launch video

Marketing requested a 15-second phone launch video. The file should be saved here when an Android device is visible to ADB:

- `video/phone-launch-com.socialapps.plinko-v2.mp4`

Current capture blocker: `adb devices` returned no attached device at report creation time.
