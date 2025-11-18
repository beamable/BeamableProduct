#!/usr/bin/env node
import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import readline from 'node:readline';
import process from 'node:process';
import console from 'node:console';
import semver from 'semver';

let reprompt = false;
const filePath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  'package.json',
);
const packageJson = JSON.parse(await readFile(filePath, 'utf8'));
const currentVersion = packageJson.version;

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
});

const ask = (query) => new Promise((resolve) => rl.question(query, resolve));

do {
  const answer = (
    await ask(`Current version is ${currentVersion}. Enter a new version: `)
  ).trim();

  if (!answer) {
    console.error('No version provided; nothing was changed.');
    reprompt = true;
    continue;
  }

  if (!semver.valid(answer)) {
    console.error(
      'Invalid version string. Use semantic versioning, e.g. 1.2.3 or 1.2.3-beta.1',
    );
    reprompt = true;
    continue;
  }

  if (!semver.gt(answer, currentVersion)) {
    console.error('New version must be greater than the current version.');
    reprompt = true;
    continue;
  }

  reprompt = false;
  rl.close();

  packageJson.version = answer;
  await writeFile(filePath, `${JSON.stringify(packageJson, null, 2)}\n`);
  console.log(`package.json version updated to ${answer}`);
} while (reprompt);
