import Foundation
#if canImport(AppIntents) && canImport(ActivityKit)
import AppIntents
import ActivityKit
import BeamableNotifications

/// The **app-process** half of the "actions" Live Activity buttons.
///
/// `LiveActivityIntent` is the one AppIntents protocol iOS performs in the APP's process rather than
/// in the extension that drew the button — it has to be, because only the process that owns an
/// Activity can enumerate `Activity<T>.activities` and update or end it. iOS finds the intent through
/// the app bundle's `Metadata.appintents`, which `appintentsmetadataprocessor` builds from SOURCE
/// compiled into the app target (and into the static-library dependencies whose own `.appintents`
/// bundles get merged into it).
///
/// That is why this file exists here instead of being inherited from the core. The core ships to
/// engines as a prebuilt `BeamableNotifications.xcframework` — a static `.a` plus a `.swiftinterface`,
/// carrying NO AppIntents metadata — so core's copy of this intent is invisible to the processor.
/// Before this file, the app's extraction step reported "Extracted no relevant App Intents symbols",
/// the widget was the only target that registered the intent, and every Claim/Dismiss tap dispatched
/// into a process that had never heard of it: the buttons rendered and did nothing.
///
/// This file is compiled into the `BeamableNotificationsRN` pod, which is statically linked into the
/// app and is already configured to `import BeamableNotifications` (the podspec's `SWIFT_INCLUDE_PATHS`
/// points at the xcframework's staged Headers). The pod's extracted metadata is merged into the app's
/// via the build's `--static-metadata-file-list`.
///
/// **Wire contract with the widget's copy.** The Expo config plugin copies core's
/// `LiveActivity/LiveActivityActionIntent.swift` into the Widget extension so `Button(intent:)`
/// compiles there. The widget's copy only ever CONSTRUCTS the intent; this copy is the one that runs.
/// AppIntents pairs them by unqualified type name, so these two must always agree on:
///   • the type name `BeamLiveActivityActionIntent`, and
///   • the `@Parameter` name `actionId`.
/// Change either one here and you must change it in
/// `nativeLibraries/iOS/BeamableNotifications/core/Sources/BeamableNotifications/LiveActivity/LiveActivityActionIntent.swift`
/// in the same commit, or the buttons go silent again.
@available(iOS 16.0, *)
struct BeamLiveActivityActionIntent: LiveActivityIntent {
    static var title: LocalizedStringResource = "Beam Live Activity Action"

    /// The tapped button's `id` (matches `BeamLiveActivityButton.id`). `"dismiss"` ends the activity;
    /// anything else resolves it (clears the buttons, flips to the resolved card).
    @Parameter(title: "Action")
    var actionId: String

    init() {}
    init(actionId: String) { self.actionId = actionId }

    func perform() async throws -> some IntentResult {
        // The interactive `Button(intent:)` that triggers this is iOS 17+, so `perform()` only ever
        // runs there — but this pod deploys lower, so gate the 16.2 ActivityKit APIs.
        if #available(iOS 16.2, *) {
            for activity in Activity<BeamActionsActivityAttributes>.activities {
                if actionId == "dismiss" {
                    await activity.end(nil, dismissalPolicy: .immediate)
                } else {
                    var state = activity.content.state
                    state.isResolved = true
                    state.headline = "Claimed"
                    state.buttons = []
                    await activity.update(ActivityContent(state: state, staleDate: nil))
                }
            }
        }
        return .result()
    }
}
#endif
