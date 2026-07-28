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
/// compiles) — the podspec globs it into the app and the Expo plugin copies it into the widget.
///
/// Note: `perform()` is available from iOS 16, but interactive `Button(intent:)` in a Live Activity
/// requires iOS 17 — so the WIDGET gates the button rendering, not this intent.
@available(iOS 16.0, *)
public struct BeamLiveActivityActionIntent: LiveActivityIntent {
    public static var title: LocalizedStringResource = "Beam Live Activity Action"

    /// The tapped button's `id` (matches `BeamLiveActivityButton.id`). `"dismiss"` ends the activity;
    /// anything else resolves it (clears the buttons, flips to the resolved card).
    @Parameter(title: "Action")
    public var actionId: String

    public init() {}
    public init(actionId: String) { self.actionId = actionId }

    public func perform() async throws -> some IntentResult {
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
        return .result()
    }
}
#endif
