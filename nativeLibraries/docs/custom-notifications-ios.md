# Custom Notification Styles — iOS

**Status: implemented** (built-in presets on the remote path, rendered in the NSE). Custom
app-defined styles are covered by the cross-engine
[`custom-notification-styles-guide.md`](./custom-notification-styles-guide.md) (iOS section).

This spec describes how Beamable push notifications get selectable *styles* on **iOS** — a working
rich-media image, badge, sound, and action buttons — on the remote (APNs) path. The Android
counterpart lives in [`custom-notifications-android.md`](./custom-notifications-android.md).

---

## 1. Overview & goal

The styling is applied **in the Notification Service Extension (NSE)**, mirroring Android's
client-renders model (`NotificationBuilder.applyStyle`). `PushRailService` stays completely
**style-agnostic**: `ApnsProvider` already sends `mutable-content: 1` (so the NSE runs on every push,
including when the app is killed) and the shared `WriteIntentData` already puts `style` / `imageUrl` /
`badge` / `sound` / `category` / arbitrary `extra` into the top-level **`userInfo`**. The NSE reads
those keys back and maps them onto the native notification — **no APNs payload change, no per-style
server code.**

**Built-in presets (v1):**

| `style` | iOS rendering | Applied by |
|---------|---------------|-----------|
| `default` | plain alert; full body shown natively | — (no work) |
| `bigPicture` | image attachment from `imageUrl` | `RichMediaServicePlugin` |
| `bigText` | full body shown natively on expand | — (no work) |
| `actions` | `UNNotificationCategory` buttons (Open / Dismiss) | `StyleServicePlugin` + `beam_actions` |

`badge` and `sound` are **orthogonal** to `style` and applied whenever present.

---

## 2. Shared wire contract

No change from the flat string→string §3.3 map. On iOS the styling fields stay in **`userInfo`**
(they are **not** lifted into the `aps` dictionary) and the NSE applies them to
`UNMutableNotificationContent`:

| Key | Meaning | NSE mapping |
|-----|---------|-------------|
| `style` | `default` \| `bigPicture` \| `bigText` \| `actions` | drives category default for `actions` |
| `imageUrl` | **canonical** rich-media image URL (aliases `media-url` / `bmn.mediaUrl` kept) | `content.attachments` |
| `badge` | app-icon badge count (string int) | `content.badge` |
| `sound` | bundled sound filename | `content.sound` |
| `category` | `UNNotificationCategory` id (buttons + which content extension renders) | `content.categoryIdentifier` |

---

## 3. Push extension (shared sender UI) + console

The console already collects/sends `style` / `imageUrl` / `sound` / `badge` / `category` as generic
`extraData`. The only console change for iOS parity is marking these styles as natively supported so
the native-support callout shows **"native for Android and iOS"**:

- `agentic-portal/.../push/src/notificationConfig.ts` — `NATIVE_SUPPORTED_STYLES.ios =
  ['default','bigPicture','bigText','actions']`.

---

## 4. Service — APNs slice

**No change.** `ApnsProvider.BuildPayload` (`ApnsProvider.cs:174`) already:
- sends `mutable-content: 1` (required for the NSE to run), and
- writes every styling field into top-level `userInfo` via `PushMessage.WriteIntentData`
  (`PushRailService.cs:782`).

The NSE owns the translation, so nothing style-specific belongs in the service (keeps the shared lib
generic for adopting teams).

---

## 5. iOS native

`nativeLibraries/iOS/BeamableNotifications`

### 5.1 `extension/ServicePlugins/RichMediaServicePlugin.swift`
Reads the canonical `imageUrl` key first, falling back to the legacy `media-url` / `bmn.mediaUrl`
aliases; downloads → `UNNotificationAttachment` → `content.attachments` (unchanged). Powers
`bigPicture`.

### 5.2 `extension/ServicePlugins/StyleServicePlugin.swift` (new)
A `NotificationServicePlugin` in the default NSE chain (`NotificationService.makePlugins`). Maps
`userInfo` onto the content: `badge` → `content.badge`, `sound` → `content.sound`,
`category` (or the built-in `beam_actions` for `style == "actions"`) → `content.categoryIdentifier`.
`default` / `bigText` need no work.

### 5.3 Built-in `beam_actions` category
`NotificationManager.initialize` registers a built-in `beam_actions` `CategorySpec` (Open / Dismiss)
via `CategoryStore.register` at app init — iOS resolves a remote notification's buttons from the
OS-held category registration, so it must exist before the push arrives. Apps may register additional
categories via the existing `registerCategory` API (accumulates, doesn't clobber).

### 5.4 Reuse (not re-implemented)
`RichMediaServicePlugin` download→attach, `CategoryStore.register` + `CategorySpec`/`ActionSpec`, the
NSE plugin chain + `BMNServicePlugins` discovery.

---

## 6. Custom (app-defined) styles

Beyond the four built-ins, apps add their own iOS styles two ways — both documented with worked
examples in [`custom-notification-styles-guide.md`](./custom-notification-styles-guide.md):

- **Transform-only** — a custom `NotificationServicePlugin` (attach media / tweak content), listed in
  the NSE target's `BMNServicePlugins` Info.plist array.
- **Fully-custom UI** — a `UNNotificationContentExtension` rendering a custom UIKit view in the
  expanded notification (keyed by `categoryIdentifier`), via a `BeamContentRenderer` listed in the
  content-extension target's `BMNContentRenderers`. This is the iOS analog of Android's fully-custom
  renderer (the RN sample ships a `countdown` demo). Limitation: custom UI shows only in the
  **expanded** view; the collapsed banner is the OS default.

---

## 7. Artifact rebuild (macOS-only)

Editing native Swift does **not** change the engine packages until the `.xcframework` is rebuilt and
restaged:

```bash
# from iOS/BeamableNotifications
./scripts/build-xcframework.sh
# then restage into EnginePlugins/ReactNative/ios/Frameworks/
```

For the RN sample, `expo prebuild` regenerates `ios/` with the NSE (+ Content Extension when
`enableContentExtension` is set) targets and their `BMNServicePlugins` / `BMNContentRenderers`.

---

## 8. Verification (end-to-end, macOS + real device)

1. Realm secrets set: `apns_push/*` — confirm via `CheckPushConfig`.
2. Rebuild + restage the iOS `.xcframework` (§7).
3. Build + install the RN sample on a real iOS device (release; the NSE target must be bundled). Opt
   in to push; confirm the token registers via `/message-rail/register`.
4. From the Portal push console, send each style and confirm on-device:
   - `bigPicture` (+`imageUrl`) → image attachment on expand,
   - `bigText` → full body,
   - `actions` → Open / Dismiss (app must have launched once so `beam_actions` is registered),
   - `badge` → app-icon badge count,
   - `default` unchanged; deep-link on tap still routes,
   - the sample `countdown` custom style → expanding shows the custom card with a ticking countdown
     (Content Extension).
