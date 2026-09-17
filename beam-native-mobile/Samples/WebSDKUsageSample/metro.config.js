// Learn more: https://docs.expo.dev/guides/customizing-metro/
const { getDefaultConfig } = require('expo/metro-config');
const { withBeamableSdk } = require('@beamable/notifications-react-native/metro');

// withBeamableSdk enables package-exports resolution (so Metro selects @beamable/sdk's
// native react-native build) and watches the external file: SDK source. On Windows
// (short `subst` drive) pass { repoRoot: process.env.BEAM_REPO_ROOT }.
module.exports = withBeamableSdk(getDefaultConfig(__dirname), {
  repoRoot: process.env.BEAM_REPO_ROOT,
});
