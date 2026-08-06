// swift-tools-version:5.7
import PackageDescription

let package = Package(
    name: "BeamableNotifications",
    platforms: [
        .iOS(.v14)
    ],
    products: [
        .library(
            name: "BeamableNotifications",
            type: .static,
            targets: ["BeamableNotifications"]
        )
    ],
    targets: [
        .target(
            name: "BeamableNotifications",
            path: "Sources/BeamableNotifications"
        ),
        .testTarget(
            name: "BeamableNotificationsTests",
            dependencies: ["BeamableNotifications"],
            path: "Tests/BeamableNotificationsTests"
        )
    ]
)
