using System;
using System.Runtime.InteropServices;

namespace Beamable.Notifications
{
    // P/Invoke bindings to the BeamableNotifications C ABI (see BeamableNotifications.h).
    // On iOS the symbols are statically linked, hence "__Internal". Static callbacks are
    // tagged [MonoPInvokeCallback] so they survive IL2CPP/AOT, and are kept alive by the
    // static delegate fields in NotificationBridge.
    internal static class Native
    {
        internal const string LIB = "__Internal";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void BMNCallback([MarshalAs(UnmanagedType.LPStr)] string json);

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport(LIB)] internal static extern void bmn_initialize();
        [DllImport(LIB)] internal static extern void bmn_requestPermission(string optionsJson);
        [DllImport(LIB)] internal static extern void bmn_getPermissionStatus();
        [DllImport(LIB)] internal static extern void bmn_scheduleLocal(string requestJson);
        [DllImport(LIB)] internal static extern void bmn_cancelLocal(string id);
        [DllImport(LIB)] internal static extern void bmn_cancelAllLocal();
        [DllImport(LIB)] internal static extern void bmn_getPending();
        [DllImport(LIB)] internal static extern void bmn_registerForRemote();
        [DllImport(LIB)] internal static extern void bmn_unregisterForRemote();
        [DllImport(LIB)] internal static extern void bmn_configureAnalytics(string configJson);
        [DllImport(LIB)] internal static extern void bmn_getDeliveryReceipts();
        [DllImport(LIB)] internal static extern void bmn_registerTemplate(string templateJson);
        [DllImport(LIB)] internal static extern void bmn_registerCategory(string categoryJson);
        [DllImport(LIB)] internal static extern void bmn_setBadge(int count);
        [DllImport(LIB)] internal static extern void bmn_clearDelivered();
        [DllImport(LIB)] internal static extern IntPtr bmn_getLaunchNotification();
        [DllImport(LIB)] internal static extern void bmn_free(IntPtr ptr);

        [DllImport(LIB)] internal static extern void bmn_setOnPermissionResult(BMNCallback cb);
        [DllImport(LIB)] internal static extern void bmn_setOnTokenReceived(BMNCallback cb);
        [DllImport(LIB)] internal static extern void bmn_setOnTokenError(BMNCallback cb);
        [DllImport(LIB)] internal static extern void bmn_setOnNotificationPresented(BMNCallback cb);
        [DllImport(LIB)] internal static extern void bmn_setOnNotificationReceived(BMNCallback cb);
        [DllImport(LIB)] internal static extern void bmn_setOnNotificationTapped(BMNCallback cb);
        [DllImport(LIB)] internal static extern void bmn_setOnPendingNotifications(BMNCallback cb);
        [DllImport(LIB)] internal static extern void bmn_setOnDeliveryReceipts(BMNCallback cb);
#else
        // Editor / non-iOS stubs so the project compiles and runs on the desktop.
        internal static void bmn_initialize() { }
        internal static void bmn_requestPermission(string optionsJson) { }
        internal static void bmn_getPermissionStatus() { }
        internal static void bmn_scheduleLocal(string requestJson) { }
        internal static void bmn_cancelLocal(string id) { }
        internal static void bmn_cancelAllLocal() { }
        internal static void bmn_getPending() { }
        internal static void bmn_registerForRemote() { }
        internal static void bmn_unregisterForRemote() { }
        internal static void bmn_configureAnalytics(string configJson) { }
        internal static void bmn_getDeliveryReceipts() { }
        internal static void bmn_registerTemplate(string templateJson) { }
        internal static void bmn_registerCategory(string categoryJson) { }
        internal static void bmn_setBadge(int count) { }
        internal static void bmn_clearDelivered() { }
        internal static IntPtr bmn_getLaunchNotification() { return IntPtr.Zero; }
        internal static void bmn_free(IntPtr ptr) { }

        internal static void bmn_setOnPermissionResult(BMNCallback cb) { }
        internal static void bmn_setOnTokenReceived(BMNCallback cb) { }
        internal static void bmn_setOnTokenError(BMNCallback cb) { }
        internal static void bmn_setOnNotificationPresented(BMNCallback cb) { }
        internal static void bmn_setOnNotificationReceived(BMNCallback cb) { }
        internal static void bmn_setOnNotificationTapped(BMNCallback cb) { }
        internal static void bmn_setOnPendingNotifications(BMNCallback cb) { }
        internal static void bmn_setOnDeliveryReceipts(BMNCallback cb) { }
#endif
    }
}
