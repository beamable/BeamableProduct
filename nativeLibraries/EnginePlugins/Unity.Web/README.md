# Beamable Notifications — Web / WebView (`com.beamable.notifications.web`)

Lets a web / React UI running **inside a Unity WebView** drive the Beamable **native**
notifications binary on iOS and Android — real APNs/FCM tokens, OS notification permission,
scheduled local notifications, deep links, closed-app analytics — even though the page itself is
a sandboxed browser with no OS access.

## Why this package (and not `com.beamable.notifications`)

When the game UI is a web app in a WebView, the page uses the **Beamable Web SDK**
(`@beamable/sdk`), not the Beamable **Unity** SDK. So this package has **zero dependency on
`com.beamable`** — its runtime assembly (`Beamable.Notifications.Web`) references nothing. It is a
**thin JSON relay**: page args go to the native binary verbatim, native events come back to the
page verbatim. No typed C# DTOs, no `BeamContext`, no Unity-SDK serialization.

(The sibling `com.beamable.notifications` package is for native Unity-SDK games and *does* depend
on `com.beamable`. Both ship the same native `.aar` / `.xcframework`; pick the one that matches how
your game is built.)

```
Web page JS  ──window.Unity.call──►  BeamableWebViewBridge  ──►  NativeNotifications  ──►  .aar / .xcframework
             ◄─window.onUnityMessage─                        ◄──  (raw JSON, both directions)
```

## Requirements

- A Unity WebView plugin of your choice (not bundled): gree/unity-webview, Vuplex, uniwebview, …
- The native binaries staged into `Plugins/` (Android `.aar` is committed; iOS `.xcframework` is
  produced by `dev-native.sh`).

## Integration (the whole thing)

`BeamableWebViewBridge` is configured with **one delegate** (how to run JS in your page) and fed
inbound messages through **one method** (`OnPageMessage`). No interface to implement, no
MonoBehaviour to subclass.

```csharp
using Beamable.Notifications.Web;

var bridge = new BeamableWebViewBridge(evaluateJs: js => webView.EvaluateJS(js)); // 1. how to run JS
bridge.Start();                                                                   // 2. init native + forward events
// 3. point your plugin's message callback at OnPageMessage (Unity main thread)
// 4. load your page (bundled locally via StreamingAssetsServer, or any URL)
```

### `window.Unity` shim

The page calls `window.Unity.call(msg)`. Some plugins expose that natively; others need a one-line
shim injected **after each page load**. The bridge provides the JS strings:

| Your WebView | Inbound (page→Unity) | Shim to inject on page load |
|---|---|---|
| gree — Android | native `window.Unity` (JavascriptInterface) | none |
| gree — iOS/macOS (WKWebView) | `webkit.messageHandlers` | `BeamableWebViewBridge.WkWebViewShim("unityControl")` |
| Vuplex | `window.vuplex.postMessage` | `BeamableWebViewBridge.PassthroughShim("window.vuplex.postMessage")` |
| uniwebview | `uniwebview://` URL scheme | `BeamableWebViewBridge.LocationSchemeShim("uniwebview://msg?data=")` |

### Example: gree/unity-webview

```csharp
using Beamable.Notifications.Web;
using Gree.UnityWebView;
using UnityEngine;

public class NotificationsWebView : MonoBehaviour
{
    private WebViewObject _webView;
    private BeamableWebViewBridge _bridge;

    void Start()
    {
        _webView = new GameObject("WebViewObject").AddComponent<WebViewObject>();
        _bridge = new BeamableWebViewBridge(js => _webView.EvaluateJS(js));
        _bridge.Start();

        _webView.Init(
            cb: _bridge.OnPageMessage,                       // inbound: page → Unity
            ld: _ =>
            {
#if UNITY_EDITOR_OSX || (!UNITY_ANDROID && !UNITY_WEBGL)
                _webView.EvaluateJS(BeamableWebViewBridge.WkWebViewShim("unityControl")); // iOS/macOS only
#endif
            },
            enableWKWebView: true);

        _webView.SetVisibility(true);
        _webView.LoadURL("http://127.0.0.1:17890/");         // e.g. StreamingAssetsServer.BaseUrl
    }

    void OnDestroy() => _bridge?.Dispose();
}
```

### Example: Vuplex

```csharp
var bridge = new BeamableWebViewBridge(js => webView.ExecuteJavaScript(js));
bridge.Start();
webView.MessageEmitted += (s, e) => bridge.OnPageMessage(e.Value);
webView.LoadProgressChanged += (s, e) =>
{
    if (e.Type == ProgressChangeType.Finished)
        webView.ExecuteJavaScript(BeamableWebViewBridge.PassthroughShim("window.vuplex.postMessage"));
};
```

### Example: uniwebview

```csharp
var bridge = new BeamableWebViewBridge(js => webView.EvaluateJavaScript(js, null));
bridge.Start();
webView.OnMessageReceived += (view, msg) => bridge.OnPageMessage(msg.RawMessage); // decode your scheme
webView.OnPageFinished += (view, code, url) =>
    webView.EvaluateJavaScript(BeamableWebViewBridge.LocationSchemeShim("uniwebview://msg?data="), null);
```

## Serving the web bundle (optional)

`StreamingAssetsServer` serves a static web bundle from `Assets/StreamingAssets/react` over
`http://127.0.0.1:17890/` (proper http origin; works in Editor, iOS, and Android). Or skip it and
point your WebView at any URL.

```csharp
var server = gameObject.AddComponent<StreamingAssetsServer>();
yield return server.StartServer();
_webView.LoadURL(server.BaseUrl);
```

Export the web bundle into StreamingAssets with the shipped script (run from your Unity project root):

```bash
/path/to/com.beamable.notifications.web/Tools/build-react-webview.sh
# or explicitly:
RN_SAMPLE_DIR=/path/to/ExpoApp UNITY_PROJECT_DIR=/path/to/UnityProject \
  /path/to/.../Tools/build-react-webview.sh
```

## Auth & analytics

The bridge does **not** wire auth to a Unity `BeamContext` (there isn't one). The **page** drives
the closed-app campaign funnel by sending a `configureAuth` call with the Web SDK's player tokens
(`accessToken`, `refreshToken`, `accessTokenExpiresAt`, `cid`, `pid`, `host`), which is forwarded to
native verbatim. Offer-funnel analytics are likewise sent by the page via the Web SDK, so there is
no `trackOffer*` on the Unity side.

## Bridge protocol (reference)

- page → Unity (into `OnPageMessage`): `{type:'ready'}`, `{type:'call', method, args?, id?}`
- Unity → page (via `evaluateJs` → `window.onUnityMessage`): `{type:'platform', os, isEditor, nativeSupported}`, `{type:'event', name, payload}`, `{type:'result', id, ok, payload|error}`
