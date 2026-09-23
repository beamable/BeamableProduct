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

/** Read one KEY=VALUE from env.local, or undefined if the file/key is absent. */
function readEnvLocal(key) {
  try {
    const txt = fs.readFileSync(path.join(__dirname, 'env.local'), 'utf8');
    const match = txt.match(new RegExp(`^\\s*${key}\\s*=\\s*(.+?)\\s*$`, 'm'));
    return match ? match[1].replace(/^["']|["']$/g, '').trim() : undefined;
  } catch {
    return undefined;
  }
}

module.exports = ({ config }) => {
  const apiBase = readEnvLocal('VITE_API_BASE');
  // Routing key for microservices started with `beam project run` (which is how the local stack
  // runs them — see .beamable/local-stack.json). Such a service registers its binding behind a
  // per-machine key and the platform routes to it ONLY for callers presenting that key; without
  // it every microservice call answers `BindingNotFoundException`. `beam fed local-key` prints
  // it. Leave it unset for realm-deployed services, which bind unkeyed.
  const routingKey = readEnvLocal('BEAM_ROUTING_KEY');
  // Cleartext is opt-in via the explicit local build variant only (`APP_VARIANT=local`, set
  // by the `:local` npm scripts). Never committed, never inferred from the URL — so
  // remote/release builds always keep Android's default TLS-only enforcement.
  const usesCleartext = process.env.APP_VARIANT === 'local';

  const plugins = [...(config.plugins || [])];
  if (usesCleartext) {
    plugins.push(['expo-build-properties', { android: { usesCleartextTraffic: true } }]);
  }

  // iOS counterpart of the Android cleartext relaxation above. iOS App Transport Security
  // blocks plain HTTP by default and (unlike Android) is unaffected by the cleartext flag, so a
  // LAN local stack at `http://<private-ip>:8080` is otherwise unreachable. `NSAllowsLocalNetworking`
  // permits cleartext to local/private-range hosts (`.local`, `10/8`, `172.16/12`, `192.168/16`)
  // WITHOUT the blanket `NSAllowsArbitraryLoads`. Gated on the same `APP_VARIANT=local` switch, so
  // it never reaches remote/release builds.
  const ios = { ...(config.ios || {}) };
  if (usesCleartext) {
    ios.infoPlist = {
      ...(ios.infoPlist || {}),
      NSAppTransportSecurity: {
        ...(ios.infoPlist?.NSAppTransportSecurity || {}),
        NSAllowsLocalNetworking: true,
      },
    };
  }

  return {
    ...config,
    plugins,
    ios,
    extra: {
      ...(config.extra || {}),
      // The Beamable API base URL (e.g. https://dev.api.beamable.com). Undefined falls back
      // to the named environment in src/beam/config.ts.
      apiBase,
      // The local-microservice routing key, or undefined for realm-deployed services.
      routingKey,
      // Surfaced for diagnostics: whether this build allows cleartext HTTP.
      usesCleartext,
    },
  };
};
