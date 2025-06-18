#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

// Post-build helper: rename TokenStorage-*.d.ts chunks to a stable filename and patch imports
const dist = path.resolve(__dirname, '../dist');

for (const sub of ['', 'platform']) {
  const dir = path.join(dist, sub);
  if (!fs.existsSync(dir)) continue;
  for (const file of fs.readdirSync(dir)) {
    const m = file.match(/^TokenStorage-[^]+\.(d\.ts|d\.mts)$/);
    if (m) {
      fs.renameSync(
        path.join(dir, file),
        path.join(dir, `TokenStorage.${m[1]}`),
      );
    }
  }
}

for (const rel of [
  'index.d.ts',
  'index.d.mts',
  path.join('platform', 'index.d.ts'),
  path.join('platform', 'index.d.mts'),
]) {
  const p = path.join(dist, rel);
  if (!fs.existsSync(p)) continue;
  const content = fs
    .readFileSync(p, 'utf8')
    .replace(/TokenStorage-[^"'\/]+/g, 'TokenStorage');
  fs.writeFileSync(p, content);
}
