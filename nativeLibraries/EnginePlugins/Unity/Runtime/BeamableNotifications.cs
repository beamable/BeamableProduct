using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using Beamable.Serialization;
using Beamable.Api.Analytics;
using Beamable.Common.Dependencies;

namespace Beamable.Notifications
{
    /// <summary>
    /// Unity entry point for Beamable notifications. One cross-platform API: on iOS it drives the
    /// BeamableNotifications Swift SDK (C ABI <c>bmn_*</c>); on Android it drives the Kotlin
    /// <c>com.beamable.push</c> library through the <c>UnityNotifications</c> facade. Both share the
    /// same DTOs (see Payloads.cs) and raise the same events on the Unity main thread.
    ///
    /// Call <see cref="Initialize"/> once at startup, subscribe to the events you care about, then
    /// drive the API. Methods/features with no Android equivalent are best-effort / no-op there.
    /// </summary>
    public static class BeamableNotifications
    {
        // Events (feature 3)
        public static event Action<PermissionResult> OnPermissionResult;
        public static event Action<string> OnTokenReceived;
        public static event Action<string> OnTokenError;
        public static event Action<NotificationData> OnNotificationPresented;
        public static event Action<NotificationData> OnNotificationReceived;
        public static event Action<NotificationData> OnNotificationTapped;
        public static event Action<List<NotificationData>> OnPendingNotifications;
        public static event Action<List<DeliveryReceipt>> OnDeliveryReceipts;

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            Dispatcher.Ensure();

#if UNITY_IOS && !UNITY_EDITOR
            // Register native callbacks. Delegate instances are held in static fields
            // (below) so the GC cannot collect them while native code holds pointers.
            Native.bmn_setOnPermissionResult(_permission);
            Native.bmn_setOnTokenReceived(_tokenReceived);
            Native.bmn_setOnTokenError(_tokenError);
            Native.bmn_setOnNotificationPresented(_presented);
            Native.bmn_setOnNotificationReceived(_received);
            Native.bmn_setOnNotificationTapped(_tapped);
            Native.bmn_setOnPendingNotifications(_pending);
            Native.bmn_setOnDeliveryReceipts(_receipts);

            Native.bmn_initialize();
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Spawn the GameObject the native bridge targets via UnitySendMessage, then init.
            AndroidNotificationsRelay.Ensure();
            AndroidBackend.Initialize();
#endif
        }

        // MARK: API

        public static void RequestPermission(PermissionOptions options = null)
        {
            string json = JsonSerializable.ToJson(options ?? new PermissionOptions());
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_requestPermission(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("requestPermission", json);
#endif
        }

        public static void GetPermissionStatus()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_getPermissionStatus();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("getPermissionStatus");
#endif
        }

        public static void ScheduleLocal(LocalRequest request)
        {
            string json = JsonSerializable.ToJson(request);
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_scheduleLocal(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("scheduleLocal", json);
#endif
        }

        public static void CancelLocal(string id)
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_cancelLocal(id);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("cancelLocal", id);
#endif
        }

        public static void CancelAllLocal()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_cancelAllLocal();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("cancelAllLocal");
#endif
        }

        public static void GetPending()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_getPending();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("getPending");
#endif
        }

        public static void RegisterForRemote()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_registerForRemote();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("registerForRemote");
#endif
        }

        public static void UnregisterForRemote()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_unregisterForRemote();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("unregisterForRemote");
#endif
        }

        public static void ConfigureAnalytics(AnalyticsConfig config)
        {
            string json = JsonSerializable.ToJson(config);
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_configureAnalytics(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("configureAnalytics", json);
#endif
        }

        // MARK: Auth credentials (§4 closed-app campaign funnel)

        /// <summary>
        /// Push the player's auth credentials to the native layer so the native push/campaign
        /// pipeline can attribute conversions while the app is closed/backgrounded. The game should
        /// call this on login and again whenever the token is refreshed; call <see cref="ClearAuth"/>
        /// on logout. Field names are sent as the canonical camelCase contract both natives expect
        /// (<c>accessToken</c>, <c>refreshToken</c>, <c>accessTokenExpiresAt</c>, <c>cid</c>,
        /// <c>pid</c>, <c>host</c>).
        /// </summary>
        /// <param name="accessTokenExpiresAtMs">Absolute token expiry as epoch time in MILLISECONDS.</param>
        public static void ConfigureAuth(string accessToken, string refreshToken,
            long accessTokenExpiresAtMs, string cid, string pid, string host)
        {
            var creds = new AuthCredentials
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAtMs,
                Cid = cid,
                Pid = pid,
                Host = host,
            };
            string json = JsonSerializable.ToJson(creds);
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_configureAuth(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("configureAuth", json);
#endif
        }

        /// <summary>
        /// Convenience that pulls the current player's token from <see cref="BeamContext.Default"/>
        /// and forwards it to <see cref="ConfigureAuth(string,string,long,string,string,string)"/>.
        /// <paramref name="host"/> must be supplied by the integrator because it is the Beamable API
        /// host (from the game's environment config), not part of the access token. Call on login and
        /// on every token refresh; call <see cref="ClearAuth"/> on logout. No-op if no token exists.
        /// </summary>
        public static void ConfigureAuthFromContext(string host)
        {
            var token = BeamContext.Default?.AccessToken;
            if (token == null || string.IsNullOrEmpty(token.Token))
            {
                Debug.LogWarning("[BeamableNotifications] ConfigureAuthFromContext: no access token on the default context.");
                return;
            }
            long expiresAtMs = (long)(token.ExpiresAt.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds;
            ConfigureAuth(token.Token, token.RefreshToken, expiresAtMs, token.Cid, token.Pid, host);
        }

        /// <summary>
        /// Clear any auth credentials previously pushed via <see cref="ConfigureAuth"/>. Call on logout.
        /// </summary>
        public static void ClearAuth()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_clearAuth();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("clearAuth");
#endif
        }

        public static void GetDeliveryReceipts()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_getDeliveryReceipts();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("getDeliveryReceipts");
#endif
        }

        public static void RegisterTemplate(TemplateSpec template)
        {
            string json = JsonSerializable.ToJson(template);
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_registerTemplate(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("registerTemplate", json);
#endif
        }

        public static void RegisterCategory(CategorySpec category)
        {
            string json = JsonSerializable.ToJson(category);
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_registerCategory(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("registerCategory", json);
#endif
        }

        public static void SetBadge(int count)
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_setBadge(count);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("setBadge", count);
#endif
        }

        public static void ClearDelivered()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_clearDelivered();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("clearDelivered");
#endif
        }

        /// <summary>The notification that launched the app (feature 6, "get intent"), or null.</summary>
        public static NotificationData GetLaunchNotification()
        {
#if UNITY_IOS && !UNITY_EDITOR
            IntPtr ptr = Native.bmn_getLaunchNotification();
            if (ptr == IntPtr.Zero) return null;
            try
            {
                string json = Marshal.PtrToStringAnsi(ptr);
                return string.IsNullOrEmpty(json) ? null : JsonSerializable.FromJson<NotificationData>(json);
            }
            finally
            {
                Native.bmn_free(ptr);
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            string json = AndroidBackend.CallStr("getLaunchNotification");
            return string.IsNullOrEmpty(json) ? null : JsonSerializable.FromJson<NotificationData>(json);
#else
            return null;
#endif
        }

        // MARK: Offer / conversion analytics helpers (§4.7)

        /// <summary>Funnel stage names matching the §4.6 shared contract.</summary>
        private const string FunnelCategory = "notification_funnel";

        /// <summary>
        /// Record that the player clicked an offer that came from a campaign notification (§4.7).
        /// Emits a <c>Clicked</c> funnel <see cref="CoreEvent"/> via the player's Beamable
        /// analytics service. <paramref name="campaign"/> carries the context that arrived in the
        /// notification's intent data (see <see cref="NotificationData.CampaignIntent"/>) so the
        /// conversion attributes back to the originating notification. No-op when the campaign is
        /// not tracked (missing campaignId/nodeId, §4.2).
        /// </summary>
        /// <param name="campaign">The campaign intent data the notification carried.</param>
        /// <param name="offer">The single offer that was clicked (optional).</param>
        public static void TrackOfferClicked(NotificationIntentData campaign, Offer offer = null) =>
            TrackOfferFunnel("Clicked", campaign, offer);

        /// <summary>
        /// Record that an offer click resulted in a conversion (§4.7). Emits a <c>Converted</c>
        /// funnel <see cref="CoreEvent"/>. See <see cref="TrackOfferClicked"/> for attribution rules.
        /// </summary>
        public static void TrackOfferConverted(NotificationIntentData campaign, Offer offer = null) =>
            TrackOfferFunnel("Converted", campaign, offer);

        /// <summary>
        /// Convenience overload taking the raw campaign/node ids plus the optional offer, for callers
        /// that only kept the ids from the originating notification.
        /// </summary>
        public static void TrackOfferClicked(string campaignId, string nodeId, Offer offer = null) =>
            TrackOfferFunnel("Clicked", BuildIntent(campaignId, nodeId), offer);

        /// <summary>Convenience overload for conversions (see <see cref="TrackOfferClicked(string,string,Offer)"/>).</summary>
        public static void TrackOfferConverted(string campaignId, string nodeId, Offer offer = null) =>
            TrackOfferFunnel("Converted", BuildIntent(campaignId, nodeId), offer);

        private static NotificationIntentData BuildIntent(string campaignId, string nodeId) =>
            new NotificationIntentData { CampaignId = campaignId, NodeId = nodeId };

        // Builds the §4.6 funnel CoreEvent and sends it through the player's analytics service.
        private static void TrackOfferFunnel(string funnelType, NotificationIntentData campaign, Offer offer)
        {
            if (campaign == null || !campaign.IsTrackedCampaign)
            {
                Debug.LogWarning("[BeamableNotifications] TrackOffer" + funnelType +
                                 " ignored: intent data is not a tracked campaign (campaignId + nodeId required).");
                return;
            }

            // Single offer relevant to this event (§4.6): the explicit one, else the first carried.
            Offer effectiveOffer = offer;
            if (effectiveOffer == null && campaign.Offers != null && campaign.Offers.Count > 0)
                effectiveOffer = campaign.Offers[0];

            var p = new Dictionary<string, object>();
            if (campaign.CampaignId != null) p["campaignId"] = campaign.CampaignId;
            if (campaign.NodeId != null) p["nodeId"] = campaign.NodeId;
            if (campaign.GamerTag != null) p["gamerTag"] = campaign.GamerTag;
            if (campaign.AccountId != null) p["accountId"] = campaign.AccountId;
            if (campaign.CidPid != null) p["cidPid"] = campaign.CidPid;
            if (effectiveOffer != null) p["offerData"] = JsonSerializable.Serialize(effectiveOffer);
            if (campaign.Deeplink != null) p["deeplink"] = campaign.Deeplink;
            p["funnelType"] = funnelType;

            try
            {
                var analytics = ResolveAnalyticsService();
                if (analytics == null)
                {
                    Debug.LogWarning("[BeamableNotifications] No IBeamAnalyticsService on the Beamable context; " +
                                     "offer " + funnelType + " event was not sent.");
                    return;
                }
                var coreEvent = new CoreEvent(FunnelCategory, funnelType, p);
                analytics.SendAnalyticsEvent(analytics.BuildRequest(coreEvent));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BeamableNotifications] Failed to send offer " + funnelType +
                                 " analytics event: " + e.Message);
            }
        }

        // Pulls the analytics service off the default Beamable context. Lives in Unity.Beamable
        // (BeamContext) + Unity.Beamable.Runtime.Common (IBeamAnalyticsService); both are referenced
        // by this assembly (see Beamable.Notifications.asmdef).
        private static IBeamAnalyticsService ResolveAnalyticsService()
        {
            var ctx = BeamContext.Default;
            return ctx?.ServiceProvider?.GetService<IBeamAnalyticsService>();
        }

        // MARK: Event raisers (used by the Android relay; iOS uses the trampolines below)

        internal static void RaisePermissionResult(PermissionResult r) => OnPermissionResult?.Invoke(r);
        internal static void RaiseTokenReceived(string t) => OnTokenReceived?.Invoke(t);
        internal static void RaiseTokenError(string e) => OnTokenError?.Invoke(e);
        internal static void RaiseNotificationPresented(NotificationData d) => OnNotificationPresented?.Invoke(d);
        internal static void RaiseNotificationReceived(NotificationData d) => OnNotificationReceived?.Invoke(d);
        internal static void RaiseNotificationTapped(NotificationData d) => OnNotificationTapped?.Invoke(d);
        internal static void RaisePendingNotifications(List<NotificationData> l) => OnPendingNotifications?.Invoke(l);
        internal static void RaiseDeliveryReceipts(List<DeliveryReceipt> l) => OnDeliveryReceipts?.Invoke(l);

        // MARK: Native callback trampolines (iOS; static, AOT-safe)

        private static readonly Native.BMNCallback _permission = Permission;
        private static readonly Native.BMNCallback _tokenReceived = TokenReceived;
        private static readonly Native.BMNCallback _tokenError = TokenError;
        private static readonly Native.BMNCallback _presented = Presented;
        private static readonly Native.BMNCallback _received = Received;
        private static readonly Native.BMNCallback _tapped = Tapped;
        private static readonly Native.BMNCallback _pending = Pending;
        private static readonly Native.BMNCallback _receipts = Receipts;

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Permission(string json)
        {
            var data = JsonSerializable.FromJson<PermissionResult>(json);
            Dispatcher.Run(() => RaisePermissionResult(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenReceived(string json)
        {
            var token = JsonSerializable.FromJson<TokenWrapper>(json)?.token;
            Dispatcher.Run(() => RaiseTokenReceived(token));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenError(string json)
        {
            var error = JsonSerializable.FromJson<ErrorWrapper>(json)?.error;
            Dispatcher.Run(() => RaiseTokenError(error));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Presented(string json)
        {
            var data = JsonSerializable.FromJson<NotificationData>(json);
            Dispatcher.Run(() => RaiseNotificationPresented(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Received(string json)
        {
            var data = JsonSerializable.FromJson<NotificationData>(json);
            Dispatcher.Run(() => RaiseNotificationReceived(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Tapped(string json)
        {
            var data = JsonSerializable.FromJson<NotificationData>(json);
            Dispatcher.Run(() => RaiseNotificationTapped(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Pending(string json)
        {
            var list = NotificationJson.FromJsonArray<NotificationData>(json);
            Dispatcher.Run(() => RaisePendingNotifications(list));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Receipts(string json)
        {
            var list = NotificationJson.FromJsonArray<DeliveryReceipt>(json);
            Dispatcher.Run(() => RaiseDeliveryReceipts(list));
        }

        private class TokenWrapper : JsonSerializable.ISerializable
        {
            public string token;
            public void Serialize(JsonSerializable.IStreamSerializer s) => s.Serialize("token", ref token);
        }

        private class ErrorWrapper : JsonSerializable.ISerializable
        {
            public string error;
            public void Serialize(JsonSerializable.IStreamSerializer s) => s.Serialize("error", ref error);
        }
    }
}
