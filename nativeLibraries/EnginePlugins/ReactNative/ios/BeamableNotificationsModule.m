#import <React/RCTBridgeModule.h>
#import <React/RCTEventEmitter.h>

// Exposes the Swift module to the React Native bridge. Method signatures mirror the
// @objc selectors in BeamableNotificationsModule.swift.
@interface RCT_EXTERN_MODULE(BeamableNotificationsModule, RCTEventEmitter)

RCT_EXTERN_METHOD(initialize)

RCT_EXTERN_METHOD(requestPermission:(NSDictionary *)options)
RCT_EXTERN_METHOD(getPermissionStatus)

RCT_EXTERN_METHOD(scheduleLocal:(NSDictionary *)request)
RCT_EXTERN_METHOD(cancelLocal:(NSString *)id)
RCT_EXTERN_METHOD(cancelAllLocal)
RCT_EXTERN_METHOD(getPending)

RCT_EXTERN_METHOD(registerForRemote)
RCT_EXTERN_METHOD(unregisterForRemote)

RCT_EXTERN_METHOD(registerTemplate:(NSDictionary *)template)
RCT_EXTERN_METHOD(registerCategory:(NSDictionary *)category)
RCT_EXTERN_METHOD(getDeliveryReceipts)

RCT_EXTERN_METHOD(setBadge:(nonnull NSNumber *)count)
RCT_EXTERN_METHOD(clearDelivered)

RCT_EXTERN_METHOD(getLaunchNotification:(RCTPromiseResolveBlock)resolve
                  rejecter:(RCTPromiseRejectBlock)reject)

// Offer / conversion funnel tracking — additive. Arg is an OfferTrackRequest JSON
// string (campaign context + the single offer).
RCT_EXTERN_METHOD(trackOfferClicked:(NSString *)requestJson)
RCT_EXTERN_METHOD(trackOfferConverted:(NSString *)requestJson)

// Auth for the closed-app analytics funnel — additive. Arg is a JSON string carrying
// { accessToken, refreshToken, accessTokenExpiresAt, cid, pid, host }.
RCT_EXTERN_METHOD(configureAuth:(NSString *)json)
RCT_EXTERN_METHOD(clearAuth)

// Live Activity (ActivityKit) — a live countdown shown WITHOUT tap-and-hold (Lock Screen /
// Dynamic Island). `options` = { title, body, expiresInSeconds | expiresAtMs }.
RCT_EXTERN_METHOD(startCountdownLiveActivity:(NSDictionary *)options)
RCT_EXTERN_METHOD(endCountdownLiveActivity)

// Live Activity push-to-start (iOS 17.2+). Begin observing the push-to-start + per-activity update
// tokens; each token is emitted via the liveActivity* events for the app to register with the rail.
RCT_EXTERN_METHOD(startLiveActivityPushRegistration)
// Re-emit `liveActivityCapability` on demand: whether this build can actually draw a Live Activity
// (iOS 17.2+, player setting on, widget embedded and declared) per attributes type.
RCT_EXTERN_METHOD(getLiveActivityCapabilities)
// Local starts for Simulator UI/button testing (push-to-start can't run in the Simulator).
// options = { title, headline, body, buttons:[{id,title,role}] } / { title, body, colors:[hex], flipIntervalMs }.
RCT_EXTERN_METHOD(startActionsLiveActivity:(NSDictionary *)options)
RCT_EXTERN_METHOD(startAnimatedLiveActivity:(NSDictionary *)options)
RCT_EXTERN_METHOD(endLiveActivities)

@end
