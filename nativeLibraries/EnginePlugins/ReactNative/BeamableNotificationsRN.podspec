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

  # Decision Q2: consume the Swift core as a PREBUILT BINARY (the same
  # BeamableNotifications.xcframework Unity/Unreal link) instead of compiling a
  # vendored copy of the core Swift sources. The xcframework is dropped in by tooling
  # (dev-native.sh copies it to ios/BeamableNotifications.xcframework); it is gitignored,
  # see ios/.gitignore + ios/README.md.
  s.vendored_frameworks = "ios/BeamableNotifications.xcframework"

  # Only the RN bridge sources are compiled here now (the bridge talks to the
  # xcframework's public Swift API). There is NO ios/core/ mirror anymore, so this is a
  # single-level glob, not "ios/**".
  s.source_files = "ios/*.{h,m,mm,swift}"

  s.dependency "React-Core"
end
