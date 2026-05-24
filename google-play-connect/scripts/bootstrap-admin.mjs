#!/usr/bin/env node
import { execFileSync, spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import process from 'node:process';

const required = ['RENDER_API_KEY', 'GITHUB_TOKEN'];
const renderServiceId = process.env.RENDER_SERVICE_ID || 'srv-d8948p3eo5us738g5dag';
const renderBaseUrl = 'https://api.render.com/v1';
const redirectUri = process.env.GOOGLE_REDIRECT_URI || 'https://zeywin-connect.onrender.com/oauth2callback';

function fail(message) {
  console.error(`\n${message}`);
  process.exit(1);
}

function env(name) {
  return process.env[name] || '';
}

function run(command, args, options = {}) {
  const result = spawnSync(command, args, {
    stdio: options.capture ? ['ignore', 'pipe', 'pipe'] : 'inherit',
    text: true
  });
  if (result.status !== 0) {
    if (options.optional) {
      return null;
    }
    const stderr = result.stderr ? `\n${result.stderr}` : '';
    fail(`Command failed: ${command} ${args.join(' ')}${stderr}`);
  }
  return options.capture ? result.stdout.trim() : '';
}

function jsonRequest(method, url, body) {
  const args = [
    '-sS',
    '-X',
    method,
    url,
    '-H',
    `Authorization: Bearer ${env('RENDER_API_KEY')}`,
    '-H',
    'Content-Type: application/json'
  ];
  if (body !== undefined) {
    args.push('-d', JSON.stringify(body));
  }
  const output = execFileSync('curl', args, { encoding: 'utf8' });
  try {
    return JSON.parse(output);
  } catch {
    return output;
  }
}

function readRenderKeyFromCli() {
  const cliPath = path.join(os.homedir(), '.render', 'cli.yaml');
  if (!existsSync(cliPath)) {
    return '';
  }
  const text = readFileSync(cliPath, 'utf8');
  const match = text.match(/^\s+key:\s*(.+)$/m);
  return match?.[1]?.trim() || '';
}

function requireTools() {
  for (const tool of ['curl', 'gcloud']) {
    run(tool, ['--version'], { capture: true, optional: false });
  }
}

async function main() {
  if (!env('RENDER_API_KEY')) {
    const renderKey = readRenderKeyFromCli();
    if (renderKey) {
      process.env.RENDER_API_KEY = renderKey;
    }
  }

  const missing = required.filter((name) => !env(name));
  if (missing.length) {
    fail(`Missing required environment variables: ${missing.join(', ')}\n\nExample:\n  export GITHUB_TOKEN=\"$(gh auth token)\"\n  export RENDER_API_KEY=\"rnd_...\"`);
  }

  requireTools();

  let projectId = env('GOOGLE_CLOUD_PROJECT_ID');
  if (!projectId) {
    projectId = run('gcloud', ['config', 'get-value', 'project'], { capture: true, optional: true }) || '';
  }
  if (!projectId || projectId === '(unset)') {
    fail('GOOGLE_CLOUD_PROJECT_ID is missing. Create/select a Google Cloud project once, then rerun this script.');
  }

  console.log(`Using Google Cloud project: ${projectId}`);
  run('gcloud', ['services', 'enable', 'androidpublisher.googleapis.com', 'iam.googleapis.com', 'serviceusage.googleapis.com', '--project', projectId]);

  const clientId = env('GOOGLE_CLIENT_ID');
  const clientSecret = env('GOOGLE_CLIENT_SECRET');
  if (!clientId || !clientSecret) {
    console.log('\nGoogle OAuth Web Client is still required.');
    console.log('Create it once in Google Cloud Console with this redirect URI:');
    console.log(`  ${redirectUri}`);
    console.log('\nThen rerun:');
    console.log('  export GOOGLE_CLIENT_ID="..."');
    console.log('  export GOOGLE_CLIENT_SECRET="..."');
    console.log(`  export GOOGLE_CLOUD_PROJECT_ID="${projectId}"`);
    console.log('  node google-play-connect/scripts/bootstrap-admin.mjs');
    process.exit(2);
  }

  const values = {
    GOOGLE_CLIENT_ID: clientId,
    GOOGLE_CLIENT_SECRET: clientSecret,
    GOOGLE_CLOUD_PROJECT_ID: projectId,
    GITHUB_TOKEN: env('GITHUB_TOKEN')
  };

  for (const [key, value] of Object.entries(values)) {
    const response = jsonRequest(
      'PUT',
      `${renderBaseUrl}/services/${renderServiceId}/env-vars/${encodeURIComponent(key)}`,
      { value }
    );
    const ok = response?.key === key || response?.envVar?.key === key || response?.id;
    if (!ok) {
      console.log(JSON.stringify(response, null, 2));
      fail(`Failed to update Render env var: ${key}`);
    }
    console.log(`Updated Render env: ${key}`);
  }

  console.log('\nBootstrap values are saved. Redeploy Render service after this script.');
}

main().catch((error) => fail(error.stack || error.message || String(error)));
