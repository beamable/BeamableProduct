// Dynamic Expo config. Extends app.json and injects the Beamable API base URL from the
// local, uncommitted `env.local` file so the API target can be changed without editing
// source. `env.local` is a plain KEY=VALUE file (Vite-style `VITE_API_BASE`); Expo doesn't
// auto-load it (wrong name + non-EXPO_PUBLIC prefix), so we read it here in Node and pass it
// through `extra`, where the app reads it via `expo-constants` (see src/beam/config.ts).
const fs = require('fs');
const path = require('path');

/** Read VITE_API_BASE from env.local, or undefined if the file/key is absent. */
function readApiBase() {
  try {
    const txt = fs.readFileSync(path.join(__dirname, 'env.local'), 'utf8');
    const match = txt.match(/^\s*VITE_API_BASE\s*=\s*(.+?)\s*$/m);
    return match ? match[1].replace(/^["']|["']$/g, '').trim() : undefined;
  } catch {
    return undefined;
  }
}

module.exports = ({ config }) => ({
  ...config,
  extra: {
    ...(config.extra || {}),
    // The Beamable API base URL (e.g. https://dev.api.beamable.com). Undefined falls back
    // to the named environment in src/beam/config.ts.
    apiBase: readApiBase(),
  },
});
