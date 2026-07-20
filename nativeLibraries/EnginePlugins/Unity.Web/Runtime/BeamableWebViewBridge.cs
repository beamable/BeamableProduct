using System;
using System.Collections.Generic;
using System.Text;
using Beamable.Notifications.Web.Serialization;
using UnityEngine;

namespace Beamable.Notifications.Web
{
    /// <summary>
    /// WebView-agnostic bridge between a web/React UI running inside a Unity WebView and the Beamable
    /// notifications native binary. It owns the JSON envelope protocol and relays page calls to
    /// <see cref="NativeNotifications"/> — as a thin pass-through: page args go to native verbatim and
    /// native event JSON goes to the page verbatim. It knows nothing about any specific WebView plugin,
    /// and has zero dependency on the Beamable Unity SDK.
    ///
    /// Wire it to your WebView with two things only:
    ///  - <b>outbound</b>: the <c>evaluateJs</c> delegate passed to the constructor (the bridge calls it
    ///    to run <c>window.onUnityMessage(...)</c> in the page);
    ///  - <b>inbound</b>: call <see cref="OnPageMessage"/> from your plugin's page→Unity callback.
    ///
    /// The page contract is just two globals — <c>window.Unity.call(msg)</c> and
    /// <c>window.onUnityMessage(msg)</c> — exactly what the web build's <c>src/unity/unityBridge.ts</c>
    /// already speaks, so no web-side changes are needed. Where a plugin does not expose
    /// <c>window.Unity</c> natively, inject one of <see cref="WkWebViewShim"/>/
    /// <see cref="LocationSchemeShim"/>/<see cref="PassthroughShim"/> via its page-loaded hook.
    ///
    /// Protocol (JSON strings):
    ///  - page → Unity (into <see cref="OnPageMessage"/>): {type:'ready'} | {type:'call', method, args?, id?}
    ///  - Unity → page: {type:'platform', …} | {type:'event', name, payload} | {type:'result', id, ok, payload|error}
    ///
    /// Call <see cref="OnPageMessage"/> on the Unity main thread (gree/Vuplex/uniwebview already do).
    /// </summary>
    public sealed class BeamableWebViewBridge : IDisposable
    {
        private readonly Action<string> _evaluateJs;
        private bool _started;
        private bool _disposed;
        private Action<string, string> _onNativeEvent;

        /// <param name="evaluateJs">Runs arbitrary JavaScript in the loaded page (used to invoke
        /// <c>window.onUnityMessage(...)</c>). Typically <c>js =&gt; webView.EvaluateJS(js)</c>.</param>
        public BeamableWebViewBridge(Action<string> evaluateJs)
        {
            _evaluateJs = evaluateJs ?? throw new ArgumentNullException(nameof(evaluateJs));
        }

        /// <summary>Initializes the native layer and forwards its events to the page. Idempotent.</summary>
        public void Start()
        {
            if (_started) return;
            _started = true;
            NativeNotifications.Initialize();
            _onNativeEvent = (name, payloadJson) => SendEvent(name, payloadJson);
            NativeNotifications.OnNativeEvent += _onNativeEvent;
        }

        // ── Inbound: page → Unity ───────────────────────────────────────────────

        /// <summary>Feed each raw string the page sent via <c>window.Unity.call</c> here (main thread).</summary>
        public void OnPageMessage(string msg)
        {
            if (_disposed) return;

            IDictionary<string, object> envelope = null;
            try { envelope = Json.Deserialize(msg) as IDictionary<string, object>; }
            catch { /* not JSON — a raw (non-protocol) message; ignore */ }

            var type = envelope != null && envelope.TryGetValue("type", out var t) ? t as string : null;
            switch (type)
            {
                case "ready":
                    SendPlatformInfo();
                    break;
                case "call":
                    HandleCall(envelope);
                    break;
                default:
                    Debug.Log($"[BeamableWebViewBridge] Ignoring non-protocol message: {msg}");
                    break;
            }
        }

        private void HandleCall(IDictionary<string, object> envelope)
        {
            var method = envelope.TryGetValue("method", out var m) ? m as string : null;
            var args = envelope.TryGetValue("args", out var a) ? a as IDictionary<string, object> : null;
            long? id = envelope.TryGetValue("id", out var i) && i != null ? Convert.ToInt64(i) : (long?)null;
            string argsJson = args != null ? Json.Serialize(args, new StringBuilder()) : null;

            try
            {
                switch (method)
                {
                    // Non-JSON argument types → dedicated entry points.
                    case "cancelLocal":
                        NativeNotifications.CancelLocal(Str(args, "id"));
                        break;
                    case "setBadge":
                        NativeNotifications.SetBadge(args != null && args.TryGetValue("count", out var c) ? Convert.ToInt32(c) : 0);
                        break;
                    case "getLaunchNotification":
                    {
                        var launch = NativeNotifications.GetLaunchNotification();
                        if (id.HasValue) SendResult(id.Value, true, string.IsNullOrEmpty(launch) ? "null" : launch);
                        return;
                    }

                    // No-argument methods.
                    case "initialize":
                    case "getPermissionStatus":
                    case "cancelAllLocal":
                    case "getPending":
                    case "registerForRemote":
                    case "unregisterForRemote":
                    case "getDeliveryReceipts":
                    case "clearDelivered":
                    case "clearAuth":
                        NativeNotifications.CallJson(method, null);
                        break;

                    // JSON-argument methods → forward args verbatim.
                    case "requestPermission":
                    case "scheduleLocal":
                    case "registerTemplate":
                    case "registerCategory":
                    case "configureAuth":
                        NativeNotifications.CallJson(method, argsJson);
                        break;

                    default:
                        if (id.HasValue) SendResult(id.Value, false, null, $"Unknown method '{method}'");
                        else Debug.LogWarning($"[BeamableWebViewBridge] Unknown bridge method '{method}'");
                        return;
                }
                if (id.HasValue) SendResult(id.Value, true, "null");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BeamableWebViewBridge] Bridge call '{method}' failed: {e}");
                if (id.HasValue) SendResult(id.Value, false, null, e.Message);
            }
        }

        private static string Str(IDictionary<string, object> dict, string key) =>
            dict != null && dict.TryGetValue(key, out var v) ? v as string : null;

        // ── Outbound: Unity → page ──────────────────────────────────────────────

        private void SendPlatformInfo()
        {
            string os;
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer: os = "ios"; break;
                case RuntimePlatform.Android: os = "android"; break;
                case RuntimePlatform.OSXEditor: os = "osx-editor"; break;
                case RuntimePlatform.WindowsEditor: os = "windows-editor"; break;
                case RuntimePlatform.OSXPlayer: os = "osx"; break;
                default: os = Application.platform.ToString().ToLowerInvariant(); break;
            }
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            const bool nativeSupported = true; // real .xcframework / .aar backing
#else
            const bool nativeSupported = false; // no-op in the Editor and on desktop
#endif
            SendToPage("{\"type\":\"platform\",\"os\":\"" + os + "\",\"isEditor\":" +
                       (Application.isEditor ? "true" : "false") + ",\"nativeSupported\":" +
                       (nativeSupported ? "true" : "false") + "}");
        }

        private void SendEvent(string name, string payloadJson)
        {
            SendToPage("{\"type\":\"event\",\"name\":\"" + name + "\",\"payload\":" + (payloadJson ?? "null") + "}");
        }

        private void SendResult(long id, bool ok, string payloadJson, string error = null)
        {
            SendToPage(ok
                ? "{\"type\":\"result\",\"id\":" + id + ",\"ok\":true,\"payload\":" + (payloadJson ?? "null") + "}"
                : "{\"type\":\"result\",\"id\":" + id + ",\"ok\":false,\"error\":" +
                  Json.Serialize(error ?? "unknown error", new StringBuilder()) + "}");
        }

        /// <summary>Unity → JS: delivers a protocol message to <c>window.onUnityMessage</c>.</summary>
        private void SendToPage(string message)
        {
            if (_disposed) return;
            var escaped = message
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
            _evaluateJs($"window.onUnityMessage && window.onUnityMessage('{escaped}');");
        }

        // ── window.Unity shim builders (optional; for plugins without a native window.Unity) ─────

        /// <summary>
        /// JS installing <c>window.Unity.call</c> for WKWebView-based plugins (e.g. gree on iOS/macOS),
        /// routing page messages to a registered <c>webkit.messageHandlers.&lt;handlerName&gt;</c> handler,
        /// with a <c>unity:</c> location fallback. Inject via your plugin's page-loaded hook.
        /// </summary>
        public static string WkWebViewShim(string handlerName) => @"
            if (window && window.webkit && window.webkit.messageHandlers
                && window.webkit.messageHandlers." + handlerName + @") {
              window.Unity = { call: function(msg) {
                window.webkit.messageHandlers." + handlerName + @".postMessage(msg); } };
            } else {
              window.Unity = { call: function(msg) {
                window.location = 'unity:' + msg; } };
            }";

        /// <summary>
        /// JS installing <c>window.Unity.call</c> for URL-scheme plugins (e.g. uniwebview). The payload
        /// is percent-encoded onto the URL. Pass a full scheme prefix such as <c>"uniwebview://msg?data="</c>.
        /// </summary>
        public static string LocationSchemeShim(string scheme) => @"
            window.Unity = { call: function(msg) {
              window.location = '" + scheme + @"' + encodeURIComponent(msg); } };";

        /// <summary>
        /// JS installing <c>window.Unity.call</c> by forwarding to an existing page-provided post function
        /// (e.g. Vuplex injects <c>window.vuplex.postMessage</c>). Pass the function reference as a string.
        /// </summary>
        public static string PassthroughShim(string globalPostFn) => @"
            window.Unity = { call: function(msg) { " + globalPostFn + @"(msg); } };";

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_onNativeEvent != null) NativeNotifications.OnNativeEvent -= _onNativeEvent;
        }
    }
}
