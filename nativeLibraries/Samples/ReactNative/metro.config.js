// Learn more: https://docs.expo.dev/guides/customizing-metro/
const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const projectRoot = __dirname;

// The Beamable Web SDK lives OUTSIDE this project (a `file:` dependency).
// Metro only watches the project root by default, so add the SDK folder to
// watchFolders and to the module resolution paths.
//
// This project lives at BeamableProduct/nativeLibraries/Samples/ReactNative, so
// the Web SDK (BeamableProduct/web) is three levels up. Update if you move it.
const beamSdkRoot = path.resolve(projectRoot, '../../../web');

// Resolve the SDK's ESM *browser* build explicitly. Its package `exports` list
// `require` (an IIFE global-script build) before `import`, so condition-based
// resolution would otherwise pick the wrong, non-importable file. The browser
// build is fetch-based and works in RN together with src/polyfills.ts.
const beamMain = path.resolve(beamSdkRoot, 'dist/browser/index.mjs');
const beamApi = path.resolve(beamSdkRoot, 'dist/api.mjs');

// The Beamable Notifications native SDKs are also `file:` dependencies living outside the
// project root (under nativeLibraries/iOS|Android/BeamableNotifications). npm symlinks them
// into node_modules, but their real source is here — Metro must watch them to resolve
// `beamable-notifications-ios` / `beamable-notifications-android`.
const beamNotificationsIosRoot = path.resolve(
  projectRoot,
  '../../iOS/BeamableNotifications/reactnative',
);
const beamNotificationsAndroidRoot = path.resolve(
  projectRoot,
  '../../Android/BeamableNotifications/reactnative',
);

const config = getDefaultConfig(projectRoot);

config.watchFolders = [
  beamSdkRoot,
  beamNotificationsIosRoot,
  beamNotificationsAndroidRoot,
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
