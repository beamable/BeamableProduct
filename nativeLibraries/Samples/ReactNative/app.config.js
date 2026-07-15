// Dynamic Expo config. Extends app.json and injects the Beamable API base URL from the
// local, uncommitted `env.local` file so the API target can be changed without editing
// source. `env.local` is a plain KEY=VALUE file (Vite-style `VITE_API_BASE`); Expo doesn't
// auto-load it (wrong name + non-EXPO_PUBLIC prefix), so we read it here in Node and pass it
// through `extra`, where the app reads it via `expo-constants` (see src/beam/config.ts).
//
// A plain-HTTP backend (a LAN local stack, `http://<ip>:8080`) also needs Android cleartext
// HTTP. Rather than commit that manifest-wide relaxation to app.json — where it would apply
// to every build, including release — it is injected here ONLY for the explicit local build
// variant, so the committed config stays TLS-only. The single trigger is `APP_VARIANT=local`
// (set by the `:local` npm scripts; mirrors the BEAM_REPO_ROOT env convention in
// metro.config.js). It is deliberately NOT inferred from the API URL, so the build variant —
// not a config value — decides the native security posture, and remote/release builds always
// stay TLS-only. `env.local` only chooses which backend URL to target.
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

module.exports = ({ config }) => {
  const apiBase = readApiBase();
  // Cleartext is opt-in via the explicit local build variant only (`APP_VARIANT=local`, set
  // by the `:local` npm scripts). Never committed, never inferred from the URL — so
  // remote/release builds always keep Android's default TLS-only enforcement.
  const usesCleartext = process.env.APP_VARIANT === 'local';

  const plugins = [...(config.plugins || [])];
  if (usesCleartext) {
    plugins.push(['expo-build-properties', { android: { usesCleartextTraffic: true } }]);
  }

  return {
    ...config,
    plugins,
    extra: {
      ...(config.extra || {}),
      // The Beamable API base URL (e.g. https://dev.api.beamable.com). Undefined falls back
      // to the named environment in src/beam/config.ts.
      apiBase,
      // Surfaced for diagnostics: whether this build allows cleartext HTTP.
      usesCleartext,
    },
  };
};
