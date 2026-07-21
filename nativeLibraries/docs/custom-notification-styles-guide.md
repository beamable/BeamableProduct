 # Custom Notification Styles — Implementation Guide (Android)

**Status: implemented.** Cross-engine guide for authoring a *custom* Android notification style —
one the shared `BeamableNotifications` library does **not** ship — end to end: the native Kotlin
renderer, wiring it into React Native / Unity / Unreal, and configuring the style + its fields in the
Push console extension.

This is the companion to [`custom-notifications-android.md`](./custom-notifications-android.md)
(which covers the **built-in** presets `default`/`bigPicture`/`bigText`/`actions`) and
[`notifications-feature.md`](./notifications-feature.md) (the overall feature). The shared library
ships only those built-in presets; everything beyond them is a *custom style* implemented in **your
app's own native layer** through the `PushNotificationStyleRenderer` hook described here.

> The canonical worked example ships in the repo: the React Native sample implements `animated` and
> `countdown` styles in
> [`nativeLibraries/Samples/ReactNative/plugins/android/SampleNotificationStyleRenderer.kt`](../Samples/ReactNative/plugins/android/SampleNotificationStyleRenderer.kt),
> wired by
> [`plugins/withSampleNotificationStyles.js`](../Samples/ReactNative/plugins/withSampleNotificationStyles.js).
> Read those alongside this guide.

---

## 1. What a custom style is

A **style** is a string id the sender puts on the push payload. The shared library renders its
built-in styles inside `NotificationBuilder.applyStyle`
(`Android/BeamableNotifications/notifications/src/main/java/com/beamable/push/NotificationBuilder.kt`).
Anything else is a **custom style**, and the library will not render it — instead it offers the
message to any registered **`PushNotificationStyleRenderer`** before falling back to a plain default
notification.

### The hook

`PushContracts.kt`:

```kotlin
interface PushNotificationStyleRenderer {
    fun render(context: Context, event: PushReceivedEvent): Boolean
}
```

- Return **`true`** = "I handled this notification" (you posted or scheduled it). The library then
  posts **nothing** — no duplicate, no default fallback.
- Return **`false`** = "not mine" — the library offers it to the next renderer, and if none claim it,
  posts its own default notification.

`PushReceivedEvent` (`PushModels.kt`) carries everything you need:

| Field | Meaning |
|-------|---------|
| `dataJson: String` | the full FCM `data` map as a JSON string — where you read `style` + your custom fields |
| `messageId: String?` | FCM message id (use it to derive a stable notification id) |
| `sentTimeMillis` / `receivedTimeMillis` | timestamps |
| `wasForeground: Boolean` | whether the app was foreground when received |
| `deepLink: String?` | normalized deep link, if any |
| `intentData` | parsed §3.3 intent data |

### When it runs

The renderer is invoked from `PushFirebaseService.onMessageReceived`, **only** on the
background/killed **data-only** path (`remoteMessage.notification == null`), *before* the library
posts its own notification:

```kotlin
if (event != null && PushManager.dispatchStyleRender(applicationContext, event)) return
displayDataMessage(remoteMessage, data)
```

That means it runs on FCM's background thread with **no React Native / engine runtime available** —
so a renderer must be **native code** (Kotlin/Java), not JS/C#/Blueprint. Foreground messages go to
the engine instead (`dispatchForegroundMessage`), so they never reach a style renderer.

> **Data-only pushes only.** Custom styles depend on the app receiving the message and rendering it
> itself, which only happens for data-only FCM messages. `PushRailService` already sends data-only
> high-priority messages, so this is the norm; just don't add a `notification` block to the payload.

### How renderers are discovered

Two ways, both handled by `PushManager` (`resolveRenderers` / `dispatchStyleRender`):

1. **Manifest meta-data** (reflection) — the usual path. Add a `<meta-data>` to `<application>`:
   ```xml
   <meta-data android:name="com.beamable.push.notification_style_renderer.1"
              android:value="com.your.app.YourStyleRenderer" />
   ```
   The key is used as a **prefix**: register several renderers with `...renderer.1`, `...renderer.2`,
   etc. (the manifest merger rejects duplicate exact names). The class is instantiated by reflection,
   so it **must have a public no-arg constructor**.
2. **Programmatic** — `PushManager.addStyleRenderer(renderer)` / `removeStyleRenderer(renderer)` at
   runtime (e.g. from your engine's init). Useful when you can construct the renderer with
   dependencies.

Renderers run in registration order (programmatic first, then manifest, deduped); the **first to
return `true` wins**. Per-renderer exceptions are isolated and routed to `dispatchError`.

### Wire contract

The sender emits a flat `string → string` map: a `style` id plus arbitrary keys. `PushRailService`
forwards every non-reserved key generically (`ReadExtra` captures them into `PushMessage.extra`,
`WriteIntentData` emits them into the FCM `data` map) — **no server change per style**. Your renderer
reads those keys back out of `event.dataJson`. So a field named `expiresInSeconds` in the console
arrives at your renderer as `data.optString("expiresInSeconds")`.

---

## 2. Write the native renderer (Kotlin)

Minimal skeleton — dispatch on `style`, build a `Notification`, post it, return `true`:

```kotlin
package com.your.app

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.beamable.push.PushNotificationStyleRenderer
import com.beamable.push.PushReceivedEvent
import org.json.JSONObject

class YourStyleRenderer : PushNotificationStyleRenderer {          // public no-arg ctor (implicit)

    override fun render(context: Context, event: PushReceivedEvent): Boolean {
        return try {
            val data = JSONObject(event.dataJson)
            when (data.optString("style")) {
                "yourStyle" -> renderYourStyle(context, event, data)
                else -> false                                       // not ours → let the lib handle it
            }
        } catch (_: Throwable) {
            false                                                   // any failure → default fallback
        }
    }

    private fun renderYourStyle(context: Context, event: PushReceivedEvent, data: JSONObject): Boolean {
        ensureChannel(context)
        val builder = NotificationCompat.Builder(context, CHANNEL_ID)
            .setContentTitle(data.optString("title"))
            .setContentText(data.optString("body"))
            .setSmallIcon(context.applicationInfo.icon)
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
        // …read your custom fields off `data` and build your custom view / style here…
        NotificationManagerCompat.from(context).notify(notificationId(event), builder.build())
        return true
    }

    private fun ensureChannel(context: Context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val mgr = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        mgr.createNotificationChannel(
            NotificationChannel(CHANNEL_ID, "Notifications", NotificationManager.IMPORTANCE_HIGH),
        )
    }

    private fun notificationId(event: PushReceivedEvent): Int =
        (event.messageId?.hashCode() ?: System.nanoTime().hashCode()) and Int.MAX_VALUE

    private companion object { const val CHANNEL_ID = "deeplink_channel" }   // matches the lib default
}
```

Two patterns worth copying from the sample renderer:

- **Resolve resources by name.** If your build system rewrites the class's package (the Expo plugin
  does — see §3), you can't reference a compile-time `R`. Resolve layouts/ids/colors at runtime:
  ```kotlin
  val layoutId = context.resources.getIdentifier("your_layout", "layout", context.packageName)
  ```
  `SampleNotificationStyleRenderer.renderAnimated`/`renderCountdown` do this throughout. Use only
  RemoteViews-supported widgets (`LinearLayout`, `TextView`, `Chronometer`, `ImageView`,
  `ViewFlipper`, …) in custom layouts.
- **Schedule follow-up posts** (timers, expiry, updates) by reusing the shared
  `LocalNotificationScheduler.scheduleAt(context, template, atMillis, exact)`. Posting with the
  **same notification id** replaces the notification in place — that's how `countdown` swaps the
  ticking offer for an "expired" one at zero. `exact = true` requires the exact-alarm permissions
  (see §3).

---

## 3. Wire it: React Native (Expo)

`expo prebuild` regenerates `android/` from scratch, so the renderer, its resources, and the manifest
entry must be re-applied on every prebuild by a **config plugin**. Copy the sample plugin
[`plugins/withSampleNotificationStyles.js`](../Samples/ReactNative/plugins/withSampleNotificationStyles.js);
it does three things:

1. **Copy the source + resources** (`withDangerousMod 'android'`):
   - the `.kt` → `android/app/src/main/java/<your.package as dirs>/YourStyleRenderer.kt`, rewriting
     its `package` line to `config.android.package` (regex `/^package\s+.+$/m`);
   - any res files → `android/app/src/main/res/{layout,drawable,values}/`.
2. **Register the meta-data** (`withAndroidManifest`): add
   `com.beamable.push.notification_style_renderer` → `<pkg>.YourStyleRenderer` to `<application>`
   (idempotent; use the `.1`/`.2` suffix if you register more than one).
3. **Add any extra permissions** it needs — the sample adds `USE_EXACT_ALARM` /
   `SCHEDULE_EXACT_ALARM` for the countdown's exact expiry alarm.

Register the plugin in `app.json`:

```json
{ "expo": { "plugins": [ "…", "./plugins/withSampleNotificationStyles" ] } }
```

The shared library itself is wired by the published Expo plugin
`@beamable/notifications-react-native`
([`EnginePlugins/ReactNative/plugin/withBeamableNotifications.js`](../EnginePlugins/ReactNative/plugin/withBeamableNotifications.js)),
which brings in the `.aar`, the FCM service, and the receive-time handler meta-data. Your style
plugin only adds the *style renderer* on top of it.

Rebuild: `expo prebuild -p android` then `expo run:android` (release: `--variant release`).

---

## 4. Wire it: Unity

The shared library ships as the `com.beamable.notifications` UPM package; its `.aar` (under
`Plugins/Android/`) and Gradle dependencies are auto-injected by `BeamableAndroidBuildProcessor.cs`,
and `BeamableAndroidSetup.cs` scaffolds/patches `Assets/Plugins/Android/AndroidManifest.xml`. To add a
custom style renderer:

1. **Add the renderer source** under `Assets/Plugins/Android/src/<your.package as dirs>/YourStyleRenderer.kt`
   (Kotlin, mirroring the received-handler sample the editor generates at
   `Assets/Plugins/Android/src/com/companyname/app/MyPushReceivedHandler.kt`; Java works too). Put any
   custom layouts/drawables/values under `Assets/Plugins/Android/res/`.
2. **Register the meta-data manually** in `Assets/Plugins/Android/AndroidManifest.xml`, inside
   `<application>`:
   ```xml
   <meta-data android:name="com.beamable.push.notification_style_renderer.1"
              android:value="com.your.app.YourStyleRenderer" />
   ```

> **Note:** unlike the receive-time handler (which the setup window can wire for you), there is **no
> editor helper** that auto-adds the style-renderer meta-data. It's a manual `<meta-data>` addition
> using the same prefix-keyed scheme. Everything else (the `.aar`, kotlin-stdlib, Firebase deps,
> `androidx`) is already provided by the package's build hooks.

Then build the Android player as usual (run the "Tools ▸ Beamable ▸ Android ▸ Setup and Validation"
window first if you haven't scaffolded the manifest).

---

## 5. Wire it: Unreal

The shared library ships as the `BeamPlatformNotifications` plugin. Its Android `.aar`, Gradle deps,
permissions, and deep-link wiring are injected by the Android Plugin Language file
[`Source/BeamPlatformNotifications/Android/BeamPlatformNotifications_APL.xml`](../EnginePlugins/Unreal/Source/BeamPlatformNotifications/Android/BeamPlatformNotifications_APL.xml).
To add a custom style renderer, do the equivalent additions from **your game's own APL**
(`AndroidPlugin` in your `.Build.cs`) or by extending the plugin's APL:

1. **Bring in the renderer source + resources** through the APL's Gradle additions (add your `.kt`/
   `.java` and `res/` to the gradle app module, as the plugin does for its own artifacts).
2. **Register the meta-data** with an `<androidManifestUpdates>` block adding, inside `<application>`:
   ```xml
   <meta-data android:name="com.beamable.push.notification_style_renderer.1"
              android:value="com.your.app.YourStyleRenderer" />
   ```

> **Note:** the built-in `BeamUnrealPushReceivedHandler` (inside the `.aar`) handles the native
> receive analytics, so games don't wire a *received handler*. The *style renderer* is not auto-added
> either — add the `<meta-data>` via APL yourself, using the prefix-keyed scheme.

---

## 6. Set up fields & styles in the Push console

The console (Push extension) is where the **sender-facing** side of the style is defined — which
fields the compose form offers and which style ids exist. Built-in styles/fields are hardcoded in
code (`DEFAULT_CONFIG` in `notificationConfig.ts`) and are immutable; **custom** styles/fields live in
the `NotificationConfigStorage` Micro storage.

Open the console → **Manage styles & fields** modal:

1. **Fields tab** — add each custom field. A field's **`id` is the exact wire key** your renderer
   reads (`data.optString("<id>")`). Pick the input `type` (`text`/`number`/`color`/`boolean`/
   `select`); `select` fields also take `options`.
2. **Styles tab** — add a style whose **`id` must equal** the string your renderer's `when(style)`
   matches (e.g. `"yourStyle"`), and select the field ids it uses (built-in fields like `title`/
   `body` are reusable from the shared pool).
3. **Save** — persists **only** the customs to Micro storage (built-ins always come from code).

The compose form then renders your fields; sending emits `style=<id>` plus each field id as an
`extraData` key.

### The native-vs-custom callout

Below the compose form the console shows a per-style callout, driven by `NATIVE_SUPPORTED_STYLES`
(`{ android: [...], ios: [...] }`) in `notificationConfig.ts`:

- **Built-in style** → green "native for Android / iOS …" (with a note about default fallback on
  platforms whose list doesn't include it).
- **Custom style** → yellow "custom-added — implement it natively (Android
  `PushNotificationStyleRenderer` and/or iOS NSE handler) reading these properties: …", plus a docs
  link when `CUSTOM_STYLE_DOCS_URL` is set.

If you list your custom style id in `NATIVE_SUPPORTED_STYLES[<platform>]`, the custom callout adds
"Already wired on <platform>" — a hint to senders that your renderer ships on that platform.

### Manual Mongo alternative

Instead of the modal you can insert straight into the two collections (omit `_id` — Mongo generates
it):

- **`notification_fields`**: `{ fieldId, label, type, options: [], required, defaultValue }`
- **`notification_styles`**: `{ styleId, label, fieldIds: [...] }`

(`beam project open-mongo NotificationConfigStorage`.) These map 1:1 to the console's fields/styles.

---

## 7. Verify end-to-end

1. Build & install the app with your renderer wired in (RN: `expo run:android --variant release`).
2. In the console, select your custom style, fill the fields, pick a recipient, **Send**. The payload
   preview should show `style=<id>` + your keys.
3. On the device (backgrounded/killed so it hits the data-only path), confirm your custom
   notification renders. Inspect with `adb logcat` and `adb shell dumpsys notification`.
4. Compare against the shipped samples: `animated` (cycling colored panels via `ViewFlipper`) and
   `countdown` (a ticking `Chronometer` that replaces itself with an "expired" notification at zero).

> **FCM on emulators** requires a Google-Play-Services image (`google_apis_playstore`); a plain
> `google_apis` emulator cannot receive FCM. See
> [`../Samples/ReactNative`](../Samples/ReactNative) notes.

If your renderer throws or returns `false`, delivery still succeeds — the library posts the default
notification — so a broken custom style never drops the message.
