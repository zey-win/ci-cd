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

The next bootstrap script should:

1. Create or select a Google Cloud project.
2. Enable `androidpublisher.googleapis.com`, `iam.googleapis.com`, and OAuth consent requirements.
3. Create an OAuth Web Client with redirect URI:
   `https://zeywin-connect.onrender.com/oauth2callback`
4. Write `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, and `GOOGLE_CLOUD_PROJECT_ID` into Render.
5. Write `GITHUB_TOKEN` into Render from the technical GitHub owner token.

After bootstrap, marketers use only the Connect button.
