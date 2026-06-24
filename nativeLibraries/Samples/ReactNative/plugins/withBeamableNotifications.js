/**
 * Expo config plugin — Beamable Notifications (iOS + Android native SDK).
 *
 * `expo prebuild` regenerates the native projects from scratch, wiping anything
 * you add by hand. This plugin re-applies the native setup on every prebuild.
 *
 * Android: copies `BeamablePushReceivedHandler.java` into the app package and adds
 * the `com.beamable.push.notification_received_handler` manifest meta-data (see the
 * Android section near the bottom).
 *
 * iOS (wiping any capability/target you add by hand in Xcode), what the
 * `@beamable/notifications-react-native` package needs on iOS:
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
  withAndroidManifest,
} = require('@expo/config-plugins');
const fs = require('fs');
const path = require('path');

// Manifest meta-data key the push library reads to resolve the receive-time handler.
const RECEIVED_HANDLER_META = 'com.beamable.push.notification_received_handler';
const RECEIVED_HANDLER_CLASS = 'BeamablePushReceivedHandler';

const NSE_TARGET_NAME = 'BeamableNotificationServiceExtension';
// Where the prebuilt SDK lives, relative to this project root.
const SDK_ROOT = path.resolve(
  __dirname,
  '../../../iOS/BeamableNotifications',
);
const NSE_SOURCE_DIR = path.join(SDK_ROOT, 'extension');
// The NSE compiles the core FROM SOURCE too — but only the extension-SAFE subset.
// NotificationManager.swift / RemotePush.swift use UIApplication (forbidden in an
// app extension), so they're excluded; the NSE plugins only need these two.
const CORE_SOURCE_DIR = path.join(SDK_ROOT, 'core/Sources/BeamableNotifications');
// Extension-safe core subset the NSE compiles. All Foundation-only (no UIApplication),
// required by AnalyticsServicePlugin.swift to log delivery receipts and fire the funnel
// "Received" event: Models.swift (JSONValue/DeliveryReceipt/FunnelEvent/AuthConfig +
// bmnCampaignIntent), SharedConfig.swift (App Group store), and the funnel itself
// (BeamableAnalytics.makeEvent/emit). Listed by basename — `copyCoreFile`/`nseSwiftFileNames`
// resolve each one recursively under CORE_SOURCE_DIR (BeamableAnalytics.swift is nested in
// the Analytics/ subdir).
const CORE_NSE_FILES = ['Models.swift', 'SharedConfig.swift', 'BeamableAnalytics.swift'];

// Resolve a core source by basename anywhere under CORE_SOURCE_DIR (the subset spans
// the dir root and the Analytics/ subdir). Returns the absolute path or null if missing.
function findCoreFile(name) {
  const walk = (dir) => {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        const hit = walk(full);
        if (hit) return hit;
      } else if (entry.name === name) {
        return full;
      }
    }
    return null;
  };
  return walk(CORE_SOURCE_DIR);
}

// The exact set of .swift basenames the NSE target compiles: every extension
// source plus the extension-safe core subset. Derived straight from the SDK
// dirs (which exist at config-eval time, before any mod runs), so it can be
// shared by BOTH the file-copy mod and the Xcode-project mod without relying on
// cross-mod state — `cfg.modRequest` is recreated per mod, so anything stashed
// on it in one mod is gone by the next.
function nseSwiftFileNames() {
  const names = [];
  const walk = (dir) => {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (entry.name.endsWith('.swift')) names.push(entry.name);
    }
  };
  walk(NSE_SOURCE_DIR);
  return [...names, ...CORE_NSE_FILES];
}

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

      // 2) Extension-safe core subset (Models + SharedConfig + the funnel). Foundation-only.
      //    Resolved by basename so nested sources (Analytics/BeamableAnalytics.swift) are found.
      for (const name of CORE_NSE_FILES) {
        const src = findCoreFile(name);
        if (!src) throw new Error(`[withBeamableNotifications] core NSE source not found: ${name}`);
        fs.copyFileSync(src, path.join(nseDir, name));
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

      return cfg;
    },
  ]);
}

// --- Xcode 16/26: disable Explicitly Built Modules --------------------------
// Xcode 16+/iOS 26 SDK enables "Explicitly Built Modules" by default. Its
// Clang/Swift dependency scanner expects every CocoaPods/Expo module's
// .modulemap to already exist in DerivedData before it scans — but those module
// maps are produced by a later build phase. The result is a wall of
// "module map file ... not found" / "Clang dependency scanner failure" errors
// before any code compiles. Force the legacy implicit-module path instead.
//
// Two halves: the app project (app + NSE + project-level configs) here, and the
// Pods project via a Podfile post_install injection below.
function withDisableExplicitModulesApp(config) {
  return withXcodeProject(config, (cfg) => {
    const proj = cfg.modResults;
    const configs = proj.pbxXCBuildConfigurationSection();
    for (const key in configs) {
      const bs = configs[key] && configs[key].buildSettings;
      if (!bs) continue; // skip the *_comment string entries
      bs.CLANG_ENABLE_EXPLICIT_MODULES = 'NO';
      bs.SWIFT_ENABLE_EXPLICIT_MODULES = 'NO';
    }
    return cfg;
  });
}

function withDisableExplicitModulesPods(config) {
  return withDangerousMod(config, [
    'ios',
    (cfg) => {
      const podfile = path.join(cfg.modRequest.platformProjectRoot, 'Podfile');
      let contents = fs.readFileSync(podfile, 'utf8');
      if (!contents.includes('CLANG_ENABLE_EXPLICIT_MODULES')) {
        const snippet =
          "\n    installer.pods_project.targets.each do |bmn_target|\n" +
          "      bmn_target.build_configurations.each do |bmn_config|\n" +
          "        bmn_config.build_settings['CLANG_ENABLE_EXPLICIT_MODULES'] = 'NO'\n" +
          "        bmn_config.build_settings['SWIFT_ENABLE_EXPLICIT_MODULES'] = 'NO'\n" +
          "      end\n" +
          "    end\n";
        // Inject at the top of the existing post_install block.
        contents = contents.replace(
          /post_install do \|installer\|\n/,
          (m) => m + snippet,
        );
        fs.writeFileSync(podfile, contents);
      }
      return cfg;
    },
  ]);
}

// --- 5b: register the NSE target in the Xcode project -----------------------
function withNSETarget(config, appGroup, swiftFiles) {
  return withXcodeProject(config, (cfg) => {
    const proj = cfg.modResults;

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

// === Android ================================================================
//
// `expo prebuild` regenerates `android/` from scratch, so the receive-time handler must be
// (re)applied as a plugin. The FCM service + POST_NOTIFICATIONS permission come from the
// `.aar`'s manifest merge; the deep-link VIEW intent-filter comes from Expo's `scheme`;
// google-services.json is wired via `expo.android.googleServicesFile` in app.json. The two
// things left for us:
//   1. Copy the native `BeamablePushReceivedHandler.java` into the app's package, and
//   2. Register it via the `com.beamable.push.notification_received_handler` meta-data.
// Both derive the package from `android.package` so they stay in sync if you rename it.

function withReceivedHandlerSource(config) {
  return withDangerousMod(config, [
    'android',
    (cfg) => {
      const pkg = (cfg.android && cfg.android.package) || 'com.beamable.rnsample';
      const srcFile = path.join(__dirname, 'android', `${RECEIVED_HANDLER_CLASS}.java`);
      const destDir = path.join(
        cfg.modRequest.platformProjectRoot,
        'app',
        'src',
        'main',
        'java',
        ...pkg.split('.'),
      );
      fs.mkdirSync(destDir, { recursive: true });
      // Rewrite the package declaration so the file always matches `android.package`.
      const src = fs
        .readFileSync(srcFile, 'utf8')
        .replace(/^package\s+[^;]+;/m, `package ${pkg};`);
      fs.writeFileSync(path.join(destDir, `${RECEIVED_HANDLER_CLASS}.java`), src);
      return cfg;
    },
  ]);
}

function withReceivedHandlerManifest(config) {
  return withAndroidManifest(config, (cfg) => {
    const pkg = (cfg.android && cfg.android.package) || 'com.beamable.rnsample';
    const app = cfg.modResults.manifest.application && cfg.modResults.manifest.application[0];
    if (app) {
      app['meta-data'] = app['meta-data'] || [];
      const exists = app['meta-data'].some(
        (m) => m.$ && m.$['android:name'] === RECEIVED_HANDLER_META,
      );
      if (!exists) {
        app['meta-data'].push({
          $: {
            'android:name': RECEIVED_HANDLER_META,
            'android:value': `${pkg}.${RECEIVED_HANDLER_CLASS}`,
          },
        });
      }
    }
    return cfg;
  });
}

module.exports = function withBeamableNotifications(config, props) {
  const appGroup = resolveAppGroup(config, props);
  config = withAppEntitlements(config, appGroup);
  config = withAppInfoPlist(config, appGroup);

  // Xcode 16+/iOS 26: disable Explicitly Built Modules (see notes above). Applies
  // to every iOS build, NSE or not — the base app's Expo/RN pods hit it too.
  config = withDisableExplicitModulesApp(config);
  config = withDisableExplicitModulesPods(config);

  // Android: register the receive-time handler (runs even when the app is killed).
  config = withReceivedHandlerSource(config);
  config = withReceivedHandlerManifest(config);

  // The Notification Service Extension (rich media + closed-app analytics) is
  // opt-in: `{ "enableServiceExtension": true }`. It requires a physical device
  // + APNs to exercise, and compiling the core into the extension needs an
  // extension-API-safe core build — so it's off by default to keep the app
  // building out of the box. Local + remote-registration notifications work
  // without it.
  if (props && props.enableServiceExtension) {
    const swiftFiles = nseSwiftFileNames();
    config = withNSEFiles(config, appGroup);
    config = withNSETarget(config, appGroup, swiftFiles);
  }
  return config;
};
