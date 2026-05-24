# Google Play Connect Portal

Small web portal for non-technical operators:

1. Open one link.
2. Click **Connect Google Play**.
3. Sign in with the Google account that owns or administers Play Console.
4. Confirm permissions.
5. The portal creates or reuses a Google Cloud service account, creates a JSON key, and saves it into the GitHub repository secret `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON`.

After this, the main `Build Android APK` workflow can publish AAB files automatically when `publish_to_google_play=true`.

## Required setup

Copy `.env.example` and fill:

- `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`: OAuth Web Client.
- `GOOGLE_CLOUD_PROJECT_ID`: project that will own the service account.
- `GITHUB_TOKEN`: GitHub token with repository secret write access.
- `GITHUB_OWNER` / `GITHUB_REPO`: normally `zey-win` / `ci-cd`.

The Google account used in the browser must have permission to enable APIs and manage service accounts in the chosen Cloud project. It must also have Play Console API access permissions. If Google Play refuses publication after the secret is saved, open Play Console API access and grant the shown service account release permissions for the app.

## Run locally

```bash
cd google-play-connect
cp .env.example .env
npm install
npm run dev
```

Then open `http://localhost:8080`.

## Deploy

Deploy this folder as a Node.js service with:

- build command: `npm install`
- start command: `npm start`
- Node.js 20+

Set the deployed URL as `BASE_URL`, and set the Google OAuth redirect URI to:

```text
https://YOUR-DOMAIN/oauth2callback
```

For Render, this repository includes a root `render.yaml` Blueprint. Create the Blueprint from `zey-win/ci-cd`, then fill the `sync: false` values in Render:

- `GOOGLE_CLOUD_PROJECT_ID`
- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`
- `GITHUB_TOKEN`
- optional `ALLOWED_GOOGLE_EMAILS`

The public operator entry page is intended to be:

```text
https://zeywin.github.io/connect
```

That page should point its button to the deployed backend portal, for example:

```text
https://zeywin-connect.onrender.com/auth/google
```
