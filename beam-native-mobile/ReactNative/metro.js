// Metro config helper for consuming the Beamable Web SDK in React Native.
//
// Usage in your metro.config.js:
//
//   const { getDefaultConfig } = require('expo/metro-config');
//   const { withBeamableSdk } = require('@beamable/notifications-react-native/metro');
//   module.exports = withBeamableSdk(getDefaultConfig(__dirname));
//
// `@beamable/sdk` now ships a native `react-native` build (selected via the
// package `exports` "react-native" condition), so this no longer redirects the
// SDK to the browser build. It only: (1) enables Metro's package-exports
// resolution so the "react-native" condition is honored, and (2) watches the
// external `file:` source folders so Metro indexes the linked SDK + this plugin
// during local development.
const fs = require('fs');
const path = require('path');

function safeRealpath(p) {
  try {
    return fs.realpathSync(p);
  } catch {
    return p;
  }
}

/**
 * Mutate and return an Expo/Metro config so it can bundle `@beamable/sdk` in RN.
 *
 * @param {object} config  the result of `getDefaultConfig(__dirname)`
 * @param {{ projectRoot?: string, repoRoot?: string, watchFolders?: string[] }} [opts]
 *   - projectRoot: defaults to `config.projectRoot` / cwd
 *   - repoRoot / watchFolders: extra folders to watch (Windows `subst` /
 *     `BEAM_REPO_ROOT` edge cases where the real siblings live off a short drive)
 */
function withBeamableSdk(config, opts = {}) {
  const projectRoot = opts.projectRoot || config.projectRoot || process.cwd();
  const resolver = config.resolver || (config.resolver = {});

  // Honor the SDK package's `exports` map so Metro selects its "react-native"
  // build condition. Ensure 'react-native' is among the resolved conditions.
  resolver.unstable_enablePackageExports = true;
  const conditions = new Set(resolver.unstable_conditionNames || []);
  conditions.add('react-native');
  conditions.add('require');
  conditions.add('import');
  resolver.unstable_conditionNames = Array.from(conditions);

  // Watchman off + Node crawler so every external watchFolder below is indexed
  // (Watchman's fallback otherwise leaves out-of-tree `file:` deps out of the map).
  resolver.useWatchman = false;

  const sdkSymlinkDir = path.join(
    projectRoot,
    'node_modules',
    '@beamable',
    'sdk',
  );

  // Watch the project + the real source of every Beamable `file:` dependency.
  const watch = new Set(config.watchFolders || []);
  watch.add(projectRoot);
  watch.add(safeRealpath(sdkSymlinkDir)); // @beamable/sdk real source (BeamableProduct/web)
  watch.add(safeRealpath(__dirname)); // this plugin's own source
  for (const extra of opts.watchFolders || []) watch.add(path.resolve(extra));
  if (opts.repoRoot) watch.add(path.resolve(opts.repoRoot));
  config.watchFolders = Array.from(watch);

  const nmPaths = new Set(resolver.nodeModulesPaths || []);
  nmPaths.add(path.join(projectRoot, 'node_modules'));
  nmPaths.add(path.join(safeRealpath(sdkSymlinkDir), 'node_modules'));
  resolver.nodeModulesPaths = Array.from(nmPaths);

  return config;
}

module.exports = { withBeamableSdk };
