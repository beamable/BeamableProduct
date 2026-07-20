# Custom Notification Styles — iOS

**Status: proposed / not yet implemented.**

This spec describes how to give Beamable push notifications selectable *styles* on **iOS**,
including a working rich-media image, badge, and action buttons. It is self-contained and
end-to-end: it covers the shared wire contract, the push authoring extension, the APNs slice of
`PushRailService`, the iOS native module, and the small React Native bridge change required for
action categories. The Android counterpart lives in
[`custom-notifications-android.md`](./custom-notifications-android.md).

---

## 1. Overview & goal

On iOS most of the plumbing already exists — this work is largely **reconciliation and turning it
on for the remote path**:

- **Rich media** is supported by the Notification Service Extension (NSE):
  `iOS/BeamableNotifications/extension/ServicePlugins/RichMediaServicePlugin.swift:13` downloads an
  image referenced by `media-url` (or nested `bmn.mediaUrl`) and attaches it as a
  `UNNotificationAttachment`.
- **Badge** is supported: `NotificationManager.setBadge` (`NotificationManager.swift:168`) and
  `content.badge` in `buildContent` (`NotificationManager.swift:246`).
- **Templates** exist via `TemplateStore.swift` (`{placeholder}` substitution) and **action-button
  categories** via `CategoryStore.swift` (`register`).

The gap is that the **service never sends** `imageUrl` / `badge` / `sound` / `category`, and the
`actions` style needs its category registered on-device *before* the push arrives.

**Goal:** let the sender choose a *style* and pass a working `imageUrl` + `badge`, honored on the
remote push path.

**Design (hybrid model).** The wire payload carries a `style` id plus inline fields; iOS ships
**built-in presets** (no per-app registration required) while preserving the existing
`TemplateStore`/`CategoryStore` override path. Presets shipped in v1:

| `style` | iOS rendering |
|---------|---------------|
| `default` | plain alert; full body shown natively |
| `bigPicture` | image attachment via the NSE (`imageUrl`) |
| `bigText` | full body shown natively (no attachment) |
| `actions` | `UNNotificationCategory` action buttons (Open / Dismiss) |

`badge` is **orthogonal** — sent as `aps.badge` whenever a badge value is present.

---

## 2. Shared wire contract (§3.3 additions)

New keys added to the flat string→string §3.3 map. On iOS, scalar styling fields are lifted into
the APNs `aps` dictionary (badge / sound / category), while `imageUrl` rides in `userInfo` for the
NSE to consume.

| Key | Type (wire) | Meaning | Default when absent |
|-----|-------------|---------|---------------------|
| `style` | string | `default` \| `bigPicture` \| `bigText` \| `actions` | `default` |
| `imageUrl` | string | **canonical** rich-media image URL (aliases `media-url` / `bmn.mediaUrl` kept) | none |
| `badge` | string(int) | app-icon badge count → `aps.badge` | unchanged |
| `sound` | string | sound name → `aps.sound` | `default` |
| `category` | string | `UNNotificationCategory` id; derived from `style` when omitted | none |

**Doc updates required alongside implementation:**
- `nativeLibraries/docs/notifications-feature.md` §3.3 (~lines 170-192) — document the new keys,
  and note `imageUrl` is now the canonical key with `media-url`/`bmn.mediaUrl` as back-compat aliases.
- `PushRailService.cs:344-345` — the stale comment currently reads *"imageUrl / sound / badge are
  accepted but not rendered by the copied APNs/FCM clients."* Update it once they are rendered.

---

## 3. Push extension (shared sender UI)

`D:\Repositories\agentic-portal\extensions\hubs\player-engagement\push\src\App.tsx`

The extension **already** collects and sends `imageUrl`, `sound`, and `badge` in `extraDataFed`
(`buildExtraData`, `App.tsx:85`; interface `PushExtraData`, `App.tsx:34`). They currently do
nothing because the service drops them (§4). The only new UI is the style selector:

- `PushExtraData` (line 34): add `style?: string` and `category?: string`.
- `buildExtraData` (line 85): default `style` to `'default'`; keep `imageUrl`/`sound`/`badge`
  passthrough exactly as today.
- Compose card (~line 294): add a **Style** `<select>` / segmented control
  (Default / Big Picture / Big Text / Action buttons) bound to a new `style` state (default
  `'default'`). The live payload preview (~line 427) already renders `extraDataFed` verbatim.

---

## 4. Service — APNs slice

`D:\Repositories\agentic-portal\services\PushRailService`

**`PushRailService.cs`**
- `PushMessage` (line 685): add `string imageUrl; string sound; int? badge; string style; string category;`.
- `ParsePushMessage` (line 347): read `imageUrl` / `sound` / `style` / `category` via the existing
  `ReadString` helper, and `badge` via a new `ReadInt` helper. Keep the *title-or-body-required* guard.
- `WriteIntentData` (line 708): emit `imageUrl` and `style` as string entries (omit blanks) so they
  reach `userInfo`. `badge` / `sound` / `category` are lifted into `aps` in `BuildPayload` instead
  (below).
- **`DeliverBatch` per-recipient copy (~line 238) — easy to miss.** `DeliverBatch` parses once into
  a `baseMessage`, then builds a **new** `PushMessage` per recipient. That copy must forward
  `imageUrl`/`sound`/`badge`/`style`/`category`, or they're parsed but dropped before the provider.
  (This bit us on 2026-07-16.)

**`ApnsProvider.cs` — `BuildPayload` (line 174):**
- `aps["sound"]`: use `message.sound` when set, else `"default"` (currently hardcoded to `"default"`
  at `ApnsProvider.cs:183`).
- `aps["badge"] = message.badge.Value` when `message.badge.HasValue`.
- `aps["category"]`: `message.category` ?? a style-derived id (e.g. `beam_actions` when
  `style == "actions"`).
- `imageUrl` already flows into `userInfo` via `WriteIntentData`; the NSE attaches it (§5.1).
- Keep `mutable-content: 1` (`ApnsProvider.cs:188`) — required for the NSE to run and attach media.

---

## 5. iOS native

`D:\Repositories\BeamableProduct\nativeLibraries\iOS\BeamableNotifications`

### 5.1 `extension/ServicePlugins/RichMediaServicePlugin.swift` (line 13)

Extend the URL lookup to also read the **canonical** `imageUrl` key first, keeping the existing
`media-url` and `bmn.mediaUrl` as fallbacks:

```swift
let urlString = (content.userInfo["imageUrl"] as? String)
    ?? (content.userInfo["media-url"] as? String)
    ?? ((content.userInfo["bmn"] as? [String: Any])?["mediaUrl"] as? String)
```

Everything else in the plugin (download → `UNNotificationAttachment` → `content.attachments`) is
unchanged.

### 5.2 Built-in action categories

The `actions` style renders `UNNotificationAction` buttons. iOS requires the category to be
registered with `UNUserNotificationCenter` **before** the push is delivered, so it cannot be
purely payload-driven. Register a built-in `beam_actions` `CategorySpec` (Open / Dismiss) at init
by reusing `CategoryStore.register` (`CategoryStore.swift:14`). This is wired from the RN bridge
`initialize()` (§6). Apps may still register additional categories via the existing
`registerCategory` API (override path preserved).

### 5.3 Badge / sound / category on the remote path

Once APNs carries `aps.badge` / `aps.sound` / `aps.category` (§4), the OS honors them directly —
no `NotificationManager` change is required for the remote path. `setBadge` and `buildContent`
remain in use for the **local** scheduling path.

### 5.4 Reuse (do not re-implement)

`TemplateStore.swift`, `CategoryStore.register`, `RichMediaServicePlugin.swift`,
`NotificationManager.setBadge` / `buildContent`.

---

## 6. RN bridge changes

`D:\Repositories\BeamableProduct\nativeLibraries\EnginePlugins\ReactNative\src`

Minimal — the remote-path styling is native; the bridge only needs to register the iOS action
category so the `actions` style renders:

- `index.ts` — `initialize()` (line 468): on iOS, register the built-in `beam_actions` category via
  the existing `registerCategory` path (a `CategorySpec` with Open / Dismiss `ActionSpec`s).
- `types.ts`: add optional `style?: string` and `imageUrl?: string` to `LocalRequest` and
  `TemplateSpec` for type-completeness and future local use (does not block the e2e path).

---

## 7. Artifact rebuild (macOS-only)

Editing native Swift does **not** change the engine packages until the `.xcframework` is rebuilt
and restaged (see `AGENTS.md`).

```bash
# from iOS/BeamableNotifications
./scripts/build-xcframework.sh
# then restage into EnginePlugins/ReactNative/ios/Frameworks/
```

---

## 8. Verification (end-to-end)

1. Ensure realm secrets are set: `apns_push/*` (see `ApnsSettings` doc comment). Confirm via the
   `CheckPushConfig` ServerCallable.
2. Rebuild + restage the iOS `.xcframework` (§7) — requires macOS.
3. Build + install the RN sample on a real iOS device (release build; the NSE target must be
   bundled). Opt in to push and confirm the device token is registered via `/message-rail/register`
   (the delivery micro must be running).
4. Deploy `PushRailService` + the `push` extension (co-deployed via `microserviceDependencies`).
5. From the Portal push console, send **each** style and confirm on-device:
   - `bigPicture` (with `imageUrl`) → image attachment on the expanded notification,
   - `bigText` → full body,
   - `actions` → Open / Dismiss buttons (requires the app to have launched once so `beam_actions`
     is registered),
   - a `badge` value → app-icon badge count,
   - deep-link on tap still routes (no regression),
   - a plain `default` push is unchanged from today's behavior.
