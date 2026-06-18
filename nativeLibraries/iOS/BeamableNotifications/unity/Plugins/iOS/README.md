# Native binaries

Place the built `BeamableNotifications.xcframework` here (this folder, `Plugins/iOS/`).
Unity auto-links `.xcframework` bundles found under `Plugins/iOS` into the generated
Xcode project — no manual linking needed.

The Notification Service Extension sources live in `Plugins/iOS~/Extension/` (the `~`
prevents Unity from importing the `.swift` files as scripts). The editor post-build step
(`Editor/NotificationsPostProcess.cs`) copies them into a new NSE target automatically.

Build the xcframework with `scripts/build-xcframework.sh` from the repo root.
