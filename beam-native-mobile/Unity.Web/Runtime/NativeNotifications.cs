using System;
using System.Runtime.InteropServices;
#if UNITY_IOS && !UNITY_EDITOR
using AOT;
#endif
using UnityEngine;

namespace Beamable.Notifications.Web
{
    /// <summary>
    /// Thin JSON-string native facade used by <see cref="BeamableWebViewBridge"/>. It drives the
    /// Beamable notifications native binary — on iOS the <c>bmn_*</c> C ABI (see <see cref="Native"/>),
    /// on Android the Kotlin <c>com.beamable.push.unity.UnityNotifications</c> facade — and raises
    /// native events back as <b>raw JSON strings</b> tagged with the RN-vocabulary event names the web
    /// page expects (<c>permissionResult</c>, <c>tokenReceived</c>, <c>tokenError</c>,
    /// <c>notificationPresented</c>, <c>notificationReceived</c>, <c>notificationOpened</c>,
    /// <c>pendingNotifications</c>, <c>deliveryReceipts</c>).
    ///
    /// Unlike the Unity-SDK package, there are no typed DTOs and no JsonSerializable: page args are
    /// forwarded to native verbatim and native payloads are forwarded to the page verbatim. This is
    /// the whole reason the package has zero dependency on <c>com.beamable</c>.
    /// </summary>
    internal static class NativeNotifications
    {
        /// <summary>Raised on the Unity main thread: (eventName, payloadJson) to forward to the page.</summary>
        internal static event Action<string, string> OnNativeEvent;

        private static bool _initialized;

        internal static void Emit(string name, string json) => OnNativeEvent?.Invoke(name, json ?? "null");

        internal static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            Dispatcher.Ensure();

#if UNITY_IOS && !UNITY_EDITOR
            // Delegate instances are held in static fields (below) so the GC cannot collect them
            // while native code holds the pointers.
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
            AndroidNotificationsRelay.Ensure();
            AndroidBackend.Initialize();
#endif
        }

        /// <summary>
        /// Invoke a native method whose sole argument is a JSON string, or which takes no argument.
        /// (Methods with non-JSON argument types have dedicated entry points: <see cref="CancelLocal"/>,
        /// <see cref="SetBadge"/>, <see cref="GetLaunchNotification"/>.)
        /// </summary>
        internal static void CallJson(string method, string argsJson)
        {
#if UNITY_IOS && !UNITY_EDITOR
            switch (method)
            {
                case "initialize": Native.bmn_initialize(); break;
                case "requestPermission": Native.bmn_requestPermission(argsJson ?? "{}"); break;
                case "getPermissionStatus": Native.bmn_getPermissionStatus(); break;
                case "scheduleLocal": Native.bmn_scheduleLocal(argsJson ?? "{}"); break;
                case "cancelAllLocal": Native.bmn_cancelAllLocal(); break;
                case "getPending": Native.bmn_getPending(); break;
                case "registerForRemote": Native.bmn_registerForRemote(); break;
                case "unregisterForRemote": Native.bmn_unregisterForRemote(); break;
                case "getDeliveryReceipts": Native.bmn_getDeliveryReceipts(); break;
                case "registerTemplate": Native.bmn_registerTemplate(argsJson ?? "{}"); break;
                case "registerCategory": Native.bmn_registerCategory(argsJson ?? "{}"); break;
                case "clearDelivered": Native.bmn_clearDelivered(); break;
                case "configureAuth": Native.bmn_configureAuth(argsJson ?? "{}"); break;
                case "clearAuth": Native.bmn_clearAuth(); break;
                default: Debug.LogWarning($"[NativeNotifications] Unknown iOS method '{method}'"); break;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            // The Kotlin facade method names match the page's method names 1:1.
            if (argsJson != null) AndroidBackend.Call(method, argsJson);
            else AndroidBackend.Call(method);
#endif
        }

        internal static void CancelLocal(string id)
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_cancelLocal(id);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("cancelLocal", id);
#endif
        }

        internal static void SetBadge(int count)
        {
#if UNITY_IOS && !UNITY_EDITOR
            Native.bmn_setBadge(count);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidBackend.Call("setBadge", count);
#endif
        }

        /// <summary>The notification that launched the app as raw JSON, or null.</summary>
        internal static string GetLaunchNotification()
        {
#if UNITY_IOS && !UNITY_EDITOR
            IntPtr ptr = Native.bmn_getLaunchNotification();
            if (ptr == IntPtr.Zero) return null;
            try { return Marshal.PtrToStringAnsi(ptr); }
            finally { Native.bmn_free(ptr); }
#elif UNITY_ANDROID && !UNITY_EDITOR
            return AndroidBackend.CallStr("getLaunchNotification");
#else
            return null;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        // Native callback trampolines (static + AOT-safe). Each forwards the raw JSON to the page,
        // renaming only the native `tapped` event to the page's `notificationOpened`.
        private static readonly Native.BMNCallback _permission = Permission;
        private static readonly Native.BMNCallback _tokenReceived = TokenReceived;
        private static readonly Native.BMNCallback _tokenError = TokenError;
        private static readonly Native.BMNCallback _presented = Presented;
        private static readonly Native.BMNCallback _received = Received;
        private static readonly Native.BMNCallback _tapped = Tapped;
        private static readonly Native.BMNCallback _pending = Pending;
        private static readonly Native.BMNCallback _receipts = Receipts;

        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Permission(string json) => Dispatcher.Run(() => Emit("permissionResult", json));
        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenReceived(string json) => Dispatcher.Run(() => Emit("tokenReceived", json));
        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void TokenError(string json) => Dispatcher.Run(() => Emit("tokenError", json));
        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Presented(string json) => Dispatcher.Run(() => Emit("notificationPresented", json));
        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Received(string json) => Dispatcher.Run(() => Emit("notificationReceived", json));
        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Tapped(string json) => Dispatcher.Run(() => Emit("notificationOpened", json));
        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Pending(string json) => Dispatcher.Run(() => Emit("pendingNotifications", json));
        [MonoPInvokeCallback(typeof(Native.BMNCallback))]
        private static void Receipts(string json) => Dispatcher.Run(() => Emit("deliveryReceipts", json));
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>Outbound C# → Kotlin facade (com.beamable.push.unity.UnityNotifications).</summary>
    internal static class AndroidBackend
    {
        // A distinct GameObject name from the Unity-SDK package so the two can coexist if both installed.
        internal const string GameObjectName = "BeamableNotificationsWeb";
        private const string FacadeClass = "com.beamable.push.unity.UnityNotifications";

        private static AndroidJavaClass _facade;
        private static AndroidJavaClass Facade => _facade ?? (_facade = new AndroidJavaClass(FacadeClass));

        internal static void Initialize() => Facade.CallStatic("initialize", GameObjectName);
        internal static void Call(string method) => Facade.CallStatic(method);
        internal static void Call(string method, params object[] args) => Facade.CallStatic(method, args);
        internal static string CallStr(string method) => Facade.CallStatic<string>(method);
    }

    /// <summary>
    /// Receives native Android callbacks via UnitySendMessage (delivered on the main thread) and
    /// forwards the raw JSON to the page under the RN-vocabulary event names. Method names here MUST
    /// match those the Kotlin bridge invokes (they mirror the iOS callback set).
    /// </summary>
    internal sealed class AndroidNotificationsRelay : MonoBehaviour
    {
        private static AndroidNotificationsRelay _instance;

        internal static void Ensure()
        {
            if (_instance != null) return;
            // Name MUST match AndroidBackend.GameObjectName — the native bridge targets it.
            var go = new GameObject(AndroidBackend.GameObjectName);
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AndroidNotificationsRelay>();
        }

        private void OnApplicationFocus(bool hasFocus) => AndroidBackend.Call("setForeground", hasFocus);
        private void OnApplicationPause(bool paused) => AndroidBackend.Call("setForeground", !paused);

        public void OnPermissionResult(string json) => NativeNotifications.Emit("permissionResult", json);
        public void OnTokenReceived(string json) => NativeNotifications.Emit("tokenReceived", json);
        public void OnTokenError(string json) => NativeNotifications.Emit("tokenError", json);
        public void OnNotificationPresented(string json) => NativeNotifications.Emit("notificationPresented", json);
        public void OnNotificationReceived(string json) => NativeNotifications.Emit("notificationReceived", json);
        public void OnNotificationTapped(string json) => NativeNotifications.Emit("notificationOpened", json);
        public void OnPendingNotifications(string json) => NativeNotifications.Emit("pendingNotifications", json);
        public void OnDeliveryReceipts(string json) => NativeNotifications.Emit("deliveryReceipts", json);
    }
#endif
}
