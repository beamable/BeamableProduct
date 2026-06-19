// React Native autolinking config for the Beamable Android notifications package.
//
// The native bridges (com.beamable.push.react.ReactPushModule →"BeamablePush",
// com.beamable.deeplink.react.ReactDeepLinkModule →"BeamableDeeplink") ship inside
// the prebuilt `.aar` (android/libs/beamable-notifications-release.aar). RN Android
// autolinking only takes ONE package per dependency, so the local `android/` module
// exposes a single aggregator — BeamableNotificationsPackage — that registers both.
//
// ios: null — this package is Android-only; iOS uses `beamable-notifications-ios`.
module.exports = {
  dependency: {
    platforms: {
      android: {
        sourceDir: './android',
        packageImportPath: 'import com.beamable.reactnative.BeamableNotificationsPackage;',
        packageInstance: 'new BeamableNotificationsPackage()',
      },
      ios: null,
    },
  },
};
