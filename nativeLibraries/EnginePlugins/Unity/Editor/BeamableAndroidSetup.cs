using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared edit-time logic for the Beamable native Android library. Single source of truth
/// used by both <c>BeamableAndroidSetupWindow</c> (manual) and the pre-build processor in
/// <c>BeamableAndroidBuildProcessor</c> (automatic). Editor-only; ships inside the
/// <c>com.beamable.notifications</c> package.
///
/// The unified <c>beamable-notifications-release.aar</c> ships with this package under
/// <see cref="PackageAarPath"/> (produced by <c>./dev-native.sh</c>). App-specific config —
/// the <c>AndroidManifest.xml</c> and (optional) <c>google-services.json</c> — lives in the
/// consuming project under <see cref="PluginsAndroidDir"/>; this tool can scaffold a default
/// manifest when one is missing.
/// </summary>
public static class BeamableAndroidSetup
{
    public const string PluginsAndroidDir = "Assets/Plugins/Android";
    public const string ManifestPath = PluginsAndroidDir + "/AndroidManifest.xml";
    public const string GoogleServicesPath = PluginsAndroidDir + "/google-services.json";

    // The unified AAR ships inside this package (copied here by ./dev-native.sh).
    public const string PackageAarPath =
        "Packages/com.beamable.notifications/Plugins/Android/beamable-notifications-release.aar";

    public const string HandlerMetaKey = "com.beamable.push.notification_received_handler";
    public const string DeepLinkScheme = "beamable";
    public const int RequiredMinSdk = 24;

    // Receive-time handler class names: the placeholder a fresh scaffold points at, and the one the
    // NativeDemo sample ships (wired by the "Set Up Native Sample" menu).
    public const string PlaceholderHandlerClass = "com.companyname.app.MyPushReceivedHandler";
    public const string SampleHandlerClass = "com.beamable.sample.DiscordWebhookPushHandler";

    private const string AndroidNs = "http://schemas.android.com/apk/res/android";

    // ProjectSettings toggles that must be OFF now that gradle config is injected at build.
    private static readonly string[] StaleGradleToggles =
    {
        "useCustomMainGradleTemplate",
        "useCustomLauncherGradleManifest",
        "useCustomBaseGradleTemplate",
        "useCustomGradlePropertiesTemplate",
    };

    public enum Level { Ok, Warn, Error }

    public struct CheckResult
    {
        public Level Level;
        public string Title;
        public string Detail;

        public CheckResult(Level level, string title, string detail)
        {
            Level = level;
            Title = title;
            Detail = detail;
        }
    }

    // ---- Validation ---------------------------------------------------------

    /// <summary>Runs all checks and returns a per-item report.</summary>
    public static List<CheckResult> Validate()
    {
        var results = new List<CheckResult>();

        // The unified AAR ships inside this package; ./dev-native.sh refreshes it. Not an Error
        // (a normal checkout has it committed) — only warn if it has somehow gone missing.
        results.Add(File.Exists(PackageAarPath)
            ? new CheckResult(Level.Ok, "Notifications AAR present", "beamable-notifications-release.aar found in the package")
            : new CheckResult(Level.Warn, "Notifications AAR missing",
                "Run ./dev-native.sh to build beamable-notifications-release.aar into " + PackageAarPath));

        // Manifest checks (app-specific; auto-created by ApplySettings when absent).
        if (File.Exists(ManifestPath))
        {
            string manifest = File.ReadAllText(ManifestPath);
            results.Add(manifest.Contains("android:scheme=\"" + DeepLinkScheme + "\"")
                ? new CheckResult(Level.Ok, "Deeplink scheme present", DeepLinkScheme + ":// VIEW intent-filter found")
                : new CheckResult(Level.Warn, "Deeplink scheme missing",
                    "Add a VIEW intent-filter with scheme '" + DeepLinkScheme + "' to " + ManifestPath));

            results.Add(manifest.Contains(HandlerMetaKey)
                ? new CheckResult(Level.Ok, "Receive handler registered", HandlerMetaKey + " meta-data found")
                : new CheckResult(Level.Warn, "Receive handler not registered",
                    "Optional: add <meta-data> '" + HandlerMetaKey + "' to fire native code on push receipt while closed."));
        }
        else
        {
            results.Add(new CheckResult(Level.Warn, "AndroidManifest.xml missing",
                "No manifest at " + ManifestPath + " — run Setup (or build) to scaffold a default."));
        }

        // Min SDK.
        int min = (int)PlayerSettings.Android.minSdkVersion;
        results.Add(min >= RequiredMinSdk
            ? new CheckResult(Level.Ok, "Min SDK OK", "minSdkVersion = " + min)
            : new CheckResult(Level.Error, "Min SDK too low", "minSdkVersion = " + min + "; the libraries require " + RequiredMinSdk + "+"));

        // Remote vs local-only.
        results.Add(File.Exists(GoogleServicesPath)
            ? new CheckResult(Level.Ok, "Remote push (FCM) enabled", "google-services.json present — FCM will be wired at build")
            : new CheckResult(Level.Ok, "Local-only mode", "No google-services.json — remote push disabled (local notifications still work)"));

        // Stale gradle template toggles.
        var on = new List<string>();
        var so = LoadProjectSettings();
        if (so != null)
        {
            foreach (var t in StaleGradleToggles)
            {
                var p = so.FindProperty(t);
                if (p != null && p.boolValue) on.Add(t);
            }
        }
        results.Add(on.Count == 0
            ? new CheckResult(Level.Ok, "Gradle templates off", "Gradle config is injected at build (no custom templates needed)")
            : new CheckResult(Level.Warn, "Stale gradle templates enabled",
                "These should be OFF (run Setup): " + string.Join(", ", on)));

        return results;
    }

    // ---- Auto-apply ---------------------------------------------------------

    /// <summary>
    /// Applies the settings the tool can fix automatically. Returns what changed.
    /// <paramref name="handlerClass"/>, when given, wires the receive-time handler meta-data to that
    /// class (the NativeDemo sample passes its own handler); when null an existing handler value is
    /// left untouched and a missing one gets the placeholder.
    /// </summary>
    public static List<string> ApplySettings(string handlerClass = null)
    {
        var changes = new List<string>();

        ConfigureManifest(changes, handlerClass);

        if ((int)PlayerSettings.Android.minSdkVersion < RequiredMinSdk)
        {
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)RequiredMinSdk;
            changes.Add("Set Android minSdkVersion to " + RequiredMinSdk);
        }

        var so = LoadProjectSettings();
        if (so != null)
        {
            bool dirty = false;

            var customMain = so.FindProperty("useCustomMainManifest");
            if (customMain != null && !customMain.boolValue)
            {
                customMain.boolValue = true;
                dirty = true;
                changes.Add("Enabled custom main AndroidManifest");
            }

            foreach (var t in StaleGradleToggles)
            {
                var p = so.FindProperty(t);
                if (p != null && p.boolValue)
                {
                    p.boolValue = false;
                    dirty = true;
                    changes.Add("Disabled " + t + " (gradle is injected at build now)");
                }
            }

            if (dirty)
                so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (changes.Count == 0)
            changes.Add("Nothing to change — already configured.");

        return changes;
    }

    // ---- Manifest scaffold / patch ------------------------------------------

    /// <summary>
    /// Ensures the consuming project's custom main <c>AndroidManifest.xml</c> is wired for Beamable
    /// notifications + deep links. If absent, writes a working default. If present, **patches it
    /// idempotently** — adding the <c>beamable://</c> VIEW intent-filter, the push permissions, and
    /// the receive-handler meta-data only where missing. <paramref name="handlerClass"/>, when given,
    /// sets/updates the handler meta-data value (the placeholder is used otherwise, and an existing
    /// value is never overwritten when null).
    /// </summary>
    public static void ConfigureManifest(List<string> changes, string handlerClass = null)
    {
        if (!File.Exists(ManifestPath))
        {
            Directory.CreateDirectory(PluginsAndroidDir);
            File.WriteAllText(ManifestPath, DefaultManifestXml(handlerClass));
            AssetDatabase.ImportAsset(ManifestPath);
            changes?.Add("Created default AndroidManifest.xml at " + ManifestPath +
                         " (edit the package id as needed).");
            return;
        }

        try
        {
            PatchManifest(changes, handlerClass);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[BeamableAndroid] could not patch " + ManifestPath + " (" + e.Message +
                "). Add the " + DeepLinkScheme + ":// filter, the '" + HandlerMetaKey +
                "' meta-data, and POST_NOTIFICATIONS / SCHEDULE_EXACT_ALARM by hand.");
        }
    }

    // Idempotently inject the Beamable bits into an existing manifest. Only saves when something
    // actually changed, so re-running produces no churn (beyond a one-time reformat).
    private static void PatchManifest(List<string> changes, string handlerClass)
    {
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.Load(ManifestPath);

        var root = doc.DocumentElement;
        if (root == null || root.Name != "manifest")
            throw new System.Exception("root <manifest> element not found");

        bool dirty = false;

        // 1. Permissions.
        string[] perms =
        {
            "android.permission.INTERNET",
            "android.permission.POST_NOTIFICATIONS",
            "android.permission.SCHEDULE_EXACT_ALARM",
        };
        foreach (var perm in perms)
        {
            if (HasUsesPermission(root, perm)) continue;
            var el = doc.CreateElement("uses-permission");
            SetAndroidAttr(doc, el, "name", perm);
            root.AppendChild(el);
            changes?.Add("Added uses-permission " + perm);
            dirty = true;
        }

        // 2. <application>.
        var app = root.SelectSingleNode("application") as XmlElement;
        if (app == null)
        {
            app = doc.CreateElement("application");
            root.AppendChild(app);
            dirty = true;
        }

        // 3. Launcher activity: deep-link filter + launchMode/exported.
        var activity = FindLauncherActivity(app);
        if (activity != null)
        {
            if (GetAndroidAttr(activity, "launchMode") != "singleTask")
            {
                SetAndroidAttr(doc, activity, "launchMode", "singleTask");
                changes?.Add("Set launchMode=singleTask on the launcher activity");
                dirty = true;
            }
            if (GetAndroidAttr(activity, "exported") != "true")
            {
                SetAndroidAttr(doc, activity, "exported", "true");
                changes?.Add("Set exported=true on the launcher activity");
                dirty = true;
            }
            if (!HasDeepLinkFilter(activity))
            {
                activity.AppendChild(CreateDeepLinkFilter(doc));
                changes?.Add("Added " + DeepLinkScheme + ":// VIEW intent-filter");
                dirty = true;
            }
        }

        // 4. Receive-handler meta-data.
        var meta = FindMetaData(app, HandlerMetaKey);
        if (meta == null)
        {
            var el = doc.CreateElement("meta-data");
            SetAndroidAttr(doc, el, "name", HandlerMetaKey);
            SetAndroidAttr(doc, el, "value", handlerClass ?? PlaceholderHandlerClass);
            app.AppendChild(el);
            changes?.Add("Added receive-handler meta-data (" + (handlerClass ?? PlaceholderHandlerClass) + ")");
            dirty = true;
        }
        else if (handlerClass != null && GetAndroidAttr(meta, "value") != handlerClass)
        {
            SetAndroidAttr(doc, meta, "value", handlerClass);
            changes?.Add("Wired receive-handler to " + handlerClass);
            dirty = true;
        }

        if (!dirty)
            return;

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false),
        };
        using (var w = XmlWriter.Create(ManifestPath, settings))
            doc.Save(w);
        AssetDatabase.ImportAsset(ManifestPath);
    }

    /// <summary>
    /// A ready-to-build manifest: launcher + <c>beamable://</c> deep-link intent-filter on
    /// UnityPlayerActivity, the receive-time handler meta-data, and the push permissions
    /// (POST_NOTIFICATIONS + SCHEDULE_EXACT_ALARM + INTERNET). The push service/receiver come from
    /// the library's own merged manifest, so they are not repeated here.
    /// </summary>
    public static string DefaultManifestXml(string handlerClass = null)
    {
        return
@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
          package=""com.companyname.app""
          android:installLocation=""Automatic"">

  <uses-permission android:name=""android.permission.INTERNET"" />
  <!-- Runtime notification permission (Android 13+). -->
  <uses-permission android:name=""android.permission.POST_NOTIFICATIONS"" />
  <!-- Exact local alarms (LocalRequest calendar/exact). API 33+ user-granted; falls back to inexact. -->
  <uses-permission android:name=""android.permission.SCHEDULE_EXACT_ALARM"" />

  <application>
    <activity android:name=""com.unity3d.player.UnityPlayerActivity""
              android:launchMode=""singleTask""
              android:exported=""true"">
      <intent-filter>
        <action android:name=""android.intent.action.MAIN"" />
        <category android:name=""android.intent.category.LAUNCHER"" />
      </intent-filter>
      <!-- Deep links: " + DeepLinkScheme + @"://... -->
      <intent-filter>
        <action android:name=""android.intent.action.VIEW"" />
        <category android:name=""android.intent.category.DEFAULT"" />
        <category android:name=""android.intent.category.BROWSABLE"" />
        <data android:scheme=""" + DeepLinkScheme + @""" />
      </intent-filter>
      <meta-data android:name=""unityplayer.UnityActivity"" android:value=""true"" />
    </activity>

    <!-- Native receive-time handler (runs even when the app is killed, data-only FCM).
         Point this at your PushNotificationReceivedHandler implementation; the NativeDemo
         sample ships com.beamable.sample.DiscordWebhookPushHandler as an example. -->
    <meta-data android:name=""" + HandlerMetaKey + @"""
               android:value=""" + (handlerClass ?? PlaceholderHandlerClass) + @""" />
  </application>

</manifest>
";
    }

    // ---- Manifest XML helpers ----------------------------------------------

    private static bool HasUsesPermission(XmlElement root, string name)
    {
        foreach (XmlElement el in root.SelectNodes("uses-permission"))
            if (GetAndroidAttr(el, "name") == name) return true;
        return false;
    }

    // The launcher activity (preferring UnityPlayerActivity by name, else whatever declares the
    // LAUNCHER category) — that's where the deep-link filter belongs.
    private static XmlElement FindLauncherActivity(XmlElement app)
    {
        XmlElement byName = null;
        foreach (XmlElement act in app.SelectNodes("activity"))
        {
            if (GetAndroidAttr(act, "name") == "com.unity3d.player.UnityPlayerActivity")
                byName = act;
            foreach (XmlElement cat in act.SelectNodes("intent-filter/category"))
                if (GetAndroidAttr(cat, "name") == "android.intent.category.LAUNCHER")
                    return act;
        }
        return byName;
    }

    private static bool HasDeepLinkFilter(XmlElement activity)
    {
        foreach (XmlElement data in activity.SelectNodes("intent-filter/data"))
            if (GetAndroidAttr(data, "scheme") == DeepLinkScheme) return true;
        return false;
    }

    private static XmlElement CreateDeepLinkFilter(XmlDocument doc)
    {
        var f = doc.CreateElement("intent-filter");
        f.AppendChild(MakeNamedChild(doc, "action", "android.intent.action.VIEW"));
        f.AppendChild(MakeNamedChild(doc, "category", "android.intent.category.DEFAULT"));
        f.AppendChild(MakeNamedChild(doc, "category", "android.intent.category.BROWSABLE"));
        var data = doc.CreateElement("data");
        SetAndroidAttr(doc, data, "scheme", DeepLinkScheme);
        f.AppendChild(data);
        return f;
    }

    private static XmlElement MakeNamedChild(XmlDocument doc, string tag, string androidName)
    {
        var el = doc.CreateElement(tag);
        SetAndroidAttr(doc, el, "name", androidName);
        return el;
    }

    private static XmlElement FindMetaData(XmlElement app, string name)
    {
        foreach (XmlElement el in app.SelectNodes("meta-data"))
            if (GetAndroidAttr(el, "name") == name) return el;
        return null;
    }

    private static string GetAndroidAttr(XmlElement el, string localName) =>
        el.GetAttribute(localName, AndroidNs);

    private static void SetAndroidAttr(XmlDocument doc, XmlElement el, string localName, string value)
    {
        var attr = doc.CreateAttribute("android", localName, AndroidNs);
        attr.Value = value;
        el.SetAttributeNode(attr);
    }

    // ---- Helpers ------------------------------------------------------------

    private static SerializedObject LoadProjectSettings()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0)
            return null;
        return new SerializedObject(assets[0]);
    }
}
