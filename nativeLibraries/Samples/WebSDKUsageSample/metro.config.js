// Learn more: https://docs.expo.dev/guides/customizing-metro/
const { getDefaultConfig } = require('expo/metro-config');
const { withBeamableSdk } = require('@beamable/sdk-react-native/metro');

// withBeamableSdk resolves @beamable/sdk (+ /api) to the SDK's browser build and
// watches the external file: SDK source. On Windows (short `subst` drive) pass
// { repoRoot: process.env.BEAM_REPO_ROOT }.
module.exports = withBeamableSdk(getDefaultConfig(__dirname), {
  repoRoot: process.env.BEAM_REPO_ROOT,
});
