// Learn more: https://docs.expo.dev/guides/customizing-metro/
const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const projectRoot = __dirname;

// The repo root (BeamableProduct), three levels up from this project.
//
// On Windows the repo's deep path overflows the 260-char MAX_PATH limit during the
// New-Architecture C++ build, so the app is built from a short `subst` drive (e.g.
// `subst B: <this folder>`). A drive root can't resolve `../../..` to the real
// siblings (the web SDK + native modules live OUTSIDE this folder), so allow the real
// repo root to be passed explicitly via BEAM_REPO_ROOT; otherwise use the relative path.
const repoRoot = process.env.BEAM_REPO_ROOT
  ? path.resolve(process.env.BEAM_REPO_ROOT)
  : path.resolve(projectRoot, '../../..');

// The Beamable Web SDK lives OUTSIDE this project (a `file:` dependency) at
// BeamableProduct/web. Metro only watches the project root by default, so add the
// SDK folder to watchFolders and to the module resolution paths.
const beamSdkRoot = path.resolve(repoRoot, 'web');

// Resolve the SDK's ESM *browser* build explicitly. Its package `exports` list
// `require` (an IIFE global-script build) before `import`, so condition-based
// resolution would otherwise pick the wrong, non-importable file. The browser
// build is fetch-based and works in RN together with src/polyfills.ts.
// Resolve through the node_modules symlink (`@beamable/sdk` is a `file:` dep → symlinked
// to the web SDK). Metro indexes files under this in-tree path; pointing at the symlink's
// real out-of-tree target instead makes Metro's SHA-1 lookup miss (the file map keys the
// crawled file under the node_modules path).
const beamPkg = path.resolve(projectRoot, 'node_modules/@beamable/sdk');
const beamMain = path.resolve(beamPkg, 'dist/browser/index.mjs');
const beamApi = path.resolve(beamPkg, 'dist/api.mjs');

// The unified Beamable Notifications RN package is also a `file:` dependency living outside
// the project root (under nativeLibraries/EnginePlugins/ReactNative). npm symlinks it into
// node_modules, but its real source is here — Metro must watch it to resolve
// `@beamable/notifications-react-native`.
const beamNotificationsRoot = path.resolve(
  repoRoot,
  'nativeLibraries/EnginePlugins/ReactNative',
);

const config = getDefaultConfig(projectRoot);

// The C# microservice + its portal extensions live under this project dir but are NOT part of
// the RN bundle. Their nested node_modules carry platform-specific optional binaries (e.g.
// rollup's linux-riscv64-musl build) that aren't installed on Windows; Metro's fallback watcher
// (used because Watchman is disabled) crashes trying to `fs.watch` those dangling entries. Exclude
// the whole subtree from the file map + watcher — the app never imports from it.
const defaultBlockList = config.resolver.blockList;
config.resolver.blockList = [
  ...(Array.isArray(defaultBlockList)
    ? defaultBlockList
    : defaultBlockList
      ? [defaultBlockList]
      : []),
  /[\\/]Microservices[\\/]/,
];

// Watchman isn't required and, when it's absent on Windows, Metro's fallback only
// reliably crawls the projectRoot — leaving the external watchFolders below (the web
// SDK in particular) out of the file map, which breaks SHA-1 lookups for the SDK's
// browser build. Force Metro's Node crawler so every watchFolder is indexed.
config.resolver.useWatchman = false;

// Keep the projectRoot AND the external `file:`/SDK roots watched (these live outside
// the project root). Replacing the list drops the default projectRoot entry, so re-add it.
config.watchFolders = [
  projectRoot,
  beamSdkRoot,
  beamNotificationsRoot,
];

config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, 'node_modules'),
  path.resolve(beamSdkRoot, 'node_modules'),
];

config.resolver.resolveRequest = (context, moduleName, platform) => {
  if (moduleName === '@beamable/sdk') {
    return { type: 'sourceFile', filePath: beamMain };
  }
  if (moduleName === '@beamable/sdk/api') {
    return { type: 'sourceFile', filePath: beamApi };
  }
  return context.resolveRequest(context, moduleName, platform);
};

module.exports = config;
