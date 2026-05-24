# Play Console UI Operator

This is the fallback path for Google Play releases when the Google Play API service account is not yet allowed inside the target Play Console.

It uses a real browser session and the Google Play Console web UI. It does not need Google passwords in the repository. The operator signs in once in the opened browser profile, then the script can upload AAB files to a release track.

## Install once

```bash
cd play-console-operator
npm install
npx playwright install chromium
```

## Upload a built AAB

```bash
cd play-console-operator
npm run upload -- \
  --developer-id 7898824668858143466 \
  --app-id 4972415713524037688 \
  --track production \
  --aab ../builds/com.playmaxsolutions.slotspot/v22/AAB_com.playmaxsolutions.slotspot_v22.aab \
  --version-name 1.0.22 \
  --notes "Bug fixes and SDK updates."
```

By default the script uploads and saves the release draft, then stops before final review submission.

To also continue through review screens, add:

```bash
--submit
```

Use `--dry-run` to only open the target release page and print the resolved values.

## Login once

```bash
cd play-console-operator
npm run upload -- --login-only
```

The browser opens with the persistent profile. Sign in to the needed Google Play account once, then stop the command.

## Upload from a CI build folder

If the build folder contains `build.txt`, the operator can read most values automatically:

```bash
cd play-console-operator
npm run upload -- \
  --from-build-dir ../builds/com.playmaxsolutions.slotspot/v22 \
  --developer-id 7898824668858143466 \
  --app-id 4972415713524037688 \
  --dry-run
```

`developer-id` and `app-id` are still explicit because they are Play Console UI identifiers, not Android package identifiers.

## Required fields

- `--developer-id`: Play Console developer account id.
- `--app-id`: Play Console app id, visible in the app dashboard URL.
- `--track`: usually `production`, `internal`, `alpha`, or `beta`.
- `--aab`: local AAB path.
- `--version-name`: release name to show in Play Console.
- `--notes`: release notes text.
- `--from-build-dir`: optional path to a CI build folder containing `build.txt`.
- `--profile-dir`: optional browser profile path. Defaults to `.play-console-profile`.
- `--login-only`: open Play Console only for login/session setup.

The persistent browser profile is stored in `.play-console-profile/` so the operator login survives between runs.
