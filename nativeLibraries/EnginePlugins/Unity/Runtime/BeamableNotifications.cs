using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

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
            string json = Json.Serialize(options ?? new PermissionOptions());
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
            string json = Json.Serialize(request);
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
            string json = Json.Serialize(config);
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_configureAnalytics(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("configureAnalytics", json);
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
            string json = Json.Serialize(template);
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_registerTemplate(json);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("registerTemplate", json);
#endif
        }

        public static void RegisterCategory(CategorySpec category)
        {
            string json = Json.Serialize(category);
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
                return string.IsNullOrEmpty(json) ? null : Json.Deserialize<NotificationData>(json);
            }
            finally
            {
                Native.bmn_free(ptr);
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            string json = AndroidBackend.CallStr("getLaunchNotification");
            return string.IsNullOrEmpty(json) ? null : Json.Deserialize<NotificationData>(json);
#else
            return null;
#endif
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
            var data = Json.Deserialize<PermissionResult>(json);
            Dispatcher.Run(() => RaisePermissionResult(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenReceived(string json)
        {
            var token = Json.Deserialize<TokenWrapper>(json)?.token;
            Dispatcher.Run(() => RaiseTokenReceived(token));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenError(string json)
        {
            var error = Json.Deserialize<ErrorWrapper>(json)?.error;
            Dispatcher.Run(() => RaiseTokenError(error));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Presented(string json)
        {
            var data = Json.Deserialize<NotificationData>(json);
            Dispatcher.Run(() => RaiseNotificationPresented(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Received(string json)
        {
            var data = Json.Deserialize<NotificationData>(json);
            Dispatcher.Run(() => RaiseNotificationReceived(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Tapped(string json)
        {
            var data = Json.Deserialize<NotificationData>(json);
            Dispatcher.Run(() => RaiseNotificationTapped(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Pending(string json)
        {
            var list = Json.Deserialize<List<NotificationData>>(json);
            Dispatcher.Run(() => RaisePendingNotifications(list));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Receipts(string json)
        {
            var list = Json.Deserialize<List<DeliveryReceipt>>(json);
            Dispatcher.Run(() => RaiseDeliveryReceipts(list));
        }

        [Serializable] private class TokenWrapper { public string token; }
        [Serializable] private class ErrorWrapper { public string error; }
    }
}
