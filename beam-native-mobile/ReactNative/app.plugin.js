// Expo config-plugin entry point. Expo resolves this file when an app references
// the package by name in its `app.json` `plugins` array:
//
//   ["@beamable/notifications-react-native", { "appGroup": "group.…", "enableServiceExtension": true }]
//
// The implementation lives in ./plugin (alongside the native templates it copies).
module.exports = require('./plugin/withBeamableNotifications');
