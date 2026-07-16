# Custom Notification Styles — Android

**Status: implemented** (Android native + `PushRailService` FCM slice + push extension style selector).
Pending: rebuild/restage the `.aar` (§6) and on-device verification (§8).

This spec describes how to give Beamable push notifications selectable *styles* on **Android**,
including a working rich-media image and a best-effort badge count. It is self-contained and
end-to-end: it covers the shared wire contract, the push authoring extension, the FCM slice of
`PushRailService`, and the Android native module. The iOS counterpart lives in
[`custom-notifications-ios.md`](./custom-notifications-ios.md).

---

## 1. Overview & goal

Today Android renders a single fixed notification shape. `NotificationBuilder.build`
(`Android/BeamableNotifications/notifications/src/main/java/com/beamable/push/NotificationBuilder.kt:54`)
sets only title / body / smallIcon / autoCancel / priority / contentIntent. There is:

- **no** `NotificationCompat.BigPictureStyle` / `BigTextStyle` / `setStyle(...)`,
- **no** `setLargeIcon(...)` — `NotificationTemplate.largeIconResName` is parsed but never applied,
- **no** image-URL field on the payload at all,
- badge is a **no-op stub** (`unity/UnityNotifications.kt:133` — `fun setBadge(count: Int) {}`),
- `registerTemplate` / `registerCategory` are no-ops on Android.

**Goal:** bring Android to parity with iOS by letting the sender choose a *style* and pass a
working `imageUrl` + `badge`. The sender selects the style; the Android library renders it.

**Design (hybrid model).** The wire payload carries a `style` id plus inline fields
(`imageUrl`, `badge`, title/body). The Android library ships **built-in style presets** so no
per-app registration is required. Presets shipped in v1:

| `style` | Android rendering |
|---------|-------------------|
| `default` | current behavior (auto-promote to BigText when body is long) |
| `bigPicture` | `BigPictureStyle` with `imageUrl` + `setLargeIcon` |
| `bigText` | `BigTextStyle` with the full body |

`badge` and action buttons are **orthogonal** to `style`: `badge` is applied via `setNumber`
whenever present; action buttons render whenever the notification names a registered `category`
(see §9). (Action buttons are no longer a `style` value — that hardcoded Open/Dismiss preset was
replaced by the configurable category system.)

---

## 2. Shared wire contract (§3.3 additions)

New keys added to the flat string→string §3.3 map. Because Android's FCM message is **data-only**
(the library builds the tray notification itself — see `FcmProvider.BuildPayload`), every styling
field travels as a plain `data` string.

| Key | Type (wire) | Meaning | Default when absent |
|-----|-------------|---------|---------------------|
| `style` | string | `default` \| `bigPicture` \| `bigText` | `default` |
| `imageUrl` | string | rich-media image URL for `bigPicture` | none (falls back to plain/bigText) |
| `badge` | string(int) | app-icon badge count | unchanged |
| `sound` | string | sound name (mapped to a channel) | `default` |
| `category` | string | id of a registered action-button category to render (see §9) | none (no buttons) |

**Doc updates required alongside implementation:**
- `nativeLibraries/docs/notifications-feature.md` §3.3 (~lines 170-192) — document the new keys.
- `PushRailService.cs:344-345` — the stale comment currently reads *"imageUrl / sound / badge are
  accepted but not rendered by the copied APNs/FCM clients."* Update it once they are rendered.

---

## 3. Push extension (shared sender UI)

`D:\Repositories\agentic-portal\extensions\hubs\player-engagement\push\src\App.tsx`

The extension **already** collects and sends `imageUrl`, `sound`, and `badge` in `extraDataFed`
(`buildExtraData`, `App.tsx:85`; interface `PushExtraData`, `App.tsx:34`). They currently do
nothing because the service drops them (§4). The only new UI is the style selector:

- `PushExtraData` (line 34): add `style?: string` (and `category?: string` for the iOS path).
- `buildExtraData` (line 85): default `style` to `'default'`; keep `imageUrl`/`sound`/`badge`
  passthrough exactly as today.
- Compose card (~line 294): add a **Style** `<select>` / segmented control
  (Default / Big Picture / Big Text / Action buttons) bound to a new `style` state (default
  `'default'`). The live payload preview (~line 427) already renders `extraDataFed` verbatim.

---

## 4. Service — FCM slice

`D:\Repositories\agentic-portal\services\PushRailService`

**`PushRailService.cs`**
- `PushMessage` (line 685): add `string imageUrl; string sound; int? badge; string style; string category;`.
- `ParsePushMessage` (line 347): read `imageUrl` / `sound` / `style` / `category` via the existing
  `ReadString` helper, and `badge` via a new `ReadInt` helper. Keep the *title-or-body-required* guard.
- `WriteIntentData` (line 708): emit `imageUrl`, `style`, `badge`, `sound` as string entries
  (omit blanks) — identical pattern to the existing scalar keys.
- **`DeliverBatch` per-recipient copy (~line 238) — easy to miss.** `DeliverBatch` parses once into
  a `baseMessage`, then builds a **new** `PushMessage` per recipient (adding `gamerTag`/`cidPid`).
  That copy must forward the new fields (`imageUrl`/`sound`/`badge`/`style`/`category`); otherwise
  they're parsed but silently dropped before reaching the provider and every push renders plain.
  (This bit us on 2026-07-16 — the copy only carried title/body/deepLink/campaignData.)

**`FcmProvider.cs` — `BuildPayload` (line 205):** no change beyond `WriteIntentData` now including
the new keys in the data-only map. Android reads them from `remoteMessage.data` and builds the
notification locally, so no FCM `notification` block is introduced (this preserves the reason the
message is data-only: `onMessageReceived` must fire in all app states — see the method's own
doc comment, `FcmProvider.cs:192-204`).

---

## 5. Android native (the bulk of the work)

`Android/BeamableNotifications/notifications/src/main/java/com/beamable/push/`

### 5.1 `PushModels.kt` — `NotificationTemplate` (line 81)

Add fields (all optional, defaulting to null):
- `imageUrl: String? = null`
- `style: String? = null`
- `badge: Int? = null`

Extend `toJson` (line 111) and `fromJson` (line 133) to round-trip them (mirror the existing
`smallIconResName` handling; `badge` uses `optInt`/`has` since 0 is a valid count). Keep field
names identical to the wire keys so the cross-platform contract note at `PushModels.kt:227`
stays true.

### 5.2 `NotificationBuilder.kt` — `build` (line 54)

After the existing small-icon resolution, branch on `template.style`:

- **`bigPicture`**: download `template.imageUrl` to a `Bitmap`; apply
  `NotificationCompat.BigPictureStyle().bigPicture(bmp)` and `builder.setLargeIcon(bmp)`. If the
  URL is blank or the download fails, **fall back** to the `default` branch (never fail the post).
- **`bigText`**: `NotificationCompat.BigTextStyle().bigText(template.body)`.
- **`actions`**: add two `NotificationCompat.Action`s — **Open** (a content-style `PendingIntent`
  reusing `buildContentIntent`) and **Dismiss** — routed through the existing
  `NotificationActionReceiver.kt` (already used by the local scheduler for action handling).
- **`default` / null**: current behavior; optionally auto-promote to `BigTextStyle` when the body
  exceeds one line (documents the "sensible default" behavior).

Badge (orthogonal, after the style branch):
```kotlin
template.badge?.let { builder.setNumber(it) }
```
Add a code comment that a numeric **app-icon** badge on Android is launcher/OEM-dependent and often
surfaces only as a notification dot; this is intentionally best-effort with **no new dependency**.

**Bitmap downloader.** Add a small private helper (e.g. `downloadBitmap(url: String): Bitmap?`)
using `HttpURLConnection` → `BitmapFactory.decodeStream`, returning null on any failure. This is
safe to call synchronously because `PushFirebaseService.onMessageReceived` runs on an FCM
background thread. No Glide/Coil/OkHttp dependency is added.

### 5.3 `PushFirebaseService.kt` — `displayDataMessage` (line 98)

Currently reads only `title` / `body` / `channelId` / `deeplink` / `smallIcon`. Also read
`imageUrl`, `style`, `badge` (parse to `Int?`), and `sound`, and populate the new
`NotificationTemplate` fields before calling `NotificationBuilder.show`.

### 5.4 Reuse (do not re-implement)

`NotificationBuilder.build` / `show` / `ensureChannel` / `buildContentIntent`,
`NotificationTemplate.toJson` / `fromJson` / `effectivePayload`, `NotificationActionReceiver.kt`.

---

## 6. Artifact rebuild

Editing native Kotlin does **not** change the engine packages until the `.aar` is rebuilt and
restaged (see `AGENTS.md`).

```bash
# from Android/BeamableNotifications
./gradlew :notifications:assembleRelease
# then restage the .aar into EnginePlugins/*/android/libs/ (per ../dev-native.sh)
```

> **⚠ Gradle transform-cache trap (this bit us — 2026-07-16).** The RN app consumes the `.aar`
> via `api fileTree(dir: "$projectDir/libs", include: ['*.aar'])`
> (`EnginePlugins/ReactNative/android/build.gradle`). Gradle caches the **exploded** form of that
> loose `.aar` under `~/.gradle/caches/<ver>/transforms/<hash>/` and does **not** reliably
> re-explode it when the file is overwritten in place under the same name. Result: you rebuild the
> `.aar`, rebuild the APK, and the APK still packages the **old** bytecode — every notification
> renders plain regardless of `style`. **`./gradlew clean` does NOT fix this** (the transform cache
> lives outside the project). `dev-native.sh` now deletes the stale transform after copying; if you
> restage the `.aar` by hand, also delete it:
>
> ```bash
> find ~/.gradle/caches -type d -name "beamable-notifications-release" \
>   -exec sh -c 'rm -rf "$(dirname "$(dirname "$1")")"' _ {} \;
> ```
>
> Then **uninstall + reinstall** the app (don't rely on an incremental install), and verify the
> packaged code is current (e.g. the regenerated transform's `classes.jar` contains `applyStyle`).

## 7. RN bridge note

The Android remote *styling* path (`PushFirebaseService` → `NotificationBuilder`) does not go through
the React Native `scheduleLocal` bridge, so styles need no bridge change. **Action buttons do** need
two small bridge changes (JS only, not iOS native): `registerCategory` gains an Android branch
(`BeamablePush.registerCategory(JSON.stringify(category))`), and `toNotificationData` maps `actionId`
so `notificationOpened.actionId` is populated on Android (parity with iOS). See §9.

---

## 8. Verification (end-to-end)

1. Ensure realm secrets are set: `fcm_push/service_account_json` (see `FcmSettings` doc comment).
   Confirm via the `CheckPushConfig` ServerCallable.
2. Rebuild + restage the Android `.aar` (§6 — the `dev-native.sh` cache-bust is essential).
3. Build + install the RN sample as a **release** build on a real Android device
   (`expo run:android --variant release`; rebuild the web SDK first). Opt in to push and confirm
   the device token is registered via `/message-rail/register` (the delivery micro must be running).
4. Deploy `PushRailService` + the `push` extension (co-deployed via `microserviceDependencies`).
5. From the Portal push console, confirm on-device:
   - `style: bigPicture` (with `imageUrl`) → expanded image + large icon,
   - `style: bigText` → full expanded body,
   - `category: beam_actions` → Accept / Decline buttons; tapping one opens the app and fires
     `notificationOpened` with `actionId` = `accept`/`decline` (see §9),
   - a `badge` value → app-icon dot (numeric badge is OEM-only — see §10),
   - deep-link on tap still routes (no regression),
   - a plain `default` push is byte-compatible with today's behavior.

---

## 9. Action buttons (categories)

Configurable, reactive action buttons via **registered categories** (matches iOS; reuses the shared
`CategorySpec`/`ActionSpec`/`NotificationData.actionId` types). Buttons are **orthogonal to `style`**.

- **Define:** the app registers a category once at start —
  `BeamNotifications.registerCategory({ id:'beam_actions', actions:[{id:'accept',title:'Accept',foreground:true},{id:'decline',title:'Decline',foreground:true}] })`.
- **Send:** a push carries `category: "beam_actions"`.
- **React:** tapping a button opens the app and fires the unified `notificationOpened` event with
  `actionId` = the tapped button's id; the app's handler decides the behavior. The full payload
  (deeplink, campaign data) rides along.

Android implementation:
- `CategoryStore.kt` (new) — persistent (SharedPreferences) registry so a **killed-app** data push
  can still resolve the category and render its buttons; `NotificationActionSpec`/`NotificationCategorySpec`
  models mirror the TS shapes. `ReactPushModule.registerCategory(json)` persists.
- `NotificationTemplate.category` + `PushFirebaseService.displayDataMessage` reads `data["category"]`.
- `NotificationBuilder.applyActions` renders one `addAction` per action (unique per-action request
  code); `buildActionIntent` is a `getActivity` PendingIntent (Android-12 trampoline-safe) carrying
  `EXTRA_ACTION_ID`. `IntentDataReader` merges it into the payload as `actionId`; it then flows through
  the existing `PushManager.dispatchNotificationOpened` → `onNotificationOpened` path — no new event.
- Note: an action tap does not auto-dismiss the notification in v1 (body tap still auto-cancels);
  the app can `cancelLocal` if desired.

## 10. Badge (best-effort, documented limitation)

`badge` is applied via `NotificationCompat.setNumber` and the channel is created with
`setShowBadge(true)`. On **stock Android / Pixel** this yields only a **notification dot** — a numeric
app-icon badge is OEM-specific (Samsung/Sony/etc.) and not available without a launcher-badge library.
Per decision, no such dependency is added. iOS renders `aps.badge` as a real numeric badge.
