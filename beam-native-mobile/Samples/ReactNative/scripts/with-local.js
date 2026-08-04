#!/usr/bin/env node
// Runs the Expo CLI with APP_VARIANT=local so app.config.js enables Android cleartext HTTP
// for a local-stack build (see the README's "Pointing at a local stack" section).
//
// This is a zero-dependency, cross-platform replacement for `cross-env APP_VARIANT=local …`:
// npm scripts can't set an env var inline on both Windows and POSIX shells, and Node is
// already a hard requirement of the project, so we set it here and delegate to `expo`.
//
// Usage (via package.json scripts): node scripts/with-local.js run:android [--variant release]
const { spawnSync } = require('child_process');

const args = process.argv.slice(2);
const result = spawnSync('npx', ['expo', ...args], {
  stdio: 'inherit',
  shell: true, // needed so `npx` resolves via the shell on Windows
  env: { ...process.env, APP_VARIANT: 'local' },
});

if (result.error) {
  console.error(result.error.message);
  process.exit(1);
}
process.exit(result.status ?? 1);
