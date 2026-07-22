/**
 * Local Expo config plugin — sample-app CUSTOM notification styles (`animated` + `countdown`).
 *
 * Demonstrates how a consuming app extends the shared `@beamable/notifications-react-native`
 * library with notification styles the shared library does NOT build itself. The shared lib
 * discovers a customer-provided renderer by reflection from an AndroidManifest meta-data key
 * (`com.beamable.push.notification_style_renderer`) — EXACTLY like the receive-time handler
 * (`com.beamable.push.notification_received_handler`). This plugin mirrors the Android half of
 * `EnginePlugins/ReactNative/plugin/withBeamableNotifications.js`.
 *
 * `expo prebuild` regenerates `android/` from scratch, wiping anything placed by hand, so the
 * renderer + its resources must be re-applied on every prebuild via this plugin:
 *   1. Copy `plugins/android/SampleNotificationStyleRenderer.kt` into the app package (rewriting
 *      its `package` declaration to `config.android.package`), and copy the three res files
 *      (layout/drawable/values) into `app/src/main/res/`.
 *   2. Register the renderer via the `com.beamable.push.notification_style_renderer` meta-data.
 *
 * Register in app.json: add "./plugins/withSampleNotificationStyles" to `expo.plugins`.
 */
const fs = require('fs');
const path = require('path');

// Resolve `@expo/config-plugins` from the CONSUMING PROJECT, not relative to this file.
function requireConfigPlugins() {
  const candidates = [process.cwd(), __dirname];
  try {
    return require(require.resolve('@expo/config-plugins', { paths: candidates }));
  } catch {
    return require('@expo/config-plugins');
  }
}
const { withDangerousMod, withAndroidManifest } = requireConfigPlugins();

// Manifest meta-data key the push library reads to resolve the custom-style renderer
// (reflection — the class needs a public no-arg constructor).
const STYLE_RENDERER_META = 'com.beamable.push.notification_style_renderer';
const STYLE_RENDERER_CLASS = 'SampleNotificationStyleRenderer';

const DEFAULT_PACKAGE = 'com.beamable.rnsample';

// The three sample resources copied alongside the renderer. `dir` is the res subfolder; `name`
// is the destination filename (the values file is renamed to avoid colliding with any other
// values file, keeping the `beam_notif_cycle_*` color names intact).
const RES_FILES = [
  { src: path.join('res', 'layout', 'beam_notif_animated.xml'), dir: 'layout', name: 'beam_notif_animated.xml' },
  { src: path.join('res', 'layout', 'beam_notif_countdown.xml'), dir: 'layout', name: 'beam_notif_countdown.xml' },
  { src: path.join('res', 'drawable', 'beam_notif_panel_bg.xml'), dir: 'drawable', name: 'beam_notif_panel_bg.xml' },
  { src: path.join('res', 'values', 'beam_notif_colors.xml'), dir: 'values', name: 'beam_notif_colors.xml' },
];

// Exact-alarm permissions the countdown's "expired" swap needs to fire precisely at 0.
// USE_EXACT_ALARM is auto-granted on API 33+ (alarm/timer use case); SCHEDULE_EXACT_ALARM covers 31-32.
const EXACT_ALARM_PERMISSIONS = [
  'android.permission.USE_EXACT_ALARM',
  'android.permission.SCHEDULE_EXACT_ALARM',
];

// Copy the renderer .kt into the app package (rewriting its package line) and the res files.
function withSampleRendererSource(config) {
  return withDangerousMod(config, [
    'android',
    (cfg) => {
      const pkg = (cfg.android && cfg.android.package) || DEFAULT_PACKAGE;
      const androidRoot = path.join(cfg.modRequest.platformProjectRoot, 'app', 'src', 'main');

      // 1) Kotlin renderer -> app/src/main/java/<pkg-as-dirs>/, package line rewritten.
      const srcFile = path.join(__dirname, 'android', `${STYLE_RENDERER_CLASS}.kt`);
      const destDir = path.join(androidRoot, 'java', ...pkg.split('.'));
      fs.mkdirSync(destDir, { recursive: true });
      const src = fs
        .readFileSync(srcFile, 'utf8')
        .replace(/^package\s+.+$/m, `package ${pkg}`);
      fs.writeFileSync(path.join(destDir, `${STYLE_RENDERER_CLASS}.kt`), src);

      // 2) Resources -> app/src/main/res/{layout,drawable,values}/.
      for (const file of RES_FILES) {
        const from = path.join(__dirname, 'android', file.src);
        const toDir = path.join(androidRoot, 'res', file.dir);
        fs.mkdirSync(toDir, { recursive: true });
        fs.copyFileSync(from, path.join(toDir, file.name));
      }

      return cfg;
    },
  ]);
}

// Register the renderer via the notification_style_renderer meta-data (idempotent).
function withSampleRendererManifest(config) {
  return withAndroidManifest(config, (cfg) => {
    const pkg = (cfg.android && cfg.android.package) || DEFAULT_PACKAGE;
    const app = cfg.modResults.manifest.application && cfg.modResults.manifest.application[0];
    if (app) {
      app['meta-data'] = app['meta-data'] || [];
      const exists = app['meta-data'].some(
        (m) => m.$ && m.$['android:name'] === STYLE_RENDERER_META,
      );
      if (!exists) {
        app['meta-data'].push({
          $: {
            'android:name': STYLE_RENDERER_META,
            'android:value': `${pkg}.${STYLE_RENDERER_CLASS}`,
          },
        });
      }
    }
    return cfg;
  });
}

// Declare the exact-alarm permissions (idempotent) so the countdown's expiry alarm is exact.
function withExactAlarmPermissions(config) {
  return withAndroidManifest(config, (cfg) => {
    const manifest = cfg.modResults.manifest;
    manifest['uses-permission'] = manifest['uses-permission'] || [];
    for (const name of EXACT_ALARM_PERMISSIONS) {
      const exists = manifest['uses-permission'].some(
        (p) => p.$ && p.$['android:name'] === name,
      );
      if (!exists) manifest['uses-permission'].push({ $: { 'android:name': name } });
    }
    return cfg;
  });
}

function withSampleNotificationStyles(config) {
  config = withSampleRendererSource(config);
  config = withSampleRendererManifest(config);
  config = withExactAlarmPermissions(config);
  return config;
}

module.exports = withSampleNotificationStyles;
