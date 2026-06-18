/**
 * Expo config plugin — Beamable Notifications (iOS native SDK).
 *
 * `expo prebuild` regenerates the native iOS project from scratch, wiping any
 * capability/target you add by hand in Xcode. This plugin re-applies, on every
 * prebuild, everything the `beamable-notifications` package needs on iOS:
 *
 *   1. Push Notifications      → `aps-environment` entitlement on the app.
 *   2. Background Modes         → `remote-notification` in UIBackgroundModes.
 *   3. App Group               → entitlement on BOTH the app and the NSE, so the
 *                                 app + extension can share closed-app analytics
 *                                 and delivery receipts (SharedConfig).
 *   4. BMNAppGroup             → Info.plist key (app + NSE) the SDK reads to find
 *                                 the App Group container.
 *   5. Notification Service Extension target — copies the SDK's NSE sources into
 *                                 the project and registers a new app-extension
 *                                 target that builds them (rich media + receipts).
 *
 * Config (app.json):
 *   ["./plugins/withBeamableNotifications", { "appGroup": "group.com.beamable.rnsample" }]
 *
 * The App Group id defaults to `group.<your bundle id>` and must match an App
 * Group you have enabled on your Apple Developer account / provisioning profile.
 *
 * NOTE: APNs entitlement here uses the `development` environment. Archive/release
 * builds need `production`; Xcode/EAS flips this for you when signing for release.
 */
const {
  withEntitlementsPlist,
  withInfoPlist,
  withXcodeProject,
  withDangerousMod,
} = require('@expo/config-plugins');
const fs = require('fs');
const path = require('path');

const NSE_TARGET_NAME = 'BeamableNotificationServiceExtension';
// Where the prebuilt SDK lives, relative to this project root.
const SDK_ROOT = path.resolve(
  __dirname,
  '../../ClaudeProjects/BeamableNotifications',
);
const NSE_SOURCE_DIR = path.join(SDK_ROOT, 'extension');
// The NSE compiles the core FROM SOURCE too — but only the extension-SAFE subset.
// NotificationManager.swift / RemotePush.swift use UIApplication (forbidden in an
// app extension), so they're excluded; the NSE plugins only need these two.
const CORE_SOURCE_DIR = path.join(SDK_ROOT, 'core/Sources/BeamableNotifications');
const CORE_NSE_FILES = ['Models.swift', 'SharedConfig.swift'];

function resolveAppGroup(config, props) {
  if (props && props.appGroup) return props.appGroup;
  const bundleId = config.ios && config.ios.bundleIdentifier;
  return `group.${bundleId || 'com.beamable.app'}`;
}

// --- 1 + 3: app-target entitlements (push + App Group) ----------------------
function withAppEntitlements(config, appGroup) {
  return withEntitlementsPlist(config, (cfg) => {
    cfg.modResults['aps-environment'] = 'development';
    const groups =
      cfg.modResults['com.apple.security.application-groups'] || [];
    if (!groups.includes(appGroup)) groups.push(appGroup);
    cfg.modResults['com.apple.security.application-groups'] = groups;
    return cfg;
  });
}

// --- 2 + 4: app-target Info.plist (background mode + BMNAppGroup) ------------
function withAppInfoPlist(config, appGroup) {
  return withInfoPlist(config, (cfg) => {
    const modes = cfg.modResults.UIBackgroundModes || [];
    if (!modes.includes('remote-notification')) {
      modes.push('remote-notification');
    }
    cfg.modResults.UIBackgroundModes = modes;
    cfg.modResults.BMNAppGroup = appGroup;
    return cfg;
  });
}

// --- 5a: copy the SDK's NSE sources + write the NSE Info.plist/entitlements --
function withNSEFiles(config, appGroup) {
  return withDangerousMod(config, [
    'ios',
    (cfg) => {
      const iosRoot = cfg.modRequest.platformProjectRoot;
      const nseDir = path.join(iosRoot, NSE_TARGET_NAME);
      fs.mkdirSync(nseDir, { recursive: true });

      const swiftFiles = [];

      // 1) Extension sources (NotificationService + ServicePlugins). They ship with
      //    `import BeamableNotifications`, but the core is compiled into THIS same
      //    module — so strip that import (an unresolved/self import otherwise).
      const collectExt = (dir) => {
        for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
          const full = path.join(dir, entry.name);
          if (entry.isDirectory()) collectExt(full);
          else if (entry.name.endsWith('.swift')) {
            const src = fs
              .readFileSync(full, 'utf8')
              .replace(/^[ \t]*import BeamableNotifications[ \t]*\r?\n/m, '');
            fs.writeFileSync(path.join(nseDir, entry.name), src);
            swiftFiles.push(entry.name);
          }
        }
      };
      collectExt(NSE_SOURCE_DIR);

      // 2) Extension-safe core subset (Models + SharedConfig). Foundation-only.
      for (const name of CORE_NSE_FILES) {
        fs.copyFileSync(path.join(CORE_SOURCE_DIR, name), path.join(nseDir, name));
        swiftFiles.push(name);
      }

      // NSE Info.plist — UNNotificationServiceExtension point + BMNAppGroup so the
      // extension reaches the same shared container as the app. CFBundlePackageType
      // = XPC! is REQUIRED for an app extension; without it the built .appex is an
      // invalid bundle and the simulator rejects install ("Invalid placeholder
      // attributes / Failed to create app extension placeholder").
      const infoPlist = `<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleDevelopmentRegion</key>
	<string>$(DEVELOPMENT_LANGUAGE)</string>
	<key>CFBundleDisplayName</key>
	<string>${NSE_TARGET_NAME}</string>
	<key>CFBundleExecutable</key>
	<string>$(EXECUTABLE_NAME)</string>
	<key>CFBundleIdentifier</key>
	<string>$(PRODUCT_BUNDLE_IDENTIFIER)</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleName</key>
	<string>$(PRODUCT_NAME)</string>
	<key>CFBundlePackageType</key>
	<string>XPC!</string>
	<key>CFBundleShortVersionString</key>
	<string>$(MARKETING_VERSION)</string>
	<key>CFBundleVersion</key>
	<string>$(CURRENT_PROJECT_VERSION)</string>
	<key>BMNAppGroup</key>
	<string>${appGroup}</string>
	<key>NSExtension</key>
	<dict>
		<key>NSExtensionPointIdentifier</key>
		<string>com.apple.usernotifications.service</string>
		<key>NSExtensionPrincipalClass</key>
		<string>$(PRODUCT_MODULE_NAME).NotificationService</string>
	</dict>
</dict>
</plist>
`;
      fs.writeFileSync(path.join(nseDir, 'Info.plist'), infoPlist);

      // NSE entitlements — App Group only (the extension doesn't register for push).
      const entitlements = `<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>com.apple.security.application-groups</key>
	<array>
		<string>${appGroup}</string>
	</array>
</dict>
</plist>
`;
      fs.writeFileSync(
        path.join(nseDir, `${NSE_TARGET_NAME}.entitlements`),
        entitlements,
      );

      cfg.modRequest._bmnSwiftFiles = swiftFiles; // hand off to the pbxproj mod
      return cfg;
    },
  ]);
}

// --- 5b: register the NSE target in the Xcode project -----------------------
function withNSETarget(config, appGroup) {
  return withXcodeProject(config, (cfg) => {
    const proj = cfg.modResults;
    const swiftFiles = cfg.modRequest._bmnSwiftFiles || [];

    // Idempotent: prebuild --clean starts fresh, but guard re-runs anyway.
    if (proj.pbxTargetByName(NSE_TARGET_NAME)) return cfg;

    const bundleId = cfg.ios && cfg.ios.bundleIdentifier;
    const nseBundleId = `${bundleId}.${NSE_TARGET_NAME}`;

    // Group holding the NSE files in the navigator.
    const groupFiles = [...swiftFiles, 'Info.plist', `${NSE_TARGET_NAME}.entitlements`];
    const pbxGroup = proj.addPbxGroup(groupFiles, NSE_TARGET_NAME, NSE_TARGET_NAME);

    // Attach the new group under the project's main group.
    const groups = proj.hash.project.objects.PBXGroup;
    Object.keys(groups).forEach((key) => {
      if (groups[key].name === undefined && groups[key].path === undefined && groups[key].isa !== 'PBXVariantGroup' && groups[key].children) {
        proj.addToPbxGroup(pbxGroup.uuid, key);
      }
    });

    // The app-extension target + its build phases.
    const target = proj.addTarget(NSE_TARGET_NAME, 'app_extension', NSE_TARGET_NAME, nseBundleId);
    proj.addBuildPhase(swiftFiles, 'PBXSourcesBuildPhase', 'Sources', target.uuid);
    proj.addBuildPhase([], 'PBXResourcesBuildPhase', 'Resources', target.uuid);
    proj.addBuildPhase([], 'PBXFrameworksBuildPhase', 'Frameworks', target.uuid);

    // Build settings for both Debug/Release configs of the new target.
    const configs = proj.pbxXCBuildConfigurationSection();
    for (const key in configs) {
      const buildSettings = configs[key].buildSettings;
      if (buildSettings && buildSettings.PRODUCT_NAME === `"${NSE_TARGET_NAME}"`) {
        buildSettings.PRODUCT_BUNDLE_IDENTIFIER = `"${nseBundleId}"`;
        buildSettings.INFOPLIST_FILE = `"${NSE_TARGET_NAME}/Info.plist"`;
        buildSettings.CODE_SIGN_ENTITLEMENTS = `"${NSE_TARGET_NAME}/${NSE_TARGET_NAME}.entitlements"`;
        buildSettings.IPHONEOS_DEPLOYMENT_TARGET = '14.0';
        buildSettings.SWIFT_VERSION = '5.0';
        buildSettings.TARGETED_DEVICE_FAMILY = '"1,2"';
        buildSettings.CODE_SIGN_STYLE = 'Automatic';
        // We ship a complete Info.plist — don't let Xcode synthesize/merge one.
        buildSettings.GENERATE_INFOPLIST_FILE = 'NO';
        // Pin versions so the Info.plist's $(MARKETING_VERSION)/$(CURRENT_PROJECT_VERSION)
        // resolve (an empty CFBundleShortVersionString is rejected at install).
        buildSettings.MARKETING_VERSION = '1.0';
        buildSettings.CURRENT_PROJECT_VERSION = '1';
        buildSettings.SWIFT_OPTIMIZATION_LEVEL =
          buildSettings.SWIFT_OPTIMIZATION_LEVEL || '"-Onone"';
      }
    }

    return cfg;
  });
}

module.exports = function withBeamableNotifications(config, props) {
  const appGroup = resolveAppGroup(config, props);
  config = withAppEntitlements(config, appGroup);
  config = withAppInfoPlist(config, appGroup);

  // The Notification Service Extension (rich media + closed-app analytics) is
  // opt-in: `{ "enableServiceExtension": true }`. It requires a physical device
  // + APNs to exercise, and compiling the core into the extension needs an
  // extension-API-safe core build — so it's off by default to keep the app
  // building out of the box. Local + remote-registration notifications work
  // without it.
  if (props && props.enableServiceExtension) {
    config = withNSEFiles(config, appGroup);
    config = withNSETarget(config, appGroup);
  }
  return config;
};
