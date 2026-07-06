/**
 * @beamable/sdk-react-native — React Native adapter for the Beamable Web SDK.
 *
 * Three pieces make `@beamable/sdk` run under React Native:
 *   1. `import '@beamable/sdk-react-native/polyfills'` — install the browser
 *      globals the SDK's browser build assumes (before importing the SDK).
 *   2. `RNTokenStorage` (below) — an AsyncStorage-backed `TokenStorage`, passed to
 *      `Beam.init({ tokenStorage })`.
 *   3. `withBeamableSdk` (from `@beamable/sdk-react-native/metro`) — wires Metro to
 *      resolve the SDK's browser build. Used in `metro.config.js`.
 *
 * The one thing this package can't apply for you is the babel transform for the
 * SDK's ES2022 static blocks — add `@babel/plugin-transform-class-static-block`
 * to your `babel.config.js`. See this package's README.
 */
export { RNTokenStorage } from './RNTokenStorage';
