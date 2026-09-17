# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0]

Initial release of `com.beamable.notifications.web`, which lets a web or React UI running inside a
Unity WebView drive the Beamable native notifications binary. No dependency on the Beamable Unity
SDK — the page uses the Beamable Web SDK, and this is a thin JSON relay.

### Added

- `BeamableWebViewBridge` — a verbatim JSON relay giving the page real APNs/FCM tokens, the OS
  permission prompt, local notifications, deep links, and closed-app analytics. Configure it with one
  delegate (`evaluateJs`) and feed it with one method (`OnPageMessage`); there is no interface to
  implement and no MonoBehaviour to subclass.
- Shim helpers for bring-your-own WebView plugins: gree/unity-webview, Vuplex, and uniwebview.
- `StreamingAssetsServer` — serves a static web bundle from StreamingAssets over a real local http
  origin, so it works in the Editor and on device.
- Editor **Tools ▸ Beamable ▸ Web Bundle Exporter**, plus Android and iOS build processing and
  Beamable config resolution for the page.
- Prebuilt Android `.aar` and iOS `.xcframework` binaries ship in the package.

### Notes

- Auth and analytics are page-driven: the page forwards the Web SDK's tokens and `cid`/`pid`/`host`
  via `configureAuth`. There is intentionally no `trackOffer*` API on the Unity side.
