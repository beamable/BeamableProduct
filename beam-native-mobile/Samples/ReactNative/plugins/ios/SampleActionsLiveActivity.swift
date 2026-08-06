import ActivityKit
import WidgetKit
import SwiftUI
#if canImport(AppIntents)
import AppIntents
#endif

// Sample WidgetKit extension that renders the "actions" **Live Activity** — the always-visible,
// no-tap Lock-Screen / Dynamic-Island card with interactive buttons. This is the iOS answer to
// "action buttons without tap-and-hold": a native `actions` push notification can only reveal its
// buttons when the user expands it (OS rule), whereas a Live Activity shows them persistently.
//
// Buttons use `Button(intent: BeamLiveActivityActionIntent(...))` so a tap runs in the app's
// background process (no foregrounding) to update/end the Activity. Interactive buttons in a Live
// Activity require iOS 17 — on 16.1–16.4 we fall back to a non-interactive label so the card still
// renders. The shared `BeamActionsActivityAttributes` + `BeamLiveActivityActionIntent` types are
// compiled into this widget target by the Beamable Expo plugin.
//
// Colors adapt to light/dark exactly like the countdown widget: the Lock-Screen card sets an explicit
// adaptive `activityBackgroundTint`; the Dynamic Island is always drawn on black so it uses light text.

// --- Adaptive palette (mirrors SampleCountdownLiveActivity) ----------------
private func actionsCardTint(_ scheme: ColorScheme) -> Color {
    scheme == .dark ? Color(white: 0.12) : Color(white: 0.97)
}
private func actionsTitleColor(_ scheme: ColorScheme) -> Color {
    scheme == .dark ? Color.white : Color.black
}
private func actionsBodyColor(_ scheme: ColorScheme) -> Color {
    scheme == .dark ? Color(white: 0.78) : Color(white: 0.30)
}

/// A single action button. Interactive (`Button(intent:)`) on iOS 17+, otherwise a static pill so the
/// card still reads on iOS 16.1–16.4.
private struct ActionButtonView: View {
    let button: BeamLiveActivityButton
    var body: some View {
        let isDestructive = button.role == "destructive"
        #if canImport(AppIntents)
        if #available(iOS 17.0, *) {
            Button(intent: BeamLiveActivityActionIntent(actionId: button.id)) {
                Text(button.title).font(.callout.weight(.semibold)).frame(maxWidth: .infinity)
            }
            .tint(isDestructive ? .red : .accentColor)
            .buttonStyle(.borderedProminent)
        } else {
            fallbackLabel(isDestructive)
        }
        #else
        fallbackLabel(isDestructive)
        #endif
    }

    private func fallbackLabel(_ isDestructive: Bool) -> some View {
        Text(button.title)
            .font(.callout.weight(.semibold))
            .frame(maxWidth: .infinity)
            .padding(.vertical, 8)
            .background((isDestructive ? Color.red : Color.accentColor).opacity(0.9))
            .foregroundStyle(.white)
            .clipShape(RoundedRectangle(cornerRadius: 10, style: .continuous))
    }
}

/// Lock-Screen / banner card: title + headline + body, then the buttons (or a "resolved" line).
private struct ActionsLockScreenCard: View {
    let context: ActivityViewContext<BeamActionsActivityAttributes>
    @Environment(\.colorScheme) private var scheme

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(context.attributes.title)
                .font(.headline)
                .foregroundStyle(actionsTitleColor(scheme))
            Text(context.state.headline)
                .font(.subheadline.weight(.semibold))
                .foregroundStyle(actionsTitleColor(scheme))
            Text(context.state.body)
                .font(.body)
                .foregroundStyle(actionsBodyColor(scheme))
            if context.state.isResolved || context.state.buttons.isEmpty {
                Text(context.state.isResolved ? "Done" : "")
                    .font(.footnote)
                    .foregroundStyle(.secondary)
            } else {
                HStack(spacing: 10) {
                    ForEach(context.state.buttons, id: \.id) { ActionButtonView(button: $0) }
                }
                .padding(.top, 2)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding()
        .activityBackgroundTint(actionsCardTint(scheme))
        .activitySystemActionForegroundColor(actionsTitleColor(scheme))
    }
}

struct SampleActionsLiveActivity: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: BeamActionsActivityAttributes.self) { context in
            ActionsLockScreenCard(context: context)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    Text(context.attributes.title).font(.caption).foregroundStyle(.white).lineLimit(1)
                }
                DynamicIslandExpandedRegion(.trailing) {
                    Text(context.state.headline).font(.caption).foregroundStyle(.white.opacity(0.9)).lineLimit(1)
                }
                DynamicIslandExpandedRegion(.bottom) {
                    if context.state.isResolved || context.state.buttons.isEmpty {
                        Text(context.state.body).font(.caption2).foregroundStyle(.white.opacity(0.75))
                    } else {
                        HStack(spacing: 8) {
                            ForEach(context.state.buttons.prefix(2), id: \.id) { ActionButtonView(button: $0) }
                        }
                    }
                }
            } compactLeading: {
                Image(systemName: "bell.badge").foregroundStyle(.white)
            } compactTrailing: {
                Image(systemName: context.state.isResolved ? "checkmark" : "hand.tap").foregroundStyle(.white)
            } minimal: {
                Image(systemName: context.state.isResolved ? "checkmark" : "bell.badge").foregroundStyle(.white)
            }
        }
    }
}
