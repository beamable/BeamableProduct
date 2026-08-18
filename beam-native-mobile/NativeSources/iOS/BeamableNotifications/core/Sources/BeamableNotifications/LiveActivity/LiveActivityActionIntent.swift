import Foundation
#if canImport(AppIntents) && canImport(ActivityKit)
import AppIntents
import ActivityKit

/// The App Intent behind the "actions" Live Activity buttons (`BeamActionsActivityAttributes`).
///
/// A `LiveActivityIntent` runs its `perform()` in the **app's process**, which iOS launches in the
/// background if needed WITHOUT foregrounding the app — that's what lets a Claim/Dismiss button on the
/// Lock Screen mutate or end the Activity in place, no tap-through to the app. The type must be
/// compiled into BOTH the app target (to execute) and the Widget extension (so `Button(intent:)`
/// compiles). The Expo plugin copies THIS file into the widget target.
///
/// **This copy cannot serve the app target.** iOS dispatches a tap by looking the intent up in the
/// app bundle's `Metadata.appintents`, which `appintentsmetadataprocessor` builds from SOURCE in the
/// app target and from the `.appintents` bundles of its static-library dependencies. The core reaches
/// engines as a prebuilt xcframework (a static `.a` + `.swiftinterface`) that carries no AppIntents
/// metadata, so this copy is invisible to that lookup no matter that it is linked in. Each engine
/// plugin therefore compiles its OWN app-side copy from source; for React Native that is
/// `ReactNative/ios/BeamLiveActivityActionIntent.swift`, compiled into the
/// `BeamableNotificationsRN` pod. Both copies must keep the type name and the `actionId` parameter
/// name identical — AppIntents pairs the widget's constructed intent to the app's executable one by
/// unqualified type name. (Unity and Unreal have no app-side copy yet, so their Live Activity buttons
/// render but do nothing — same defect, still open.)
///
/// Note: `perform()` is available from iOS 16, but interactive `Button(intent:)` in a Live Activity
/// requires iOS 17 — so the WIDGET gates the button rendering, not this intent.
/// **Deliberately `internal`, not `public`.** The core ships to engines as an xcframework built with
/// library evolution, and `xcodebuild -create-xcframework` keeps only the TEXTUAL `.swiftinterface`
/// (it strips the binary `.swiftmodule`), so every consumer parses that interface. Swift cannot print
/// an AppIntents `@Parameter` wrapper into an interface — `actionId` round-trips into a call to
/// `IntentParameter.init()`, which AppIntents marks `@available(*, unavailable)` — so a PUBLIC intent
/// here makes the entire module unimportable ("error: 'init()' is unavailable"). Internal keeps this
/// code compiled into every app that links the core, while leaving it out of the interface.
/// Widget targets never depend on this copy: the Expo plugin's `LIVE_ACTIVITY_SHARED_SUBPATHS` copies
/// this file's SOURCE into the widget target, where it compiles as that module's own type — and
/// ActivityKit matches activities to widgets by unqualified type name, so the split is invisible.
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
        // runs there — but the widget target deploys at 16.1, so gate the 16.2 ActivityKit APIs.
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
