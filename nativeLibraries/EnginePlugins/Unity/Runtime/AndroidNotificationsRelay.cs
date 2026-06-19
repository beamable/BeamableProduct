// Android plumbing for the shared BeamableNotifications API.
//
// Outbound (C# -> native): AndroidBackend calls the Kotlin @JvmStatic facade
// `com.beamable.push.unity.UnityNotifications` with the SAME serialized DTO JSON the iOS path
// uses, so one C# call site serves both platforms.
//
// Inbound (native -> C#): the Kotlin bridge calls UnityPlayer.UnitySendMessage targeting the
// GameObject hosting AndroidNotificationsRelay, whose On* methods mirror the iOS callback names.
// UnitySendMessage delivers on the Unity main thread, so events are raised directly.
//
// The whole file is Android-player-only; iOS/editor use the other branches in BeamableNotifications.
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Notifications
{
    internal static class AndroidBackend
    {
        internal const string GameObjectName = "BeamableNotifications";
        private const string FacadeClass = "com.beamable.push.unity.UnityNotifications";

        private static AndroidJavaClass _facade;
        private static AndroidJavaClass Facade => _facade ?? (_facade = new AndroidJavaClass(FacadeClass));

        internal static void Initialize() => Facade.CallStatic("initialize", GameObjectName);
        internal static void Call(string method) => Facade.CallStatic(method);
        internal static void Call(string method, params object[] args) => Facade.CallStatic(method, args);
        internal static string CallStr(string method) => Facade.CallStatic<string>(method);
    }

    /// <summary>
    /// Receives native Android callbacks via UnitySendMessage and raises the shared
    /// <see cref="BeamableNotifications"/> events. Method names mirror the iOS callback set.
    /// Also relays app focus/pause so the native side reports foreground state correctly.
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

        // ---- UnitySendMessage targets (called from the Kotlin UnityNotificationsBridge) ----

        public void OnPermissionResult(string json)
            => BeamableNotifications.RaisePermissionResult(Json.Deserialize<PermissionResult>(json));

        public void OnTokenReceived(string json)
            => BeamableNotifications.RaiseTokenReceived(Json.Deserialize<TokenMsg>(json)?.token);

        public void OnTokenError(string json)
            => BeamableNotifications.RaiseTokenError(Json.Deserialize<ErrorMsg>(json)?.error);

        public void OnNotificationPresented(string json)
            => BeamableNotifications.RaiseNotificationPresented(Json.Deserialize<NotificationData>(json));

        public void OnNotificationReceived(string json)
            => BeamableNotifications.RaiseNotificationReceived(Json.Deserialize<NotificationData>(json));

        public void OnNotificationTapped(string json)
            => BeamableNotifications.RaiseNotificationTapped(Json.Deserialize<NotificationData>(json));

        public void OnPendingNotifications(string json)
            => BeamableNotifications.RaisePendingNotifications(Json.Deserialize<List<NotificationData>>(json));

        public void OnDeliveryReceipts(string json)
            => BeamableNotifications.RaiseDeliveryReceipts(Json.Deserialize<List<DeliveryReceipt>>(json));

        private class TokenMsg { public string token; }
        private class ErrorMsg { public string error; }
    }
}
#endif
