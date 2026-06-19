using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Beamable.Notifications;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// IMGUI test harness for the cross-platform <see cref="BeamableNotifications"/> API (same code on
/// iOS + Android) plus the Android-only <see cref="DeepLinkManager"/> (beamable:// VIEW capture).
/// Create the scene via Tools/Beamable/Android/Create Native Sample Scene.
/// </summary>
public class BeamableNativeSample : MonoBehaviour
{
    private const int MaxLogLines = 40;

    // Same webhook as the native DiscordWebhookPushHandler — to A/B test the network path from C#.
    private const string WebhookUrl =
        "https://canary.discord.com/api/webhooks/1517215291623608532/nuiDOyAaW3l4Ysn1UzUUAigEdH6SqrtECNNg3RkotAuoEELWkvwDiWmec_3zPH-eke7G";

    private readonly List<string> _log = new List<string>();
    private Vector2 _logScroll;
    private GUIStyle _header;

    private string _url = "beamable://order/42?ref=push&id=7";
    private NotificationData _lastNotification;
    private DeepLinkManager.DeepLinkData _lastDeepLink;
    private string _token;
    private bool _permResolved;
    private bool _permGranted;

    private void OnEnable()
    {
        BeamableNotifications.OnNotificationReceived += OnNotificationReceived;
        BeamableNotifications.OnNotificationTapped += OnNotificationTapped;
        BeamableNotifications.OnTokenReceived += OnToken;
        BeamableNotifications.OnTokenError += OnTokenError;
        BeamableNotifications.OnPermissionResult += OnPermission;
        DeepLinkManager.DeepLinkReceived += OnDeepLink;

        BeamableNotifications.Initialize();
        BeamableNotifications.RequestPermission(new PermissionOptions());
        Log("Initialize() + RequestPermission() called");
    }

    private void OnDisable()
    {
        BeamableNotifications.OnNotificationReceived -= OnNotificationReceived;
        BeamableNotifications.OnNotificationTapped -= OnNotificationTapped;
        BeamableNotifications.OnTokenReceived -= OnToken;
        BeamableNotifications.OnTokenError -= OnTokenError;
        BeamableNotifications.OnPermissionResult -= OnPermission;
        DeepLinkManager.DeepLinkReceived -= OnDeepLink;
    }

    // ---- Events ----

    private void OnNotificationReceived(NotificationData n)
    {
        _lastNotification = n;
        Log($"OnNotificationReceived id={n?.Id} deepLink={n?.DeepLink} wasLaunch={n?.WasLaunch}");
    }

    private void OnNotificationTapped(NotificationData n)
    {
        _lastNotification = n;
        Log($"OnNotificationTapped id={n?.Id} deepLink={n?.DeepLink}");
        if (!string.IsNullOrEmpty(n?.DeepLink) && DeepLinkManager.Instance != null)
            DeepLinkManager.Instance.ProcessDeepLink(n.DeepLink);
    }

    private void OnToken(string t) { _token = t; Log($"OnTokenReceived (len={t?.Length ?? 0})"); }
    private void OnTokenError(string e) { Log($"OnTokenError {e}"); }

    private void OnPermission(PermissionResult r)
    {
        _permResolved = true;
        _permGranted = r != null && r.Granted;
        Log($"OnPermissionResult status={r?.Status} granted={r?.Granted}");
    }

    private void OnDeepLink(DeepLinkManager.DeepLinkData d)
    {
        _lastDeepLink = d;
        Log($"DeepLinkReceived {d.RawUrl} scheme={d.Scheme} host={d.Host} path={d.Path}");
    }

    // ---- IMGUI ----

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.width / 540f);
        Matrix4x4 prev = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), Vector2.zero);
        float vw = Screen.width / scale;
        float vh = Screen.height / scale;

        if (_header == null)
            _header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

        GUILayout.BeginArea(new Rect(8, 8, vw - 16, vh - 16), GUI.skin.box);
        GUILayout.Label("Beamable Notifications Sample", _header);
        DrawStatus();
        GUILayout.Space(6);
        DrawControls();
        GUILayout.Space(6);
        DrawLog();
        GUILayout.EndArea();
        GUI.matrix = prev;
    }

    private void DrawStatus()
    {
        GUILayout.Label($"platform={Application.platform}");
        GUILayout.Label($"permission: {(_permResolved ? (_permGranted ? "granted" : "denied") : "pending")}");
        GUILayout.Label($"token: {(string.IsNullOrEmpty(_token) ? "(none / local-only)" : "…" + Tail(_token, 10))}");
        if (_lastNotification != null)
            GUILayout.Label($"last notif: id={_lastNotification.Id} deepLink={_lastNotification.DeepLink} wasLaunch={_lastNotification.WasLaunch}");
        if (_lastDeepLink != null)
            GUILayout.Label($"last deeplink: {Truncate(_lastDeepLink.RawUrl, 60)}");
    }

    private void DrawControls()
    {
        GUILayout.Label("Deep link URL:");
        _url = GUILayout.TextField(_url ?? string.Empty, GUILayout.Height(28));

        if (GUILayout.Button("Schedule local (5s, timeInterval)", GUILayout.Height(36)))
        {
            BeamableNotifications.ScheduleLocal(NewRequest(TriggerSpec.After(5)));
            Log("ScheduleLocal(After 5s) — background to see it fire");
        }

        if (GUILayout.Button("Schedule local (calendar +1min)", GUILayout.Height(36)))
        {
            var w = DateTime.Now.AddMinutes(1);
            var trigger = new TriggerSpec
            {
                Type = "calendar",
                Year = w.Year, Month = w.Month, Day = w.Day,
                Hour = w.Hour, Minute = w.Minute, Second = w.Second
            };
            BeamableNotifications.ScheduleLocal(NewRequest(trigger));
            Log($"ScheduleLocal(calendar {w:HH:mm:ss})");
        }

        if (GUILayout.Button("Cancel all", GUILayout.Height(32)))
        {
            BeamableNotifications.CancelAllLocal();
            Log("CancelAllLocal()");
        }

        if (GUILayout.Button("Get launch notification", GUILayout.Height(32)))
        {
            var n = BeamableNotifications.GetLaunchNotification();
            Log(n == null ? "GetLaunchNotification(): none" : $"launch notif id={n.Id} deepLink={n.DeepLink}");
        }

        if (GUILayout.Button("Register for remote (FCM token)", GUILayout.Height(32)))
        {
            BeamableNotifications.RegisterForRemote();
            Log("RegisterForRemote()");
        }

        if (GUILayout.Button("Process deep link (in-app)", GUILayout.Height(32)))
        {
            if (DeepLinkManager.Instance != null) DeepLinkManager.Instance.ProcessDeepLink(_url);
            Log($"ProcessDeepLink({Truncate(_url, 50)})");
        }

        if (GUILayout.Button("Send test webhook (C#)", GUILayout.Height(32)))
        {
            Log("Sending Discord webhook (C#/UnityWebRequest)...");
            StartCoroutine(SendDiscordWebhook("Unity: test webhook (C# in-app button)"));
        }

        if (GUILayout.Button("Clear log", GUILayout.Height(28)))
            _log.Clear();
    }

    private void DrawLog()
    {
        GUILayout.Label("Event log", _header);
        _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
        for (int i = _log.Count - 1; i >= 0; i--)
            GUILayout.Label(_log[i]);
        GUILayout.EndScrollView();
    }

    // ---- Helpers ----

    private LocalRequest NewRequest(TriggerSpec trigger) => new LocalRequest
    {
        Id = "sample_" + DateTime.Now.Ticks,
        Title = "Beamable",
        Body = "Tap to open: " + _url,
        Trigger = trigger,
        UserInfo = new Dictionary<string, object> { ["deepLink"] = _url }
    };

    private IEnumerator SendDiscordWebhook(string content)
    {
        string json = "{\"content\":\"" + EscapeJson(content) + "\"}";
        using (var req = new UnityWebRequest(WebhookUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            Log($"webhook → result={req.result} http={req.responseCode} err={req.error}");
        }
    }

    private void Log(string line)
    {
        string stamped = $"[{DateTime.Now:HH:mm:ss}] {line}";
        _log.Add(stamped);
        if (_log.Count > MaxLogLines) _log.RemoveAt(0);
        Debug.Log($"[BeamableNativeSample] {line}");
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var b = new StringBuilder(s.Length + 16);
        foreach (char c in s)
        {
            switch (c)
            {
                case '"':  b.Append("\\\""); break;
                case '\\': b.Append("\\\\"); break;
                case '\n': b.Append("\\n");  break;
                case '\r': b.Append("\\r");  break;
                case '\t': b.Append("\\t");  break;
                default:
                    if (c < 0x20) b.Append("\\u").Append(((int)c).ToString("x4"));
                    else b.Append(c);
                    break;
            }
        }
        return b.ToString();
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? "") : s.Substring(0, max) + "…";

    private static string Tail(string s, int n)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(s.Length - n));
}
