// BeamableNotifications — C ABI for native engine consumers (Unity, Unreal).
//
// All structured arguments and results are UTF-8 JSON strings. Callbacks are function
// pointers invoked with a single JSON string argument. See docs/ for payload schemas.
//
// This header is informational for C/C++ callers; the symbols are exported by the Swift
// core via @_cdecl. React Native does not use this header (it calls Swift directly).

#ifndef BEAMABLE_NOTIFICATIONS_H
#define BEAMABLE_NOTIFICATIONS_H

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*bmn_callback)(const char *json);

// Lifecycle — call once at startup before anything else.
void bmn_initialize(void);

// Permission (feature 5). optionsJson: {"alert":true,"badge":true,"sound":true,...}
void bmn_requestPermission(const char *optionsJson);   // -> onPermissionResult
void bmn_getPermissionStatus(void);                    // -> onPermissionResult

// Local notifications (feature 1). See LocalRequest schema.
void bmn_scheduleLocal(const char *requestJson);
void bmn_cancelLocal(const char *id);
void bmn_cancelAllLocal(void);
void bmn_getPending(void);                             // -> onPendingNotifications

// Remote notifications (feature 2, raw APNs).
void bmn_registerForRemote(void);                      // -> onTokenReceived / onTokenError
void bmn_unregisterForRemote(void);

// Closed-app analytics config (feature 8). Stored in the App Group, read by app + NSE.
// configJson: {"enabled":true,"endpoint":"https://...","headers":{...},"commonParams":{...}}
void bmn_configureAnalytics(const char *configJson);
void bmn_getDeliveryReceipts(void);                    // -> onDeliveryReceipts

// Beamable funnel analytics auth + offer helpers (spec §4).
// configJson: AuthConfig {"accessToken":"","refreshToken":"","accessTokenExpiresAt":<sec>,
//                         "cid":"","pid":"","host":"https://..."} — persisted to App Group.
void bmn_configureAuth(const char *configJson);        // call on login/refresh
void bmn_clearAuth(void);                              // call on logout
// requestJson: OfferTrackRequest {"campaignId":"","nodeId":"","gamerTag":"","accountId":"",
//                                 "cidPid":"","deeplink":"","offer":{...}}
void bmn_trackOfferClicked(const char *requestJson);   // emits a "Clicked" funnel event
void bmn_trackOfferConverted(const char *requestJson); // emits a "Converted" funnel event

// Templates (feature 4) & action-button categories (feature 7).
void bmn_registerTemplate(const char *templateJson);
void bmn_registerCategory(const char *categoryJson);

// Badge / delivered.
void bmn_setBadge(int count);
void bmn_clearDelivered(void);

// Get intent (feature 6). Returns a malloc'd JSON string (or NULL); free with bmn_free.
const char *bmn_getLaunchNotification(void);
void bmn_free(const char *ptr);

// Callbacks (feature 3). Register once; each fires with a JSON string.
void bmn_setOnPermissionResult(bmn_callback cb);
void bmn_setOnTokenReceived(bmn_callback cb);          // {"token":"<hex>"}
void bmn_setOnTokenError(bmn_callback cb);             // {"error":"..."}
void bmn_setOnNotificationPresented(bmn_callback cb);  // foreground willPresent
void bmn_setOnNotificationReceived(bmn_callback cb);   // received while app alive
void bmn_setOnNotificationTapped(bmn_callback cb);     // tap / action (incl. actionId, deepLink)
void bmn_setOnPendingNotifications(bmn_callback cb);   // JSON array
void bmn_setOnDeliveryReceipts(bmn_callback cb);       // JSON array (replayed NSE receipts)

#ifdef __cplusplus
}
#endif

#endif /* BEAMABLE_NOTIFICATIONS_H */
