// Learn more: https://docs.expo.dev/guides/customizing-metro/
const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const projectRoot = __dirname;

// The Beamable Web SDK lives OUTSIDE this project (a `file:` dependency).
// Metro only watches the project root by default, so add the SDK folder to
// watchFolders and to the module resolution paths.
//
// NOTE: if you move this project, update this path (and the dependency path
// in package.json) to keep pointing at BeamableProduct/web.
const beamSdkRoot = path.resolve(projectRoot, '../BeamableProduct/web');

// Resolve the SDK's ESM *browser* build explicitly. Its package `exports` list
// `require` (an IIFE global-script build) before `import`, so condition-based
// resolution would otherwise pick the wrong, non-importable file. The browser
// build is fetch-based and works in RN together with src/polyfills.ts.
const beamMain = path.resolve(beamSdkRoot, 'dist/browser/index.mjs');
const beamApi = path.resolve(beamSdkRoot, 'dist/api.mjs');

// The Beamable Notifications native SDK is also a `file:` dependency living
// outside the project root. npm symlinks it into node_modules, but its real
// source is here — Metro must watch it to resolve `beamable-notifications`.
const beamNotificationsRoot = path.resolve(
  projectRoot,
  '../ClaudeProjects/BeamableNotifications/reactnative',
);

const config = getDefaultConfig(projectRoot);

config.watchFolders = [beamSdkRoot, beamNotificationsRoot];

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
