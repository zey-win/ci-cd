import crypto from 'node:crypto';
import { createRequire } from 'node:module';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import cookieParser from 'cookie-parser';
import express from 'express';
import { google } from 'googleapis';
import { Octokit } from '@octokit/rest';

const require = createRequire(import.meta.url);
const sodium = require('libsodium-wrappers');

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

const playConsoleAccounts = [
  {
    name: 'ASAD DEV',
    developerId: '4617426840232156042'
  },
  {
    name: 'Plinko apps',
    developerId: '5922122594113010907'
  },
  {
    name: 'Casino Beast Mobile Apps',
    developerId: '5744111115048602760'
  },
  {
    name: 'SALAMAT JN',
    developerId: '9163726882297311441'
  },
  {
    name: 'Play 777 Slots',
    developerId: '7898824668858143466'
  },
  {
    name: 'Play Max Solutions',
    developerId: '7373129813717950889'
  },
  {
    name: 'Popaads',
    developerId: '7964884476190249217'
  },
  {
    name: 'Сasino Slots Free',
    developerId: '5841159787162622259'
  }
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
app.use(express.urlencoded({ extended: false }));
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
  const ready = missing.length === 0;

  res.send(renderPage('Connect Google Play', `
    <section class="hero">
      <p class="eyebrow">ZeyWin CI/CD</p>
      <h1>Connect Google Play publishing</h1>
      <p class="lead">Sign in with the Google account that owns or administers Play Console. ZeyWin prepares the publishing connection in the background.</p>
      ${ready ? '' : '<div class="notice danger">Connection setup is being prepared. Please try again later.</div>'}
      <a class="button ${ready ? '' : 'disabled'}" href="${ready ? '/auth/google' : '/operator'}">Connect Google Play</a>
    </section>
    <section class="steps">
      <div><strong>1</strong><span>Google sign-in</span></div>
      <div><strong>2</strong><span>Create service account key</span></div>
      <div><strong>3</strong><span>Save GitHub secret</span></div>
      <div><strong>4</strong><span>Use Actions publish button</span></div>
    </section>
  `));
});

app.get('/operator', (_req, res) => {
  const ready = missingConfig().length === 0;
  res.send(renderPage('Operator flow', `
    <section class="hero">
      <p class="eyebrow">For marketers</p>
      <h1>Three clicks only</h1>
      <p class="lead">The operator flow is intentionally simple. No API keys, JSON files, Google Cloud screens, or GitHub secrets are shown here.</p>
      <section class="steps">
        <div><strong>1</strong><span>Open this page</span></div>
        <div><strong>2</strong><span>Sign in with Google</span></div>
        <div><strong>3</strong><span>Confirm access</span></div>
      </section>
      ${ready
        ? '<a class="button" href="/auth/google">Start Google connection</a>'
        : '<div class="notice danger">Connection setup is being prepared. Please try again later.</div>'}
    </section>
  `));
});

const partnerBenefits = [
  'Ready-to-publish mobile games for Google Play',
  'No password sharing and no recovery-code sharing',
  'Limited Play Console access only when a release is ready',
  'Revenue-share friendly process for console owners',
  'Screenshots, release notes, and build reports prepared by ZeyWin'
];

app.get('/partners', (_req, res) => {
  res.send(renderPage('Publish Games With ZeyWin', `
    <section class="partner-hero">
      <div>
        <p class="eyebrow">Partner program</p>
        <h1>Publish polished mobile games in your Google Play Console</h1>
        <p class="lead">ZeyWin prepares the game build, SDK setup, store assets, and release report. Console owners keep control of their account and grant only limited publishing access when they are ready.</p>
        <div class="actions">
          <a class="button" href="#apply">Apply as console partner</a>
          <a class="button secondary" href="/partners/guide">How access works</a>
        </div>
      </div>
      <div class="partner-panel">
        <strong>Zero password exchange</strong>
        <span>We never ask for Google passwords, backup codes, or personal account recovery data.</span>
      </div>
    </section>
    <section class="partner-strip">
      <div><strong>1</strong><span>Apply</span></div>
      <div><strong>2</strong><span>Review game fit</span></div>
      <div><strong>3</strong><span>Grant limited access</span></div>
      <div><strong>4</strong><span>Publish and track revenue</span></div>
    </section>
    <section class="growth-system">
      <h2>Console partner growth loop</h2>
      <p>Use this as the gradual system for increasing the number of partner consoles without asking for account credentials.</p>
      <div class="growth-grid">
        <div><strong>Week 1</strong><span>Collect warm leads from known marketers, studios, and console owners.</span></div>
        <div><strong>Week 2</strong><span>Publish 1 demo case with screenshots, launch video, and revenue-share terms.</span></div>
        <div><strong>Week 3</strong><span>Give every accepted partner a referral link and a bonus for each approved console.</span></div>
        <div><strong>Week 4</strong><span>Move active partners into repeat publishing: one new app slot per console per cycle.</span></div>
      </div>
    </section>
    <section class="funnel-board">
      <h2>Partner funnel statuses</h2>
      <div class="status-lanes">
        <div><strong>Lead</strong><span>Contact submitted. No access yet.</span></div>
        <div><strong>Qualified</strong><span>Console owner confirmed app category and country.</span></div>
        <div><strong>Access ready</strong><span>Limited Play Console access granted.</span></div>
        <div><strong>Published</strong><span>First app/update sent for review.</span></div>
        <div><strong>Referral</strong><span>Partner invited another console owner.</span></div>
      </div>
    </section>
    <section class="help-grid">
      ${partnerBenefits.map((item) => `<div><h2>${escapeHtml(item)}</h2><p>Built for operators who want a simple, documented publishing process without technical setup.</p></div>`).join('')}
      <div><h2>Fast handoff</h2><p>Each app can include build artifacts, screenshots, logs, and release notes in the repository.</p></div>
    </section>
    <section class="partner-form" id="apply">
      <h2>Console partner application</h2>
      <p>Submit contact details only. A ZeyWin operator will reply with the limited-access instructions.</p>
      <form method="post" action="/partners/apply">
        <label>Contact email<input name="email" type="email" required placeholder="owner@example.com"></label>
        <label>Play Console company/name<input name="consoleName" required placeholder="Company or developer name"></label>
        <label>Country<input name="country" required placeholder="Country"></label>
        <label>Messenger<input name="messenger" placeholder="Telegram, WhatsApp, or Skype"></label>
        <label>Referral code<input name="referral" placeholder="Optional partner or marketer code"></label>
        <label>Monthly traffic or publishing capacity<textarea name="notes" rows="4" placeholder="How many apps can you publish, what geos, what categories?"></textarea></label>
        <button class="button form-button" type="submit">Send application</button>
      </form>
      <p class="muted">By submitting, partners agree that passwords, 2FA codes, and recovery codes must not be shared.</p>
    </section>
  `));
});

app.post('/partners/apply', (req, res) => {
  const submittedAt = new Date().toISOString();
  const fields = {
    submittedAt,
    email: String(req.body.email || '').trim(),
    consoleName: String(req.body.consoleName || '').trim(),
    country: String(req.body.country || '').trim(),
    messenger: String(req.body.messenger || '').trim(),
    referral: String(req.body.referral || '').trim(),
    notes: String(req.body.notes || '').trim()
  };

  res.send(renderPage('Application received', `
    <section class="hero">
      <p class="eyebrow">Application received</p>
      <h1>Partner lead saved for operator review</h1>
      <p class="lead">Use the summary below in CRM, GitHub issue, or marketer chat. No secret data was collected.</p>
      <pre>${escapeHtml(JSON.stringify(fields, null, 2))}</pre>
      <div class="actions">
        <a class="button" href="/partners/guide">Show access guide</a>
        <a class="button secondary" href="/partners">Back to partner page</a>
      </div>
    </section>
  `));
});

app.get('/partners/guide', (_req, res) => {
  res.send(renderPage('Partner Access Guide', `
    <section class="hero">
      <p class="eyebrow">Safe access guide</p>
      <h1>How console owners grant publishing access</h1>
      <p class="lead">This is the marketer-friendly version. Partners keep their Google account private and add ZeyWin as a limited user or service account in Play Console.</p>
    </section>
    <section class="timeline">
      <h2>Partner steps</h2>
      <div class="timeline-row">
        <div><span>1</span><strong>Open Play Console users</strong><p>Go to Users and permissions in the partner's Play Console.</p></div>
        <div><span>2</span><strong>Add ZeyWin access</strong><p>Add the ZeyWin publishing email or service account supplied by the operator.</p></div>
        <div><span>3</span><strong>Limit permissions</strong><p>Grant release/upload permissions only for the selected app.</p></div>
        <div><span>4</span><strong>Review release</strong><p>ZeyWin prepares the build, report, screenshots, and release notes for approval.</p></div>
      </div>
    </section>
    <section class="quick-actions">
      <h2>Rules</h2>
      <ul class="rules">
        <li>No Google passwords.</li>
        <li>No backup or recovery codes.</li>
        <li>No personal Gmail takeover.</li>
        <li>Only Play Console permissions that can be revoked by the owner.</li>
      </ul>
      <div class="actions">
        <a class="button" href="/partners">Open partner landing</a>
        <a class="button secondary" href="/help">Open operator help</a>
      </div>
    </section>
  `));
});

app.get('/ready.json', (_req, res) => {
  const missing = missingConfig();
  res.json({
    ready: missing.length === 0,
    state: missing.length === 0 ? 'ready' : 'preparing'
  });
});

app.get('/play-accounts.json', (_req, res) => {
  res.json({
    accounts: playConsoleAccounts.map((account) => ({
      ...account,
      consoleUrl: `https://play.google.com/console/u/0/developers/${account.developerId}/app-list`
    }))
  });
});

app.get('/help', (_req, res) => {
  const ready = missingConfig().length === 0;
  const workflowUrl = 'https://github.com/zey-win/ci-cd/actions/workflows/bootstrap-google-play-connect.yml';
  const accountCards = playConsoleAccounts
    .map((account) => {
      const url = `https://play.google.com/console/u/0/developers/${account.developerId}/app-list`;
      return `<a class="account-card" href="${url}" target="_blank" rel="noreferrer">
        <strong>${escapeHtml(account.name)}</strong>
        <code>${escapeHtml(account.developerId)}</code>
      </a>`;
    })
    .join('');

  res.send(renderPage('Google Play Connect Help', `
    <section class="hero">
      <p class="eyebrow">Self-service guide</p>
      <h1>Google Play Connect</h1>
      <p class="lead">This page is for the next person who opens the project. Operators use the blue button. Setup checks are safe and do not publish apps.</p>
      <div class="readiness ${ready ? 'ready' : 'preparing'}">
        <span></span>
        <strong>${ready ? 'Ready for operators' : 'Setup is preparing'}</strong>
      </div>
      <div class="diagram">
        <div class="phone-card">
          <div class="phone-top"></div>
          <div class="phone-screen">
            <span>1</span>
            <strong>Open link</strong>
          </div>
        </div>
        <div class="arrow">→</div>
        <div class="phone-card">
          <div class="phone-top"></div>
          <div class="phone-screen">
            <span>2</span>
            <strong>Google sign-in</strong>
          </div>
        </div>
        <div class="arrow">→</div>
        <div class="phone-card">
          <div class="phone-top"></div>
          <div class="phone-screen">
            <span>3</span>
            <strong>Confirm</strong>
          </div>
        </div>
      </div>
      <div class="actions">
        <a class="button" href="/operator">Open operator page</a>
        <a class="button secondary" href="/status">Check setup status</a>
        <a class="button secondary" href="${workflowUrl}">Open setup workflow</a>
      </div>
    </section>
    <section class="help-grid">
      <div>
        <h2>For operators</h2>
        <p>Use only the Connect button. Do not look for API keys, JSON files, Google Cloud, or GitHub secrets.</p>
      </div>
      <div>
        <h2>For setup</h2>
        <p>Open the setup workflow and run <code>check_only</code>. It prints the exact redirect URI and next action.</p>
      </div>
      <div>
        <h2>After setup</h2>
        <p>The Connect button signs in with Google and prepares publishing for GitHub Actions.</p>
      </div>
    </section>
    <section class="timeline">
      <h2>If the button is not ready yet</h2>
      <div class="timeline-row">
        <div><span>✓</span><strong>Open guide</strong><p>You are here. No technical access is required to read this page.</p></div>
        <div><span>✓</span><strong>Run setup check</strong><p>The workflow prints the exact next step without changing production.</p></div>
        <div><span>✓</span><strong>Apply setup</strong><p>When Google OAuth values exist, the workflow saves them to Render.</p></div>
        <div><span>✓</span><strong>Use Connect</strong><p>Operators press the button and confirm Google access.</p></div>
      </div>
    </section>
    <section class="quick-actions">
      <h2>Setup buttons</h2>
      <p>GitHub opens the workflow screen. Choose <code>check_only</code> first. It is safe and does not change Render.</p>
      <div class="actions">
        <a class="button" href="${workflowUrl}">Run safe setup check</a>
        <a class="button secondary" href="${workflowUrl}">Apply setup after Google values exist</a>
      </div>
    </section>
    <section class="account-list">
      <h2>Known Play Console accounts</h2>
      <p>These accounts were found in the logged-in browser and are linked here so future operators do not have to search manually.</p>
      <div class="account-grid">${accountCards}</div>
    </section>
  `));
});

app.get('/status', (_req, res) => {
  const missing = missingConfig();
  const rows = requiredEnv
    .map((key) => {
      const ready = !missing.includes(key);
      return `<tr><td><code>${escapeHtml(key)}</code></td><td class="${ready ? 'ok' : 'missing'}">${ready ? 'Ready' : 'Missing'}</td></tr>`;
    })
    .join('');

  res.send(renderPage('Connect status', `
    <section class="hero">
      <p class="eyebrow">ZeyWin CI/CD</p>
      <h1>Connect status</h1>
      <p class="lead">${missing.length ? 'Administrator configuration is not finished yet.' : 'Google Play connection portal is ready.'}</p>
      <table class="status-table">
        <thead><tr><th>Setting</th><th>Status</th></tr></thead>
        <tbody>${rows}</tbody>
      </table>
      <div class="notice">
        Admin-only diagnostics. Marketing flow: <a href="/operator">/operator</a>. Public entry page: <a href="https://zey-win.github.io/connect/">https://zey-win.github.io/connect/</a>
      </div>
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
