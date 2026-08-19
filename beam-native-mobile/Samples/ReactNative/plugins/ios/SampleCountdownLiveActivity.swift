import ActivityKit
import WidgetKit
import SwiftUI

// Sample WidgetKit extension that renders the `countdown` **Live Activity** — the no-tap,
// always-visible countdown on the Lock Screen and Dynamic Island (the iFood/Duolingo style).
//
// The shared `BeamCountdownActivityAttributes` type is compiled into THIS widget target too — the
// Beamable Expo plugin (`enableLiveActivity`) copies `CountdownLiveActivityAttributes.swift` from the
// package alongside this file. Do NOT import the RN pod here; the widget is its own module.
//
// A pure countdown needs no push updates: `Text(timerInterval:)` ticks on its own; at the deadline
// the Activity becomes STALE (we set `staleDate = expiresAt` when starting it), so `context.state.isExpired`
// flips true and WidgetKit re-renders the "expired" state — again with no push.
//
// Colors adapt to light/dark: the Lock-Screen card sets an explicit adaptive `activityBackgroundTint`
// with matching text colors (the system's default background is dark-ish and made near-black text
// unreadable). The Dynamic Island is always rendered on black, so it uses light text there.
// Widget target deployment is iOS 16.1, so no per-symbol `@available` gating is needed.

private func timerRange(to end: Date) -> ClosedRange<Date> {
    let now = Date()
    return now <= end ? now...end : end...end
}

// --- Adaptive palette ------------------------------------------------------
private func countdownAccent(_ scheme: ColorScheme) -> Color {
    scheme == .dark ? Color(red: 1.0, green: 0.62, blue: 0.20)    // warm amber on dark
                    : Color(red: 0.80, green: 0.30, blue: 0.00)   // deep orange on light
}
private func cardTint(_ scheme: ColorScheme) -> Color {
    scheme == .dark ? Color(white: 0.12) : Color(white: 0.97)
}
private func titleColor(_ scheme: ColorScheme) -> Color {
    scheme == .dark ? Color.white : Color.black
}
private func bodyColor(_ scheme: ColorScheme) -> Color {
    scheme == .dark ? Color(white: 0.78) : Color(white: 0.30)
}

/// The countdown line, or an "expired" label once the activity is stale (deadline reached).
private struct CountdownValue: View {
    let expiresAt: Date
    let isStale: Bool
    let font: Font
    var accent: Color? = nil
    @Environment(\.colorScheme) private var scheme

    var body: some View {
        if isStale {
            Text("Offer expired").font(font).foregroundStyle(.secondary)
        } else {
            Text(timerInterval: timerRange(to: expiresAt), countsDown: true)
                .font(font)
                .monospacedDigit()
                .foregroundStyle(accent ?? countdownAccent(scheme))
        }
    }
}

/// Lock-Screen / banner card. Uses an explicit adaptive background + text so it reads in both modes.
private struct LockScreenCard: View {
    let context: ActivityViewContext<BeamCountdownActivityAttributes>
    @Environment(\.colorScheme) private var scheme

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(context.attributes.title)
                .font(.headline)
                .foregroundStyle(titleColor(scheme))
            CountdownValue(
                expiresAt: context.state.expiresAt,
                isStale: context.state.isExpired,
                font: .system(size: 34, weight: .bold, design: .monospaced),
                accent: countdownAccent(scheme)
            )
            Text(context.state.isExpired ? "This offer has ended." : context.state.body)
                .font(.body)
                .foregroundStyle(bodyColor(scheme))
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding()
        .activityBackgroundTint(cardTint(scheme))
        .activitySystemActionForegroundColor(titleColor(scheme))
    }
}

struct SampleCountdownLiveActivity: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: BeamCountdownActivityAttributes.self) { context in
            LockScreenCard(context: context)
        } dynamicIsland: { context in
            // The Dynamic Island is always drawn on black — use light text + the dark-mode accent.
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    Text(context.attributes.title).font(.caption).foregroundStyle(.white).lineLimit(1)
                }
                DynamicIslandExpandedRegion(.trailing) {
                    CountdownValue(
                        expiresAt: context.state.expiresAt,
                        isStale: context.state.isExpired,
                        font: .caption,
                        accent: countdownAccent(.dark)
                    )
                    .frame(maxWidth: 80)
                }
                DynamicIslandExpandedRegion(.bottom) {
                    Text(context.state.isExpired ? "This offer has ended." : context.state.body)
                        .font(.caption2)
                        .foregroundStyle(.white.opacity(0.75))
                }
            } compactLeading: {
                Image(systemName: context.state.isExpired ? "timer.slash" : "timer")
                    .foregroundStyle(countdownAccent(.dark))
            } compactTrailing: {
                if context.state.isExpired {
                    Image(systemName: "checkmark").foregroundStyle(.white)
                } else {
                    Text(timerInterval: timerRange(to: context.state.expiresAt), countsDown: true)
                        .monospacedDigit()
                        .foregroundStyle(countdownAccent(.dark))
                        .frame(maxWidth: 44)
                }
            } minimal: {
                Image(systemName: context.state.isExpired ? "timer.slash" : "timer")
                    .foregroundStyle(countdownAccent(.dark))
            }
        }
    }
}

// The `@main WidgetBundle` that registers all of the sample's Live Activity widgets now lives in
// `SampleWidgetsBundle.swift` (a bundle can declare `@main` only once). This file defines only the
// countdown widget.
