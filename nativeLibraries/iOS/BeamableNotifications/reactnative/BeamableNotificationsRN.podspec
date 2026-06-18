require "json"

package = JSON.parse(File.read(File.join(__dir__, "package.json")))

Pod::Spec.new do |s|
  # Distinct from the core Swift module name ("BeamableNotifications") so the two
  # never collide; RN autolinking installs this pod by this name.
  s.name         = "BeamableNotificationsRN"
  s.version      = package["version"]
  s.summary      = package["description"]
  s.homepage     = "https://beamable.com"
  s.license      = { :type => "MIT" }
  s.author       = { "Beamable" => "support@beamable.com" }
  s.platform     = :ios, "14.0"
  s.source       = { :path => "." }
  s.swift_version = "5.7"

  # RN compiles the Swift core FROM SOURCE into this pod (rather than linking the
  # prebuilt xcframework Unity/Unreal use). The bridge + core then share one Swift
  # module, sidestepping the Swift-module-distribution friction of vendoring a
  # binary framework through CocoaPods.
  #
  # CocoaPods sandboxes source_files to the pod root, so the core sources are
  # mirrored under ios/core/ (kept in sync from ../core/Sources via
  # scripts/sync-rn-core.sh). The single glob below picks up both the bridge
  # (ios/*.swift) and the mirrored core (ios/core/**/*.swift).
  s.source_files = "ios/**/*.{h,m,mm,swift}"

  s.dependency "React-Core"
end
