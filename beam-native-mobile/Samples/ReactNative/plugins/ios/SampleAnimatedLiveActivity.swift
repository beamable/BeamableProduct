import ActivityKit
import WidgetKit
import SwiftUI

// Sample WidgetKit extension that renders the "animated" **Live Activity** — the iOS parity for the
// Android `animated` style (colored panels that auto-cycle). iOS has no animated notification banner,
// so the cycling card is delivered as an always-visible Live Activity instead. Mirrors the Android
// wire keys `colors` (hex list) and `flipIntervalMs`.
//
// HONEST CONSTRAINT: SwiftUI in a Live Activity can't run a free timer, and the OS throttles
// Lock-Screen redraws to a small update budget — so a literal 900ms flip is NOT achievable on the
// Lock Screen. We highlight the "active" panel best-effort via `TimelineView(.periodic(...))`, which
// steps smoothly in the on-screen Dynamic Island but is throttled/near-static on the Lock Screen. A
// realm that needs true stepping should push `content-state.activeIndex` updates; when the state's
// `activeIndex` moves, we honor it. All panels are always shown as a row so the card is meaningful
// even when redraws are throttled. The shared `BeamAnimatedActivityAttributes` type is compiled into
// this widget target by the Beamable Expo plugin.

/// Parses a `#RRGGBB` / `#AARRGGBB` (or without `#`) hex string into a SwiftUI `Color`; nil on failure.
private func color(fromHex raw: String) -> Color? {
    var s = raw.trimmingCharacters(in: .whitespaces)
    if s.hasPrefix("#") { s.removeFirst() }
    guard let value = UInt64(s, radix: 16) else { return nil }
    let r, g, b, a: Double
    switch s.count {
    case 6:
        r = Double((value & 0xFF0000) >> 16) / 255
        g = Double((value & 0x00FF00) >> 8) / 255
        b = Double(value & 0x0000FF) / 255
        a = 1
    case 8:
        a = Double((value & 0xFF000000) >> 24) / 255
        r = Double((value & 0x00FF0000) >> 16) / 255
        g = Double((value & 0x0000FF00) >> 8) / 255
        b = Double(value & 0x000000FF) / 255
    default:
        return nil
    }
    return Color(.sRGB, red: r, green: g, blue: b, opacity: a)
}

/// Fallback palette when the payload carries no valid colors (mirrors Android's default cycle).
private let animatedFallbackColors: [Color] = [
    Color(red: 0.20, green: 0.40, blue: 0.95),
    Color(red: 0.95, green: 0.35, blue: 0.30),
    Color(red: 0.15, green: 0.70, blue: 0.45),
    Color(red: 0.95, green: 0.62, blue: 0.15),
]

/// A row of colored panels; the "active" one is emphasized (scaled + full opacity, others dimmed).
private struct PanelRow: View {
    let colors: [Color]
    let activeIndex: Int
    var body: some View {
        HStack(spacing: 6) {
            ForEach(Array(colors.enumerated()), id: \.offset) { index, panel in
                RoundedRectangle(cornerRadius: 8, style: .continuous)
                    .fill(panel)
                    .frame(height: 26)
                    .opacity(index == activeIndex ? 1.0 : 0.35)
                    .scaleEffect(index == activeIndex ? 1.0 : 0.94)
                    .animation(.easeInOut(duration: 0.25), value: activeIndex)
            }
        }
    }
}

/// Resolves the panel colors (payload hex → Color, falling back to the default palette).
private func resolveColors(_ hexes: [String]) -> [Color] {
    let parsed = hexes.compactMap(color(fromHex:))
    return parsed.isEmpty ? animatedFallbackColors : parsed
}

/// Computes the currently-highlighted panel: the server-driven `activeIndex` if it's non-zero,
/// otherwise a best-effort local step derived from the timeline `date` and `flipIntervalMs`.
private func activePanel(date: Date, count: Int, flipIntervalMs: Int, serverIndex: Int) -> Int {
    guard count > 0 else { return 0 }
    if serverIndex > 0 { return serverIndex % count }
    let interval = max(0.2, Double(flipIntervalMs) / 1000.0)
    let step = Int(date.timeIntervalSinceReferenceDate / interval)
    return ((step % count) + count) % count
}

private struct AnimatedLockScreenCard: View {
    let context: ActivityViewContext<BeamAnimatedActivityAttributes>
    @Environment(\.colorScheme) private var scheme

    var body: some View {
        let colors = resolveColors(context.state.colors)
        let interval = max(0.2, Double(context.state.flipIntervalMs) / 1000.0)
        VStack(alignment: .leading, spacing: 8) {
            Text(context.attributes.title)
                .font(.headline)
                .foregroundStyle(scheme == .dark ? Color.white : Color.black)
            TimelineView(.periodic(from: Date(), by: interval)) { timeline in
                PanelRow(
                    colors: colors,
                    activeIndex: activePanel(date: timeline.date, count: colors.count,
                                             flipIntervalMs: context.state.flipIntervalMs,
                                             serverIndex: context.state.activeIndex)
                )
            }
            Text(context.state.body)
                .font(.body)
                .foregroundStyle(scheme == .dark ? Color(white: 0.78) : Color(white: 0.30))
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding()
        .activityBackgroundTint(scheme == .dark ? Color(white: 0.12) : Color(white: 0.97))
        .activitySystemActionForegroundColor(scheme == .dark ? Color.white : Color.black)
    }
}

struct SampleAnimatedLiveActivity: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: BeamAnimatedActivityAttributes.self) { context in
            AnimatedLockScreenCard(context: context)
        } dynamicIsland: { context in
            let colors = resolveColors(context.state.colors)
            let interval = max(0.2, Double(context.state.flipIntervalMs) / 1000.0)
            return DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    Text(context.attributes.title).font(.caption).foregroundStyle(.white).lineLimit(1)
                }
                DynamicIslandExpandedRegion(.bottom) {
                    TimelineView(.periodic(from: Date(), by: interval)) { timeline in
                        PanelRow(
                            colors: colors,
                            activeIndex: activePanel(date: timeline.date, count: colors.count,
                                                     flipIntervalMs: context.state.flipIntervalMs,
                                                     serverIndex: context.state.activeIndex)
                        )
                    }
                }
            } compactLeading: {
                Circle().fill(colors.first ?? .white).frame(width: 12, height: 12)
            } compactTrailing: {
                Circle().fill(colors.count > 1 ? colors[1] : (colors.first ?? .white)).frame(width: 12, height: 12)
            } minimal: {
                Circle().fill(colors.first ?? .white).frame(width: 12, height: 12)
            }
        }
    }
}
