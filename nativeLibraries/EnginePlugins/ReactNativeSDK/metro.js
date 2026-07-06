// Metro config helper for consuming the Beamable Web SDK in React Native.
//
// Usage in your metro.config.js:
//
//   const { getDefaultConfig } = require('expo/metro-config');
//   const { withBeamableSdk } = require('@beamable/sdk-react-native/metro');
//   module.exports = withBeamableSdk(getDefaultConfig(__dirname));
//
// It resolves `@beamable/sdk` (and `@beamable/sdk/api`) to the SDK's *browser*
// build — the fetch-based build that works in RN alongside the package's
// polyfills — and watches the external `file:` source folders so Metro indexes
// them. (The SDK ships no `react-native` export condition, so Metro can't pick
// the right build on its own; this does it.)
const fs = require('fs');
const path = require('path');

function safeRealpath(p) {
  try {
    return fs.realpathSync(p);
  } catch {
    return p;
  }
}

/** node_modules/<name> under a dir, or null if absent (kept as the symlink path). */
function symlinkDir(name, fromDir) {
  const p = path.join(fromDir, 'node_modules', ...name.split('/'));
  return fs.existsSync(p) ? p : null;
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

  // Resolve the SDK through its node_modules SYMLINK path (not the realpath): Metro
  // keys its file map by the crawled node_modules path, so pointing at the symlink's
  // real out-of-tree target would make the SHA-1 lookup miss.
  const sdkSymlinkDir = path.join(projectRoot, 'node_modules', '@beamable', 'sdk');
  const beamMain = path.join(sdkSymlinkDir, 'dist', 'browser', 'index.mjs');
  const beamApi = path.join(sdkSymlinkDir, 'dist', 'api.mjs');

  // Watchman off + Node crawler so every external watchFolder below is indexed
  // (Watchman's fallback otherwise leaves out-of-tree `file:` deps out of the map).
  resolver.useWatchman = false;

  // Watch the project + the real source of every Beamable `file:` dependency.
  const watch = new Set(config.watchFolders || []);
  watch.add(projectRoot);
  watch.add(safeRealpath(sdkSymlinkDir)); // @beamable/sdk real source (BeamableProduct/web)
  watch.add(safeRealpath(__dirname)); // this adapter's own source
  const notifDir = symlinkDir('@beamable/notifications-react-native', projectRoot);
  if (notifDir) watch.add(safeRealpath(notifDir));
  for (const extra of opts.watchFolders || []) watch.add(path.resolve(extra));
  if (opts.repoRoot) watch.add(path.resolve(opts.repoRoot));
  config.watchFolders = Array.from(watch);

  const nmPaths = new Set(resolver.nodeModulesPaths || []);
  nmPaths.add(path.join(projectRoot, 'node_modules'));
  nmPaths.add(path.join(safeRealpath(sdkSymlinkDir), 'node_modules'));
  resolver.nodeModulesPaths = Array.from(nmPaths);

  const prev = resolver.resolveRequest;
  resolver.resolveRequest = (context, moduleName, platform) => {
    if (moduleName === '@beamable/sdk') {
      return { type: 'sourceFile', filePath: beamMain };
    }
    if (moduleName === '@beamable/sdk/api') {
      return { type: 'sourceFile', filePath: beamApi };
    }
    return (prev || context.resolveRequest)(context, moduleName, platform);
  };

  return config;
}

module.exports = { withBeamableSdk };
