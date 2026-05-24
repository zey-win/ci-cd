#!/usr/bin/env node
import { existsSync } from 'node:fs';
import path from 'node:path';
import process from 'node:process';

import { chromium } from 'playwright';

function parseArgs(argv) {
  const args = {};
  for (let index = 0; index < argv.length; index += 1) {
    const item = argv[index];
    if (!item.startsWith('--')) {
      continue;
    }
    const key = item.slice(2);
    const next = argv[index + 1];
    if (!next || next.startsWith('--')) {
      args[key] = true;
    } else {
      args[key] = next;
      index += 1;
    }
  }
  return args;
}

function required(args, key) {
  const value = args[key];
  if (!value) {
    console.error(`Missing --${key}`);
    process.exit(1);
  }
  return String(value);
}

async function clickFirst(page, patterns, options = {}) {
  for (const pattern of patterns) {
    const locator = page.getByRole('button', { name: pattern }).first();
    if (await locator.count()) {
      await locator.click(options);
      return true;
    }
    const link = page.getByRole('link', { name: pattern }).first();
    if (await link.count()) {
      await link.click(options);
      return true;
    }
    const text = page.getByText(pattern).first();
    if (await text.count()) {
      await text.click(options);
      return true;
    }
  }
  return false;
}

async function setFile(page, aabPath) {
  const input = page.locator('input[type="file"]').first();
  await input.setInputFiles(aabPath);
}

async function fillReleaseNotes(page, notes) {
  const textareas = page.locator('textarea');
  const count = await textareas.count();
  if (count > 0) {
    await textareas.nth(count - 1).fill(notes);
    return true;
  }

  const editable = page.locator('[contenteditable="true"]').last();
  if (await editable.count()) {
    await editable.fill(notes);
    return true;
  }

  return false;
}

const args = parseArgs(process.argv.slice(2));
const developerId = required(args, 'developer-id');
const appId = required(args, 'app-id');
const track = required(args, 'track');
const aabPath = path.resolve(required(args, 'aab'));
const versionName = required(args, 'version-name');
const notes = String(args.notes || `Bug fixes and SDK updates for ${versionName}.`);
const submit = Boolean(args.submit);
const dryRun = Boolean(args['dry-run']);

if (!existsSync(aabPath)) {
  console.error(`AAB does not exist: ${aabPath}`);
  process.exit(1);
}

const profileDir = path.resolve('.play-console-profile');
const appDashboardUrl = `https://play.google.com/console/u/0/developers/${developerId}/app/${appId}/app-dashboard`;
const trackUrl = `https://play.google.com/console/u/0/developers/${developerId}/app/${appId}/tracks/${track}`;
const chromeCandidates = [
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  '/Applications/Chromium.app/Contents/MacOS/Chromium',
  '/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge'
];
const executablePath = chromeCandidates.find((candidate) => existsSync(candidate));

console.log(`Developer: ${developerId}`);
console.log(`App: ${appId}`);
console.log(`Track: ${track}`);
console.log(`AAB: ${aabPath}`);
console.log(`Release: ${versionName}`);
console.log(`Submit: ${submit ? 'yes' : 'no, draft/save only'}`);

let context;
try {
  context = await chromium.launchPersistentContext(profileDir, {
    headless: false,
    viewport: { width: 1440, height: 1000 },
    acceptDownloads: true,
    executablePath
  });
} catch (error) {
  if (/Executable doesn't exist/i.test(error.message || '')) {
    console.error('Playwright browser is missing. Run: npx playwright install chromium');
  }
  throw error;
}

const page = context.pages()[0] || await context.newPage();
page.setDefaultTimeout(30000);

await page.goto(appDashboardUrl);
await page.waitForLoadState('domcontentloaded');

if ((await page.locator('text=/Sign in|Вхід|Увійти/i').count()) > 0) {
  console.log('Sign in in the opened browser, then rerun the same command.');
  await page.pause();
  await context.close();
  process.exit(2);
}

await page.goto(trackUrl);
await page.waitForLoadState('domcontentloaded');
await page.waitForTimeout(5000);

if (dryRun) {
  console.log(`Opened track URL: ${trackUrl}`);
  await context.close();
  process.exit(0);
}

await clickFirst(page, [/Create new release/i, /Edit release/i, /Create release/i, /New release/i]);
await page.waitForTimeout(4000);

if ((await page.locator('input[type="file"]').count()) === 0) {
  await clickFirst(page, [/Upload/i, /Add from library/i, /App bundles/i]);
  await page.waitForTimeout(2000);
}

await setFile(page, aabPath);
console.log('AAB selected for upload. Waiting for Play Console processing.');
await page.waitForTimeout(30000);

const releaseNameFields = page.locator('input');
for (let index = 0; index < await releaseNameFields.count(); index += 1) {
  const field = releaseNameFields.nth(index);
  const label = [
    await field.getAttribute('aria-label').catch(() => ''),
    await field.getAttribute('placeholder').catch(() => '')
  ].join(' ');
  if (/release|name/i.test(label)) {
    await field.fill(versionName);
    break;
  }
}

await fillReleaseNotes(page, notes);

await clickFirst(page, [/Save/i, /Review release/i, /Next/i]);
await page.waitForTimeout(8000);

if (submit) {
  await clickFirst(page, [/Start rollout/i, /Send changes for review/i, /Submit/i, /Roll out/i]);
  await page.waitForTimeout(8000);
  console.log('Submission action was attempted. Check the browser for final Play Console status.');
} else {
  console.log('Draft/review step reached. Final submit was intentionally skipped. Add --submit to continue through final submission screens.');
}

console.log(`Current URL: ${page.url()}`);
await context.close();
