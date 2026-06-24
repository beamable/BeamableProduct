// React Native autolinking config for the unified Beamable notifications package
// (@beamable/notifications-react-native).
//
// Android: the native bridges (com.beamable.push.react.ReactPushModule → "BeamablePush",
//   com.beamable.deeplink.react.ReactDeepLinkModule → "BeamableDeeplink") ship inside the
//   prebuilt `.aar` (android/libs/beamable-notifications-release.aar). RN Android autolinking
//   takes ONE package per dependency, so the local `android/` module exposes a single
//   aggregator — BeamableNotificationsPackage — that registers both. The Kotlin FQN below is
//   stable (do not change it without updating the gradle module).
//
// iOS: autolinking discovers the CocoaPods podspec (BeamableNotificationsRN.podspec) in the
//   package root automatically; the empty `{}` opts iOS in (the package is no longer
//   Android-only). The pod vendors BeamableNotifications.xcframework — see the podspec.
module.exports = {
  dependency: {
    platforms: {
      android: {
        sourceDir: './android',
        packageImportPath:
          'import com.beamable.reactnative.BeamableNotificationsPackage;',
        packageInstance: 'new BeamableNotificationsPackage()',
      },
      ios: {},
    },
  },
};
