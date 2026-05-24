#!/usr/bin/env node
import process from 'node:process';

import { google } from 'googleapis';

function fail(message) {
  console.error(message);
  process.exit(1);
}

const packageName = process.env.PACKAGE_NAME || process.argv[2] || '';
const serviceAccountJson = process.env.GOOGLE_PLAY_SERVICE_ACCOUNT_JSON || '';

if (!packageName) {
  fail('PACKAGE_NAME is required.');
}

if (!serviceAccountJson) {
  fail('GOOGLE_PLAY_SERVICE_ACCOUNT_JSON is required.');
}

let credentials;
try {
  credentials = JSON.parse(serviceAccountJson);
} catch {
  fail('GOOGLE_PLAY_SERVICE_ACCOUNT_JSON is not valid JSON.');
}

const auth = new google.auth.GoogleAuth({
  credentials,
  scopes: ['https://www.googleapis.com/auth/androidpublisher']
});

const androidpublisher = google.androidpublisher({ version: 'v3', auth });

try {
  const edit = await androidpublisher.edits.insert({
    packageName,
    requestBody: {}
  });

  if (edit.data.id) {
    await androidpublisher.edits.delete({
      packageName,
      editId: edit.data.id
    }).catch(() => {});
  }

  console.log(`Google Play API access OK for ${packageName}.`);
} catch (error) {
  const status = error.code || error.response?.status || 'unknown';
  const detail = error.errors?.[0]?.message || error.message || String(error);
  console.error(`Google Play API access failed for ${packageName}.`);
  console.error(`Status: ${status}`);
  console.error(`Reason: ${detail}`);
  console.error('');
  console.error('Grant this service account access in Play Console before publishing:');
  console.error(credentials.client_email || '(service account email missing in JSON)');
  process.exit(1);
}
