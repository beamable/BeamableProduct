# Custom Notifications & Live Activities (React Native)

How to add your own notification styles to the Beamable React Native SDK and drive them from the
Push console (the `player-engagement/push-message-rail` portal extension) — from zero. Covers both **Android** and
**iOS**, the **portal + realm setup** (secrets, per-style fields), and **iOS Live Activities**
(the always-visible Lock-Screen / Dynamic-Island cards), including what happens when a device can't
show a Live Activity and how to control that from the portal.

The RN sample at `beam-native-mobile/Samples/ReactNative` is the worked reference for everything here.

This doc is the **RN-focused map**. The platform-neutral material — the wire contract, push-console
authoring, the Android renderer hook and the iOS NSE/Content-Extension seams, artifact rebuild — lives
in the shared guide, and is not repeated here:
- *Push notifications for React Native* in the Beamable documentation — the published guide, which
  covers the wire contract, push-console authoring, both renderer seams, APNs/FCM realm setup, and the
  end-to-end walkthrough

What's **only** here: the mental model for choosing between a custom notification and a Live Activity
(§0), the RN-specific renderer gotchas (§2), `app.json` wiring (§4), **Live Activities** end to end
(§5), and the unsupported-device fallback (§6).

---

## 0. Mental model

There are **two delivery mechanisms**, and picking the right one is the first decision:

| | **Custom Notification** | **Live Activity** (iOS only) |
|---|---|---|
| What it is | A push notification with a custom-rendered banner/expanded view | An always-visible, self-updating card on the Lock Screen / Dynamic Island |
| Platforms | Android + iOS | iOS 17.2+ only (physical device) |
| Trigger | A normal APNs/FCM push (`apns-push-type: alert`) | APNs **push-to-start** (`apns-push-type: liveactivity`) |
| Visibility | Collapsed banner (Android custom views) / expanded only (iOS Content Extension) | Persistent, no tap required |
| Buttons | Android: always; iOS: only when expanded (OS rule) | Interactive App Intent buttons, always visible |
| Examples in the sample | `countdown`, `animated` (Android), `countdown` (iOS expand) | `liveActivityActions`, `liveActivityAnimated`, plus the countdown Live Activity |

Everything is **data-driven from one wire contract**: the push rail (`services/PushMessageRailService`)
puts your authored fields into the APNs `userInfo` / FCM `data` map under the **exact key names you
choose in the portal**, and native reads whatever keys it understands. **The single most important
rule:**

> **The portal field `id` IS the wire key. It must match the key the native renderer reads, byte for
> byte.** A typo means the field silently never reaches the renderer.

---

## 1. Realm setup (secrets) — do this once

The rail delivers real pushes using credentials stored in **Realm Secrets** (encrypted at rest), read
at send time. Set them in **Portal → your Realm → Secrets**:

**iOS (APNs)** — under the `apns_push/` prefix:
- `apns_push/auth_key` — the full contents of your `AuthKey_XXXX.p8` (PEM, `-----BEGIN…` included)
- `apns_push/key_id` — the 10-char Key ID of that key
- `apns_push/team_id` — your 10-char Apple Team ID
- `apns_push/bundle_id` — the app bundle id (the APNs topic), e.g. `com.beamble.samples.beamfarmrn`
- `apns_push/default_environment` — `sandbox` (default) or `production`

**Android (FCM)** — under the `fcm_push/` prefix:
- `fcm_push/service_account_json` — the full Firebase service-account JSON

> **Sandbox vs production:** a dev build (`aps-environment = development`) can only receive on the
> **sandbox** APNs host. If a device registered as `production` while running a dev build, APNs returns
> `BadDeviceToken` and nothing arrives. The environment is stored per device at registration and
> resolved against `apns_push/default_environment`. See the APNs setup guide for the full matrix.

Verify provisioning without leaking secrets: the rail exposes a `CheckPushConfig` server callable that
reports whether each provider's secrets are present and parse.

Full Apple Developer setup (App ID + Push capability, the `.p8` key, provisioning) is in
the *Push notifications for React Native* guide in the Beamable documentation.

---

## 2. Add a Custom Notification style

A custom style is: **(a)** a native renderer for that `style` id on each platform, and **(b)** the
style + its fields authored in the Push console so an operator can send it. The RN sample already ships
`countdown` and `animated` as the reference.

The renderer APIs themselves are documented once, for all engines, in the shared guide: the Android
`PushNotificationStyleRenderer` hook in
the *custom styles for Android* section,
and the two iOS seams (NSE service plugin, Content Extension renderer) in
the *custom styles for iOS* section. The sample's
implementations are `plugins/android/SampleNotificationStyleRenderer.kt`,
`plugins/ios/SampleStyleServicePlugin.swift`, and `plugins/ios/SampleCountdownContentRenderer.swift`.

What's RN-specific:

### 2.1 The renderers are copied in on every prebuild

`expo prebuild` regenerates the native projects, so nothing you add by hand survives. Android renderers
are copied and manifest-registered by the app-owned config plugin
`plugins/withSampleNotificationStyles.js` (which also rewrites the Kotlin `package` line); iOS plugins
and renderers are copied into the extension targets by the Beamable plugin from your `app.json` props
(§4). Class discovery is by name from the extension `Info.plist` arrays (`BMNServicePlugins`,
`BMNContentRenderers`) plus `UNNotificationExtensionCategory` — the plugin writes all three from those
props, which is why the Swift classes need `@objc(ClassName)`.

### 2.2 iOS gotchas baked into the plugin

- The Content Extension target links `UserNotificationsUI` — without it the extension crashes blank.
- For local/HTTP image hosts the NSE target gets an ATS exception when `APP_VARIANT=local`; the
  extension has its own ATS and does **not** inherit the app's. Public HTTPS images always work.

### 2.3 Wire type matters (a real bug we hit)

Read fields tolerantly. The rail JSON-encodes numbers as **numbers**; a hand-authored `simctl push`
`.apns` file uses **strings**. Cast for both, e.g. in Swift:

```swift
func number(_ key: String) -> Double? {
    (info[key] as? NSNumber)?.doubleValue ?? (info[key] as? String).flatMap(Double.init)
}
```

---

## 3. Author the style + fields in the Push console

Authoring styles and fields is engine-independent — see the shared guide
the *Push console* section for the modal,
the field types, and the Mongo alternative. Two things worth repeating because they cause most of the
lost time here:

**Field-matching is the #1 failure mode.** A `FieldDefinition.id` *is* the wire key, so it must match
what your renderer reads (`colors`, `flipIntervalMs`, `expiresInSeconds`, …) byte for byte. No shared
contract module enforces this, so the ids are your responsibility on both sides. The console's
custom-style callout lists the exact field ids it will send — cross-check them against your renderer.

**Use the Payload preview.** It shows the exact `extraDataFed` JSON that will be sent; confirm `style`
and every field key/value there before sending.

---

## 4. RN app wiring (`app.json`)

The Beamable Expo config plugin generates the iOS extension targets and copies your Swift/Kotlin from
their source paths on every `expo prebuild`. The sample's `app.json` shows the full shape:

```jsonc
["@beamable/notifications-react-native", {
  "appGroup": "group.com.beamable.mobiletest",
  "enableServiceExtension": true,
  "iosServicePlugins": [{ "file": "./plugins/ios/SampleStyleServicePlugin.swift", "className": "SampleStyleServicePlugin" }],
  "enableContentExtension": true,
  "contentCategories": ["countdown"],
  "iosContentRenderers": [{ "file": "./plugins/ios/SampleCountdownContentRenderer.swift", "className": "SampleCountdownContentRenderer" }],
  "enableLiveActivity": true,
  "iosLiveActivityWidgets": [
    { "file": "./plugins/ios/SampleWidgetsBundle.swift" },
    { "file": "./plugins/ios/SampleCountdownLiveActivity.swift" },
    { "file": "./plugins/ios/SampleActionsLiveActivity.swift" },
    { "file": "./plugins/ios/SampleAnimatedLiveActivity.swift" }
  ]
}]
```

Android custom renderers are wired by the app-owned `plugins/withSampleNotificationStyles.js`.

After changing native code or `app.json`: `expo prebuild --clean` then build. (On a low-RAM machine
build gently: `xcodebuild … -jobs 2 ENABLE_DEBUG_DYLIB=NO`.)

---

## 5. Live Activities (iOS 17.2+)

A Live Activity is the **always-visible** card (Lock Screen + Dynamic Island) that updates itself —
the iOS answer to "show it without tap-and-hold" and to persistent action buttons. It is **not** a
notification style; it's a separate ActivityKit surface started by an APNs **push-to-start** push.

### 5.1 The pieces (already built in the sample)

- **Shared attributes** in `ReactNative/ios/*ActivityAttributes.swift` —
  `BeamActionsActivityAttributes`, `BeamAnimatedActivityAttributes`, `BeamCountdownActivityAttributes`.
  These are compiled into the app (podspec) **and** copied into the widget target by the plugin so both
  define the identically-named type (ActivityKit matches by the unqualified type name).
- **Widgets** in `Samples/ReactNative/plugins/ios/Sample*LiveActivity.swift` + one `@main`
  `SampleWidgetsBundle.swift`. Interactive buttons use `BeamLiveActivityActionIntent` (an
  `AppIntent` — the widget target links `-framework AppIntents`).
- **Token plumbing** on the native module: `BeamNotifications.startLiveActivityPushRegistration()`
  observes the push tokens and emits them to JS; the sample's `src/beam/liveActivity.ts` forwards them
  to the rail.

### 5.2 Registering push-to-start tokens (required, once per device)

The backend can only start a Live Activity for a device that has **registered a push-to-start token**.
Call `BeamNotifications.startLiveActivityPushRegistration()` after connecting; forward the two events
to `message-rail/register` (`push` federation), mapping the short slug to the Swift type name:

```ts
BeamNotifications.addListener('liveActivityPushToStartToken', (p) =>
  registerRail('push', { kind: 'liveActivityPushToStart', attributesType: /* Beam…Attributes */, token: p.token }))
BeamNotifications.addListener('liveActivityUpdateToken', (p) =>
  registerRail('push', { kind: 'liveActivityUpdate', activityId: p.activityId, attributesType, token: p.token }))
```

(See `src/beam/liveActivity.ts` for the complete, working version incl. the slug→type map.) Tokens
**only ever arrive on a physical iOS 17.2+ device** — never on the Simulator. For Simulator UI/button
testing use the local-start methods (`BeamNotifications.startActionsLiveActivity(...)`, etc.).

### 5.3 Authoring a Live Activity in the portal

Add a style with a **`liveActivity` block** (`notificationConfig.ts` → `StyleDefinition.liveActivity`).
The sample ships two built-ins: **`liveActivityActions`** and **`liveActivityAnimated`**. The block
declares:
- `attributesType` — the **unqualified Swift type name** (e.g. `BeamActionsActivityAttributes`).
- `attributeFieldIds` — fields assembled into `aps.attributes` (start only; static for the activity).
- `contentStateFieldIds` — fields assembled into `aps.content-state` (the live, updatable data).
- `contentStateDefaults` — non-authored scaffolding the Swift `ContentState` requires (e.g.
  `isResolved:false`, `activeIndex:0`) so decoding never fails on a missing key.

> **The `attributesType` and every attributes/content-state field id must match the Swift `Codable`
> shapes exactly**, or the device silently no-ops while APNs still returns 200.

In the console, picking a `liveActivity*` style shows an **iOS-Live-Activity callout** (instead of the
native/custom one) with an `Event` selector:
- **`start`** — begins a new activity (needs a registered push-to-start token).
- **`update`** / **`end`** — target a running activity by its `activityId` (a text field appears).

### 5.4 Normal Custom Notification vs Live Activity — how the portal decides

Purely by the selected **style**: a style whose definition has a `liveActivity` block is sent as a Live
Activity (the console emits `liveActivity: true`, `event`, `attributesType`, `attributes`,
`contentState`); every other style is a normal notification (`style` + your flat fields). You don't
toggle a mode — you pick the style. Live Activity styles are intentionally **not** in
`NATIVE_SUPPORTED_STYLES` (they're not notifications), which is why they get their own callout.

---

## 6. Fallback when a device can't show a Live Activity

A Live Activity start needs a registered push-to-start token. A recipient may have **none** — they're
on Android, on iOS < 17.2, or their app never minted/registered one. The Live Activity style exposes an
**"If device unsupported"** control (portal field `liveActivityFallback`) with two options:

- **`default`** *(recommended default)* — the rail delivers a **normal notification** to that
  recipient's device tokens instead, built from the Live Activity's **Title / Body** (style forced to
  `default`). Supported devices still get the Live Activity; unsupported ones get a plain push.
- **`skip`** — the rail delivers **nothing** to unsupported recipients. They're reported with a benign,
  non-retriable `skipped` status (not an error).

Mechanics (in `PushMessageRailService.DeliverLiveActivity`): a `start` with no push-to-start token and
`fallback = default` falls through to the normal APNs/FCM delivery path; `skip` records the skip;
anything else (absent / `none`) reports a terminal `unknown` "no token" error. `update`/`end` never
fall back to a notification (there's nothing running to represent) — a missing token there is skipped or
errored per the same policy.

> Fallback delivers to the recipient's **device tokens**, so the device must also be registered for
> normal push (the usual `message-rail/register` with `{ token, platform, environment }`). A player with
> neither a Live Activity token nor a device token is reported as "no registered devices".

---

## 7. Verify end-to-end

**Simulator (iOS 17 sim) — no backend needed:**
- Custom notification: `xcrun simctl push booted <bundleId> payload.apns` with `aps.mutable-content:1`,
  `style`, and your fields (numbers as strings for simctl). Long-press to see the Content Extension.
- Live Activity UI + buttons: the sample's **Start Actions/Animated Live Activity** buttons (local
  start), then lock the screen.

**Physical device (iOS 17.2+) + backend, from the portal:**
1. Add the realm secrets (§1). Connect the sample; confirm the push-to-start tokens register on connect.
2. Send a normal custom style (e.g. `countdown`, `bigPicture`) → confirm it renders (expand for the iOS
   Content Extension).
3. Send `liveActivityActions` with `event: start` → the card appears with the app not running; verify
   the Claim/Dismiss buttons update/end it; then send `update`/`end` with the `activityId`.
4. Test fallback: send a Live Activity to a recipient with no push-to-start token (e.g. an Android
   player or a pre-17.2 device) with `If device unsupported = default` → they get a normal notification;
   with `skip` → they get nothing and are reported `skipped`.

The console's per-recipient Activity Log shows `sent` vs the terminal statuses
(`unknown` / `invalid` / `skipped` / retriable `network`) so you can confirm each recipient's outcome.

---

## 8. Quick reference — where things live

| Concern | File |
|---|---|
| Android custom renderer (sample) | `Samples/ReactNative/plugins/android/SampleNotificationStyleRenderer.kt` |
| iOS NSE style plugin (sample) | `Samples/ReactNative/plugins/ios/SampleStyleServicePlugin.swift` |
| iOS Content Extension renderer (sample) | `Samples/ReactNative/plugins/ios/SampleCountdownContentRenderer.swift` |
| iOS shared LA attributes + intent | `ReactNative/ios/*ActivityAttributes.swift`, `LiveActivityActionIntent.swift` |
| iOS LA widgets (sample) | `Samples/ReactNative/plugins/ios/Sample*LiveActivity.swift`, `SampleWidgetsBundle.swift` |
| Expo config plugin (targets, ATS, frameworks) | `ReactNative/plugin/withBeamableNotifications.js` |
| JS API (LA start/registration) | `ReactNative/src/index.ts` |
| Sample LA token forwarding | `Samples/ReactNative/src/beam/liveActivity.ts` |
| Rail: payload build + routing + fallback | `agentic-portal/services/PushMessageRailService/{ApnsProvider,PushMessageRailService}.cs` |
| Portal: styles/fields/LA meta | `agentic-portal/extensions/hubs/player-engagement/push-message-rail/src/notificationConfig.ts` |
| Portal: compose UI + payload assembly | `agentic-portal/extensions/hubs/player-engagement/push-message-rail/src/App.tsx` |
