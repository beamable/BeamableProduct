import Foundation
#if canImport(ActivityKit) && canImport(WidgetKit) && canImport(SwiftUI)
import ActivityKit
import WidgetKit
import SwiftUI

// Default widget UI for the `actions` Live Activity — the SDK's out-of-the-box rendering of the
// console's "Action Buttons" style.
//
// WHY THIS IS A TEMPLATE, NOT LIBRARY CODE: a Live Activity's UI must be compiled INTO the app's
// WidgetKit extension target (`ActivityConfiguration` is resolved by the widget process), so it cannot
// ship inside the core framework the app links. The Expo config plugin stages this file into the widget
// target — but ONLY when the app supplies no `iosLiveActivityWidgets` of its own, because two `@main`
// entry points in one target will not compile. Supply your own widget file to replace it wholesale;
// `Samples/ReactNative/plugins/ios/SampleActionsLiveActivity.swift` is a worked example of that.
//
// Everything it renders comes from `BeamActionsActivityAttributes` (staged alongside), so the labels
// are the ones the campaign author typed — the same ones the notification fallback shows.

@available(iOS 16.1, *)
struct BeamActionsLiveActivityView: View {
    let context: ActivityViewContext<BeamActionsActivityAttributes>

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(context.attributes.title)
                .font(.caption)
                .foregroundStyle(.secondary)
            Text(context.state.headline)
                .font(.headline)
            if !context.state.body.isEmpty {
                Text(context.state.body)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
            // A resolved activity has had its buttons cleared by the update push (or by
            // BeamLiveActivityActionIntent), so this row simply disappears once the offer is claimed.
            if !context.state.buttons.isEmpty {
                HStack(spacing: 8) {
                    ForEach(context.state.buttons, id: \.id) { button in
                        BeamActionsLiveActivityButton(button: button)
                    }
                }
                .padding(.top, 2)
            }
        }
        .padding()
        .activityBackgroundTint(Color.black.opacity(0.35))
        .activitySystemActionForegroundColor(Color.white)
    }
}

/// One button. Interactive `Button(intent:)` inside a Live Activity requires iOS 17, so below that the
/// same label renders as static text rather than vanishing — the player still sees what the campaign
/// offered, they just have to open the app.
@available(iOS 16.1, *)
struct BeamActionsLiveActivityButton: View {
    let button: BeamActionButton

    var body: some View {
        if #available(iOS 17.0, *) {
            Button(intent: BeamLiveActivityActionIntent(actionId: button.id)) {
                Text(button.title)
                    .font(.caption).bold()
                    .padding(.horizontal, 10).padding(.vertical, 6)
            }
            .buttonStyle(.bordered)
            .tint(button.isDestructive ? .red : .accentColor)
        } else {
            Text(button.title)
                .font(.caption).bold()
                .padding(.horizontal, 10).padding(.vertical, 6)
                .overlay(Capsule().stroke(.secondary))
        }
    }
}

@available(iOS 16.1, *)
struct BeamActionsLiveActivity: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: BeamActionsActivityAttributes.self) { context in
            BeamActionsLiveActivityView(context: context)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    Text(context.state.headline).font(.caption).bold()
                }
                DynamicIslandExpandedRegion(.trailing) {
                    // The expanded region fits two controls; the console caps authored buttons at two
                    // for exactly this reason, and `prefix` keeps a custom style from overflowing.
                    HStack(spacing: 6) {
                        ForEach(context.state.buttons.prefix(2), id: \.id) { button in
                            BeamActionsLiveActivityButton(button: button)
                        }
                    }
                }
                DynamicIslandExpandedRegion(.bottom) {
                    if !context.state.body.isEmpty {
                        Text(context.state.body).font(.caption2).foregroundStyle(.secondary)
                    }
                }
            } compactLeading: {
                Image(systemName: "bell.badge")
            } compactTrailing: {
                Text(context.state.isResolved ? "✓" : "•")
            } minimal: {
                Image(systemName: "bell.badge")
            }
        }
    }
}

/// The widget target's entry point. Only staged when the app provides no widget of its own — a second
/// `@main` in the same target is a compile error.
@available(iOS 16.1, *)
@main
struct BeamableNotificationWidgetsBundle: WidgetBundle {
    var body: some Widget {
        BeamActionsLiveActivity()
    }
}
#endif
