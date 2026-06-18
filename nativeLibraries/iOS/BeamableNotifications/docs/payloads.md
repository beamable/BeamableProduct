# Payload schemas

All structured arguments and callback payloads are JSON. Engine wrappers (de)serialize
these for you, but the shapes are listed here for reference. Optional fields may be
omitted.

## LocalRequest (`scheduleLocal`)

```jsonc
{
  "id": "daily-reward",          // required, unique; reuse to replace
  "title": "Your reward is ready",
  "body": "Tap to claim",
  "subtitle": "Daily bonus",
  "badge": 1,
  "sound": "default",            // "default" | "none" | "<bundled filename>"
  "categoryId": "REWARD",        // action-button category id (feature 7)
  "threadId": "rewards",
  "interruptionLevel": "active", // passive | active | timeSensitive | critical
  "trigger": {
    "type": "timeInterval",      // immediate | timeInterval | calendar
    "seconds": 3600,
    "repeats": false
    // calendar form: "year","month","day","hour","minute","second","weekday"
  },
  "attachments": [               // local files only; remote media uses the NSE
    { "identifier": "img", "url": "file:///path/to/image.png" }
  ],
  "userInfo": { "deepLink": "game://store", "promoId": "abc" },
  "templateId": "welcome",       // optional: fill from a registered template
  "templateValues": { "name": "Ada" }
}
```

## PermissionOptions (`requestPermission`)

```jsonc
{ "alert": true, "badge": true, "sound": true,
  "provisional": false, "criticalAlert": false, "carPlay": false }
```

## TemplateSpec (`registerTemplate`)

```jsonc
{
  "id": "welcome",
  "titleFormat": "Welcome {name}!",
  "bodyFormat": "You have {count} gifts",
  "sound": "default",
  "categoryId": "GENERIC",
  "badge": 1,
  "defaultAttachments": []
}
```

## CategorySpec (`registerCategory`) — action buttons

```jsonc
{
  "id": "REWARD",
  "actions": [
    { "id": "CLAIM", "title": "Claim", "foreground": true },
    { "id": "DISMISS", "title": "Later", "destructive": true }
  ],
  "hiddenPreviewsBodyPlaceholder": "New reward"
}
```

## AnalyticsConfig (`configureAnalytics`) — feature 8

```jsonc
{
  "enabled": true,
  "endpoint": "https://api.example.com/notif-events",
  "headers": { "Authorization": "Bearer ..." },
  "commonParams": { "appVersion": "1.4.0", "playerId": "p_123" }
}
```

## Callback payloads

`onNotificationPresented` / `onNotificationReceived` / `onNotificationTapped` and
`getLaunchNotification` deliver a **NotificationData**:

```jsonc
{
  "id": "daily-reward",
  "title": "...", "body": "...", "subtitle": "...",
  "deepLink": "game://store",   // lifted from userInfo.deepLink when present
  "actionId": "CLAIM",          // set only when an action button was tapped
  "wasLaunch": true,            // set on the cold-start launch notification
  "userInfo": { "...": "..." }
}
```

- `onTokenReceived` → `{ "token": "<apns-hex>" }`
- `onTokenError` → `{ "error": "..." }`
- `onPermissionResult` → `{ "status": "authorized", "granted": true, "alert": true, "badge": true, "sound": true }`
- `onPendingNotifications` → `NotificationData[]`
- `onDeliveryReceipts` → `DeliveryReceipt[]` where each is
  `{ "id": "...", "timestamp": 1718600000.0, "source": "nse", "userInfo": {…} }`

## Remote push payload (sent by your backend to APNs)

To trigger rich media + closed-app analytics, send `mutable-content: 1` and include the
media URL and (optionally) a stable id:

```jsonc
{
  "aps": { "alert": { "title": "Hi", "body": "There" }, "mutable-content": 1, "sound": "default" },
  "media-url": "https://cdn.example.com/promo.jpg",
  "deepLink": "game://promo/42",
  "bmnId": "promo-42"
}
```
