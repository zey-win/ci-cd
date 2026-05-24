# Admin Bootstrap

Marketing users must not configure Google Cloud, OAuth, GitHub secrets, or JSON keys.

The intended split is:

- Operator page: `https://zey-win.github.io/connect/`
- Operator actions: open page, sign in with Google, confirm.
- Technical bootstrap: done once by an owner/admin before operators use the page.

## Bootstrap values

The backend needs these values before operators can use the button:

- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`
- `GOOGLE_CLOUD_PROJECT_ID`
- `GITHUB_TOKEN`

These are backend environment variables, not marketer inputs.

## Automation target

Run the bootstrap script from repository root:

```bash
export GITHUB_TOKEN="$(gh auth token)"
export GOOGLE_CLOUD_PROJECT_ID="your-google-cloud-project"
export GOOGLE_CLIENT_ID="your-oauth-web-client-id"
export GOOGLE_CLIENT_SECRET="your-oauth-web-client-secret"
node google-play-connect/scripts/bootstrap-admin.mjs
```

If `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` are not provided, the script stops with the exact redirect URI that must be used for the OAuth Web Client:

```text
https://zeywin-connect.onrender.com/oauth2callback
```

The bootstrap script:

1. Create or select a Google Cloud project.
2. Enable `androidpublisher.googleapis.com`, `iam.googleapis.com`, and OAuth consent requirements.
3. Validate OAuth Web Client values with redirect URI:
   `https://zeywin-connect.onrender.com/oauth2callback`
4. Write `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, and `GOOGLE_CLOUD_PROJECT_ID` into Render.
5. Write `GITHUB_TOKEN` into Render from the technical GitHub owner token.

After bootstrap, marketers use only the Connect button.

## GitHub Action bootstrap

Technical owners can also run **Bootstrap Google Play Connect** from GitHub Actions.

Required repository secrets:

- `RENDER_API_KEY`
- `CONNECT_GITHUB_TOKEN`

Manual workflow inputs:

- `google_cloud_project_id`
- `google_client_id`
- `google_client_secret`
- `render_service_id`

After the workflow succeeds, operators use only:

```text
https://zey-win.github.io/connect/
```
