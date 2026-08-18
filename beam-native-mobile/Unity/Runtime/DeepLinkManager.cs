using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.IO;
using System.Threading;
#endif

public class DeepLinkManager : MonoBehaviour
{
    public static DeepLinkManager Instance { get; private set; }

    public static event Action<DeepLinkData> DeepLinkReceived;

    public List<DeepLinkData> History { get; private set; } = new List<DeepLinkData>();

    // Short window to dedupe the same URL arriving from more than one source
    // (on Android the native lib and Unity's Application.deepLinkActivated can
    // both observe a single warm/cold-start VIEW intent).
    private const float DuplicateWindowSeconds = 1.0f;
    private string _lastProcessedUrl;
    private float _lastProcessedTime = -10f;

#if UNITY_ANDROID && !UNITY_EDITOR
    private const string DeepLinkClass = "com.beamable.deeplink.unity.UnityDeepLink";
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private static Mutex _instanceMutex;
    private static readonly string MailboxPath =
        Path.Combine(Path.GetTempPath(), "DeeplinkDemo_deeplink.txt");
    private const string MutexName = "DeeplinkDemo_SingleInstance";
    private const string SchemePrefix = "deeplinkdemo://";
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetBrowserURL();

    [DllImport("__Internal")]
    private static extern void RegisterHashChangeListener();

    [DllImport("__Internal")]
    private static extern void CheckInitialURL();
#endif

    // Self-bootstrap: spawn the persistent "DeepLinkManager" GameObject at startup (the native
    // deeplink lib targets it by name via UnitySendMessage). Notifications are handled separately
    // by the Beamable.Notifications package.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("DeepLinkManager").AddComponent<DeepLinkManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        _instanceMutex = new Mutex(true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            string url = ExtractDeepLinkFromCommandLine();
            if (!string.IsNullOrEmpty(url))
            {
                try { File.WriteAllText(MailboxPath, url); }
                catch (Exception e) { Debug.LogWarning($"[DeepLink] Failed to write mailbox: {e.Message}"); }
            }
            Debug.Log("[DeepLink] Another instance is running. Forwarded deep link and quitting.");
            Application.Quit();
            return;
        }
#endif

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.deepLinkActivated += OnDeepLinkActivated;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Initialize the native deeplink library. Warm-start deeplinks are pushed
        // back to this GameObject's OnNativeDeepLink via UnitySendMessage.
        using (var dl = new AndroidJavaClass(DeepLinkClass))
            dl.CallStatic("initialize", gameObject.name);
#endif
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RegisterHashChangeListener();
        CheckInitialURL();
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
        CheckWindowsCommandLineArgs();
#elif UNITY_ANDROID && !UNITY_EDITOR
        // Cold-start: pull the launch intent's deeplink from the native library.
        using (var dl = new AndroidJavaClass(DeepLinkClass))
        {
            string url = dl.CallStatic<string>("getInitialLink");
            if (!string.IsNullOrEmpty(url))
                ProcessDeepLink(url);
        }
#else
        if (!string.IsNullOrEmpty(Application.absoluteURL))
            ProcessDeepLink(Application.absoluteURL);
#endif
    }

    private void OnDestroy()
    {
        Application.deepLinkActivated -= OnDeepLinkActivated;
    }

    private void OnDeepLinkActivated(string url)
    {
        ProcessDeepLink(url);
    }

    // Called from WebGL jslib via SendMessage
    public void OnBrowserUrlChanged(string url)
    {
        string deepLink = ExtractHashFragment(url);
        if (!string.IsNullOrEmpty(deepLink))
            ProcessDeepLink(deepLink);
    }

    // Called from the native Android deeplink library via UnitySendMessage
    // (warm-start VIEW intents). Cold-start is handled by getInitialLink in Start.
    public void OnNativeDeepLink(string url)
    {
        if (!string.IsNullOrEmpty(url))
            ProcessDeepLink(url);
    }

    public void ProcessDeepLink(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        // Drop a duplicate of the same URL within a short window (multiple sources
        // may observe a single deeplink — see DuplicateWindowSeconds).
        float now = Time.realtimeSinceStartup;
        if (url == _lastProcessedUrl && (now - _lastProcessedTime) < DuplicateWindowSeconds)
        {
            Debug.Log($"[DeepLink] Ignored duplicate within {DuplicateWindowSeconds}s: {url}");
            return;
        }
        _lastProcessedUrl = url;
        _lastProcessedTime = now;

        Debug.Log($"[DeepLink] Received: {url}");

        var data = ParseUrl(url);
        History.Insert(0, data);
        DeepLinkReceived?.Invoke(data);
    }

    private DeepLinkData ParseUrl(string url)
    {
        var data = new DeepLinkData
        {
            RawUrl = url,
            ReceivedAt = DateTime.Now
        };

        try
        {
            var uri = new Uri(url);
            data.Scheme = uri.Scheme;
            data.Host = uri.Host;
            data.Path = uri.AbsolutePath;
            data.QueryParameters = ParseQueryString(uri.Query);
        }
        catch (UriFormatException e)
        {
            Debug.LogWarning($"[DeepLink] Failed to parse URL: {e.Message}");
            data.Scheme = "parse-error";
            data.Host = url;
        }

        return data;
    }

    private Dictionary<string, string> ParseQueryString(string query)
    {
        var parameters = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query))
            return parameters;

        if (query.StartsWith("?"))
            query = query.Substring(1);

        foreach (string pair in query.Split('&'))
        {
            if (string.IsNullOrEmpty(pair))
                continue;

            int equalsIndex = pair.IndexOf('=');
            if (equalsIndex < 0)
            {
                parameters[Uri.UnescapeDataString(pair)] = "";
            }
            else
            {
                string key = Uri.UnescapeDataString(pair.Substring(0, equalsIndex));
                string value = Uri.UnescapeDataString(pair.Substring(equalsIndex + 1));
                parameters[key] = value;
            }
        }

        return parameters;
    }

    private string ExtractHashFragment(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        int hashIndex = url.IndexOf('#');
        if (hashIndex < 0 || hashIndex >= url.Length - 1)
            return null;

        return url.Substring(hashIndex + 1);
    }

    private string ExtractQueryString(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        int queryIndex = url.IndexOf('?');
        if (queryIndex < 0 || queryIndex >= url.Length - 1)
            return null;

        return url.Substring(queryIndex);
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private float _mailboxCheckTimer;

    private void Update()
    {
        _mailboxCheckTimer += Time.unscaledDeltaTime;
        if (_mailboxCheckTimer < 0.5f)
            return;
        _mailboxCheckTimer = 0f;
        CheckMailbox();
    }

    private void CheckMailbox()
    {
        try
        {
            if (!File.Exists(MailboxPath))
                return;

            string url = File.ReadAllText(MailboxPath).Trim();
            File.Delete(MailboxPath);

            if (!string.IsNullOrEmpty(url))
            {
                Debug.Log($"[DeepLink] Received from mailbox: {url}");
                ProcessDeepLink(url);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DeepLink] Mailbox read error: {e.Message}");
        }
    }

    private void CheckWindowsCommandLineArgs()
    {
        string url = ExtractDeepLinkFromCommandLine();
        if (!string.IsNullOrEmpty(url))
        {
            Debug.Log($"[DeepLink] Found in command line: {url}");
            ProcessDeepLink(url);
        }
    }

    private static string ExtractDeepLinkFromCommandLine()
    {
        // Use raw command line to avoid argument parser mangling URLs
        string cmdLine = Environment.CommandLine;
        int schemeIndex = cmdLine.IndexOf(SchemePrefix, StringComparison.OrdinalIgnoreCase);
        if (schemeIndex < 0)
            return null;

        string url = cmdLine.Substring(schemeIndex);

        // Trim trailing quote or whitespace
        url = url.TrimEnd('"', '\'', ' ', '\t', '\r', '\n');
        return url;
    }

    private void OnApplicationQuit()
    {
        if (_instanceMutex != null)
        {
            _instanceMutex.ReleaseMutex();
            _instanceMutex.Dispose();
            _instanceMutex = null;
        }
    }
#endif

    [Serializable]
    public class DeepLinkData
    {
        public string RawUrl;
        public string Scheme;
        public string Host;
        public string Path;
        public Dictionary<string, string> QueryParameters = new Dictionary<string, string>();
        public DateTime ReceivedAt;
    }
}
