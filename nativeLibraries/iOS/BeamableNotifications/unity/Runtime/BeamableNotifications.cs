using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace Beamable.Notifications
{
    /// <summary>
    /// Unity entry point for the BeamableNotifications iOS SDK. Call <see cref="Initialize"/>
    /// once at startup, subscribe to the events you care about, then drive the API.
    /// All events are raised on the Unity main thread.
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
        }

        // MARK: API

        public static void RequestPermission(PermissionOptions options = null)
            => Native.bmn_requestPermission(Json.Serialize(options ?? new PermissionOptions()));

        public static void GetPermissionStatus() => Native.bmn_getPermissionStatus();

        public static void ScheduleLocal(LocalRequest request)
            => Native.bmn_scheduleLocal(Json.Serialize(request));

        public static void CancelLocal(string id) => Native.bmn_cancelLocal(id);
        public static void CancelAllLocal() => Native.bmn_cancelAllLocal();
        public static void GetPending() => Native.bmn_getPending();

        public static void RegisterForRemote() => Native.bmn_registerForRemote();
        public static void UnregisterForRemote() => Native.bmn_unregisterForRemote();

        public static void ConfigureAnalytics(AnalyticsConfig config)
            => Native.bmn_configureAnalytics(Json.Serialize(config));

        public static void GetDeliveryReceipts() => Native.bmn_getDeliveryReceipts();

        public static void RegisterTemplate(TemplateSpec template)
            => Native.bmn_registerTemplate(Json.Serialize(template));

        public static void RegisterCategory(CategorySpec category)
            => Native.bmn_registerCategory(Json.Serialize(category));

        public static void SetBadge(int count) => Native.bmn_setBadge(count);
        public static void ClearDelivered() => Native.bmn_clearDelivered();

        /// <summary>The notification that launched the app (feature 6, "get intent"), or null.</summary>
        public static NotificationData GetLaunchNotification()
        {
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
        }

        // MARK: Native callback trampolines (static, AOT-safe)

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
            Dispatcher.Run(() => OnPermissionResult?.Invoke(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenReceived(string json)
        {
            var token = Json.Deserialize<TokenWrapper>(json)?.token;
            Dispatcher.Run(() => OnTokenReceived?.Invoke(token));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenError(string json)
        {
            var error = Json.Deserialize<ErrorWrapper>(json)?.error;
            Dispatcher.Run(() => OnTokenError?.Invoke(error));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Presented(string json)
        {
            var data = Json.Deserialize<NotificationData>(json);
            Dispatcher.Run(() => OnNotificationPresented?.Invoke(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Received(string json)
        {
            var data = Json.Deserialize<NotificationData>(json);
            Dispatcher.Run(() => OnNotificationReceived?.Invoke(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Tapped(string json)
        {
            var data = Json.Deserialize<NotificationData>(json);
            Dispatcher.Run(() => OnNotificationTapped?.Invoke(data));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Pending(string json)
        {
            var list = Json.Deserialize<List<NotificationData>>(json);
            Dispatcher.Run(() => OnPendingNotifications?.Invoke(list));
        }

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Receipts(string json)
        {
            var list = Json.Deserialize<List<DeliveryReceipt>>(json);
            Dispatcher.Run(() => OnDeliveryReceipts?.Invoke(list));
        }

        [Serializable] private class TokenWrapper { public string token; }
        [Serializable] private class ErrorWrapper { public string error; }
    }
}
