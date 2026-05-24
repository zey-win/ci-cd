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

## Required fields

- `--developer-id`: Play Console developer account id.
- `--app-id`: Play Console app id, visible in the app dashboard URL.
- `--track`: usually `production`, `internal`, `alpha`, or `beta`.
- `--aab`: local AAB path.
- `--version-name`: release name to show in Play Console.
- `--notes`: release notes text.

The persistent browser profile is stored in `.play-console-profile/` so the operator login survives between runs.
