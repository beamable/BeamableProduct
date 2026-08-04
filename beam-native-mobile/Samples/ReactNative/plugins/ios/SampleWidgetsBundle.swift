import WidgetKit
import SwiftUI

/// The single `@main` entry point for the sample's Widget extension. A WidgetKit extension may
/// declare `@main` exactly once, so ALL of the sample's Live Activity widgets are registered here:
///   • `SampleCountdownLiveActivity`  — countdown offer card (SampleCountdownLiveActivity.swift)
///   • `SampleActionsLiveActivity`    — always-visible action buttons (SampleActionsLiveActivity.swift)
///   • `SampleAnimatedLiveActivity`   — cycling color panels (SampleAnimatedLiveActivity.swift)
///
/// The Expo config plugin (`enableLiveActivity`) copies this file plus each widget file and the
/// shared `Beam*ActivityAttributes` / `BeamLiveActivityActionIntent` types into the widget target.
@main
struct BeamableSampleWidgetsBundle: WidgetBundle {
    var body: some Widget {
        SampleCountdownLiveActivity()
        SampleActionsLiveActivity()
        SampleAnimatedLiveActivity()
    }
}
