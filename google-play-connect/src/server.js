import crypto from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import cookieParser from 'cookie-parser';
import express from 'express';
import { google } from 'googleapis';
import { Octokit } from '@octokit/rest';
import sodium from 'libsodium-wrappers';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const app = express();

const requiredEnv = [
  'BASE_URL',
  'GOOGLE_CLIENT_ID',
  'GOOGLE_CLIENT_SECRET',
  'GOOGLE_CLOUD_PROJECT_ID',
  'GITHUB_OWNER',
  'GITHUB_REPO',
  'GITHUB_TOKEN'
];

const config = {
  port: Number(process.env.PORT || 8080),
  baseUrl: process.env.BASE_URL || `http://localhost:${process.env.PORT || 8080}`,
  googleClientId: process.env.GOOGLE_CLIENT_ID,
  googleClientSecret: process.env.GOOGLE_CLIENT_SECRET,
  googleCloudProjectId: process.env.GOOGLE_CLOUD_PROJECT_ID,
  serviceAccountId: process.env.SERVICE_ACCOUNT_ID || 'zeywin-play-publisher',
  serviceAccountDisplayName: process.env.SERVICE_ACCOUNT_DISPLAY_NAME || 'ZeyWin Google Play Publisher',
  githubOwner: process.env.GITHUB_OWNER || 'zey-win',
  githubRepo: process.env.GITHUB_REPO || 'ci-cd',
  githubToken: process.env.GITHUB_TOKEN,
  githubSecretName: process.env.GITHUB_SECRET_NAME || 'GOOGLE_PLAY_SERVICE_ACCOUNT_JSON',
  allowedGoogleEmails: (process.env.ALLOWED_GOOGLE_EMAILS || '')
    .split(',')
    .map((email) => email.trim().toLowerCase())
    .filter(Boolean)
};

app.disable('x-powered-by');
app.use(cookieParser());
app.use(express.static(path.join(__dirname, '..', 'public')));

function missingConfig() {
  return requiredEnv.filter((key) => !process.env[key]);
}

function oauthClient() {
  return new google.auth.OAuth2(
    config.googleClientId,
    config.googleClientSecret,
    `${config.baseUrl}/oauth2callback`
  );
}

function randomState() {
  return crypto.randomBytes(24).toString('hex');
}

function renderPage(title, body, status = 200) {
  return `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${escapeHtml(title)}</title>
  <link rel="stylesheet" href="/styles.css" />
</head>
<body>
  <main class="shell">
    ${body}
  </main>
</body>
</html>`;
}

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;');
}

app.get('/', (_req, res) => {
  const missing = missingConfig();
  const warning = missing.length
    ? `<div class="notice danger">Server is not configured yet. Missing: <code>${missing.map(escapeHtml).join('</code>, <code>')}</code></div>`
    : '';

  res.send(renderPage('Connect Google Play', `
    <section class="hero">
      <p class="eyebrow">ZeyWin CI/CD</p>
      <h1>Connect Google Play publishing</h1>
      <p class="lead">Sign in with the Google account that owns or administers the Play Console account. The portal prepares the service account and saves it into GitHub Actions secrets.</p>
      ${warning}
      <a class="button" href="/auth/google">Connect Google Play</a>
    </section>
    <section class="steps">
      <div><strong>1</strong><span>Google sign-in</span></div>
      <div><strong>2</strong><span>Create service account key</span></div>
      <div><strong>3</strong><span>Save GitHub secret</span></div>
      <div><strong>4</strong><span>Use Actions publish button</span></div>
    </section>
  `));
});

app.get('/auth/google', (req, res) => {
  const missing = missingConfig();
  if (missing.length) {
    res.status(500).send(renderPage('Configuration required', `
      <h1>Configuration required</h1>
      <p>Set these environment variables before using the portal:</p>
      <pre>${escapeHtml(missing.join('\n'))}</pre>
    `));
    return;
  }

  const state = randomState();
  res.cookie('zeywin_oauth_state', state, {
    httpOnly: true,
    sameSite: 'lax',
    secure: config.baseUrl.startsWith('https://'),
    maxAge: 10 * 60 * 1000
  });

  const client = oauthClient();
  const url = client.generateAuthUrl({
    access_type: 'offline',
    prompt: 'consent',
    state,
    scope: [
      'openid',
      'email',
      'profile',
      'https://www.googleapis.com/auth/cloud-platform',
      'https://www.googleapis.com/auth/androidpublisher'
    ]
  });
  res.redirect(url);
});

app.get('/oauth2callback', async (req, res) => {
  try {
    if (!req.query.state || req.query.state !== req.cookies.zeywin_oauth_state) {
      res.status(400).send(renderPage('Invalid session', '<h1>Invalid session</h1><p>Open the portal and start again.</p>'));
      return;
    }

    const client = oauthClient();
    const { tokens } = await client.getToken(String(req.query.code || ''));
    client.setCredentials(tokens);

    const oauth2 = google.oauth2({ version: 'v2', auth: client });
    const { data: profile } = await oauth2.userinfo.get();
    const email = String(profile.email || '').toLowerCase();

    if (config.allowedGoogleEmails.length && !config.allowedGoogleEmails.includes(email)) {
      res.status(403).send(renderPage('Account not allowed', `
        <h1>Account not allowed</h1>
        <p>The signed-in account <code>${escapeHtml(email)}</code> is not allowed to run setup.</p>
      `));
      return;
    }

    const result = await setupPublishing(client);
    const playConsoleUrl = 'https://play.google.com/console/developers/api-access';

    res.send(renderPage('Google Play connected', `
      <section class="hero">
        <p class="eyebrow">Connected account</p>
        <h1>Publishing secret is ready</h1>
        <p class="lead">GitHub secret <code>${escapeHtml(config.githubSecretName)}</code> was saved in <code>${escapeHtml(config.githubOwner)}/${escapeHtml(config.githubRepo)}</code>.</p>
      </section>
      <section class="result">
        <p><strong>Google account:</strong> <code>${escapeHtml(email)}</code></p>
        <p><strong>Service account:</strong> <code>${escapeHtml(result.serviceAccountEmail)}</code></p>
        <p><strong>Cloud project:</strong> <code>${escapeHtml(config.googleCloudProjectId)}</code></p>
        <div class="notice">
          If GitHub publish still fails with Google Play permission errors, open <a href="${playConsoleUrl}" target="_blank" rel="noreferrer">Play Console API access</a> and grant this service account release permissions for the app.
        </div>
      </section>
    `));
  } catch (error) {
    res.status(500).send(renderPage('Setup failed', `
      <h1>Setup failed</h1>
      <p>${escapeHtml(error.message || error)}</p>
      <p class="muted">Most common reason: the signed-in Google account is not an owner/admin of the Google Cloud project or Play Console API access.</p>
    `));
  }
});

async function setupPublishing(auth) {
  const projectName = `projects/${config.googleCloudProjectId}`;
  const serviceUsage = google.serviceusage({ version: 'v1', auth });
  const iam = google.iam({ version: 'v1', auth });

  await serviceUsage.services.enable({
    name: `${projectName}/services/androidpublisher.googleapis.com`
  });
  await serviceUsage.services.enable({
    name: `${projectName}/services/iam.googleapis.com`
  });

  const serviceAccountEmail = `${config.serviceAccountId}@${config.googleCloudProjectId}.iam.gserviceaccount.com`;
  let serviceAccount;

  try {
    const existing = await iam.projects.serviceAccounts.get({
      name: `${projectName}/serviceAccounts/${serviceAccountEmail}`
    });
    serviceAccount = existing.data;
  } catch (error) {
    if (error.code !== 404) {
      throw error;
    }
    const created = await iam.projects.serviceAccounts.create({
      name: projectName,
      requestBody: {
        accountId: config.serviceAccountId,
        serviceAccount: {
          displayName: config.serviceAccountDisplayName,
          description: 'Used by ZeyWin GitHub Actions to upload Android App Bundles to Google Play.'
        }
      }
    });
    serviceAccount = created.data;
  }

  const keyResponse = await iam.projects.serviceAccounts.keys.create({
    name: serviceAccount.name,
    requestBody: {
      privateKeyType: 'TYPE_GOOGLE_CREDENTIALS_FILE',
      keyAlgorithm: 'KEY_ALG_RSA_2048'
    }
  });

  const keyJson = Buffer.from(keyResponse.data.privateKeyData, 'base64').toString('utf8');
  await saveGitHubSecret(keyJson);

  return { serviceAccountEmail };
}

async function saveGitHubSecret(secretValue) {
  await sodium.ready;
  const octokit = new Octokit({ auth: config.githubToken });
  const { data: publicKey } = await octokit.actions.getRepoPublicKey({
    owner: config.githubOwner,
    repo: config.githubRepo
  });

  const encryptedBytes = sodium.crypto_box_seal(
    Buffer.from(secretValue),
    Buffer.from(publicKey.key, 'base64')
  );

  await octokit.actions.createOrUpdateRepoSecret({
    owner: config.githubOwner,
    repo: config.githubRepo,
    secret_name: config.githubSecretName,
    encrypted_value: Buffer.from(encryptedBytes).toString('base64'),
    key_id: publicKey.key_id
  });
}

app.listen(config.port, () => {
  console.log(`Google Play connect portal listening on ${config.baseUrl}`);
});
