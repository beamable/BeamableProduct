using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared edit-time logic for the Beamable native Android library. Single source of truth
/// used by both <c>BeamableNotificationsWindow</c> (manual) and the pre-build processor in
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

    // Receive-time handler wiring is NO LONGER auto-applied (Decision Q8 / §5.2): receive-time
    // analytics moved native, so the package no longer owns/scaffolds a push-received handler.
    // The editor window can GENERATE a sample handler file for the user to customize; wiring its
    // <meta-data> into the manifest is then an explicit, opt-in action (pass handlerClass to
    // ApplySettings/ConfigureManifest). Kept public for callers that opt in.
    public const string PlaceholderHandlerClass = "com.companyname.app.MyPushReceivedHandler";

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

            // Receive-handler meta-data is optional and no longer auto-wired (Decision Q8). Both
            // states are OK: report it as info, not a warning.
            results.Add(manifest.Contains(HandlerMetaKey)
                ? new CheckResult(Level.Ok, "Receive handler wired (optional)", HandlerMetaKey + " meta-data found")
                : new CheckResult(Level.Ok, "No custom receive handler",
                    "Optional: generate a sample PushNotificationReceivedHandler from the Beamable " +
                    "Notifications window and add its <meta-data> '" + HandlerMetaKey +
                    "' to run native code on push receipt while the app is closed."));
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
    /// <paramref name="handlerClass"/> is an OPT-IN: when given, wires the receive-time handler
    /// meta-data to that class; when null (the default) no receive handler is wired or scaffolded
    /// (Decision Q8 / §5.2) and any existing handler value is left untouched.
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

    // ---- Individually-runnable setup steps (§5.4) ---------------------------

    /// <summary>A single, named, individually-runnable setup step for the editor window.</summary>
    public class SetupStep
    {
        public string Title;
        public string Description;
        public System.Func<List<string>> Run; // returns a per-step change log

        public SetupStep(string title, string description, System.Func<List<string>> run)
        {
            Title = title;
            Description = description;
            Run = run;
        }
    }

    /// <summary>
    /// The post-build setup decomposed into individually-runnable items (§5.4), covering BOTH
    /// Android and iOS. Each step wraps a slice of <see cref="ApplySettings"/> so the window can run
    /// them one at a time or all together ("Run Setup (All)"). iOS steps are advisory here: the
    /// heavy iOS Xcode wiring runs automatically in the post-build processor (NotificationsPostProcess).
    /// </summary>
    public static List<SetupStep> GetSetupSteps()
    {
        return new List<SetupStep>
        {
            new SetupStep(
                "Android: AndroidManifest",
                "Scaffold or idempotently patch Assets/Plugins/Android/AndroidManifest.xml — the " +
                DeepLinkScheme + ":// deep-link VIEW filter, push permissions, launcher launchMode/exported.",
                () =>
                {
                    var changes = new List<string>();
                    ConfigureManifest(changes); // no handler (opt-in only)
                    if (changes.Count == 0) changes.Add("Manifest already configured.");
                    return changes;
                }),
            new SetupStep(
                "Android: Min SDK",
                "Ensure PlayerSettings minSdkVersion is at least " + RequiredMinSdk + ".",
                () =>
                {
                    var changes = new List<string>();
                    if ((int)PlayerSettings.Android.minSdkVersion < RequiredMinSdk)
                    {
                        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)RequiredMinSdk;
                        changes.Add("Set Android minSdkVersion to " + RequiredMinSdk);
                    }
                    else changes.Add("Min SDK already >= " + RequiredMinSdk + ".");
                    return changes;
                }),
            new SetupStep(
                "Android: Project Settings",
                "Enable custom main manifest and disable stale custom-gradle-template toggles " +
                "(gradle config is injected at build).",
                ApplyProjectSettingsToggles),
            new SetupStep(
                "iOS: Post-build wiring (auto)",
                "iOS Push capability, App Group, Notification Service Extension, remote-notification " +
                "background mode and Swift runtime are wired automatically at build by " +
                "NotificationsPostProcess. Set the AppGroupId / bundle ids there for your team.",
                () => new List<string>
                {
                    "No action needed in the editor — iOS setup runs in the post-build processor.",
                    "Verify the App Group + Push entitlements are enabled in your Apple provisioning profiles."
                }),
        };
    }

    /// <summary>Applies just the ProjectSettings toggles slice of <see cref="ApplySettings"/>.</summary>
    public static List<string> ApplyProjectSettingsToggles()
    {
        var changes = new List<string>();
        var so = LoadProjectSettings();
        if (so == null)
        {
            changes.Add("Could not load ProjectSettings.asset.");
            return changes;
        }

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
        if (dirty) so.ApplyModifiedPropertiesWithoutUndo();
        if (changes.Count == 0) changes.Add("Project settings already correct.");
        return changes;
    }

    // ---- Push-received handler SAMPLE generation (§5.2 / §5.4) --------------

    /// <summary>
    /// Generates a SAMPLE Android <c>PushNotificationReceivedHandler</c> Kotlin file for the user to
    /// customize (Decision Q8: no auto-wire). Returns the asset path written. Does NOT add the
    /// <c>&lt;meta-data&gt;</c> to the manifest — wiring is a deliberate follow-up step (call
    /// <see cref="ConfigureManifest"/> with the handler's fully-qualified class name once edited).
    /// </summary>
    public static string GenerateSampleReceivedHandler()
    {
        const string dir = "Assets/Plugins/Android/src/com/companyname/app";
        const string path = dir + "/MyPushReceivedHandler.kt";
        Directory.CreateDirectory(dir);
        if (!File.Exists(path))
            File.WriteAllText(path, SampleReceivedHandlerKotlin());
        AssetDatabase.ImportAsset(path);
        return path;
    }

    /// <summary>The sample handler source — a documented, no-op-by-default starting point.</summary>
    public static string SampleReceivedHandlerKotlin()
    {
        return
@"package com.companyname.app

import android.content.Context
import com.beamable.push.PushReceivedEvent
import com.beamable.push.PushNotificationReceivedHandler

/**
 * SAMPLE Beamable push-received handler (generated; safe to edit).
 *
 * Runs natively on the device the moment a push arrives — including when the app is killed
 * (data-only FCM). Use it for closed-app side effects; campaign funnel analytics
 * (Sent/Received/Opened) are already emitted natively by the Beamable library, so you do NOT
 * need to post analytics here.
 *
 * To activate this handler, add its fully-qualified class name to your AndroidManifest under the
 * '" + HandlerMetaKey + @"' <meta-data> key
 * (the Beamable Notifications editor window can wire it for you), or register it programmatically
 * via PushManager.addNotificationReceivedHandler(MyPushReceivedHandler()).
 */
class MyPushReceivedHandler : PushNotificationReceivedHandler {
    override fun onNotificationReceived(context: Context, event: PushReceivedEvent) {
        // TODO: your closed-app logic here. `event` carries the title/body/data payload and the
        // 3.3 campaign intent-data (campaignId, nodeId, offers, deeplink, ...).
    }
}
";
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

        // 4. Receive-handler meta-data — ONLY when explicitly opting in (handlerClass != null).
        //    Decision Q8 / §5.2: the package no longer auto-wires a placeholder handler. When a
        //    handler class is given (user wired their generated sample), set/update its meta-data.
        if (handlerClass != null)
        {
            var meta = FindMetaData(app, HandlerMetaKey);
            if (meta == null)
            {
                var el = doc.CreateElement("meta-data");
                SetAndroidAttr(doc, el, "name", HandlerMetaKey);
                SetAndroidAttr(doc, el, "value", handlerClass);
                app.AppendChild(el);
                changes?.Add("Added receive-handler meta-data (" + handlerClass + ")");
                dirty = true;
            }
            else if (GetAndroidAttr(meta, "value") != handlerClass)
            {
                SetAndroidAttr(doc, meta, "value", handlerClass);
                changes?.Add("Wired receive-handler to " + handlerClass);
                dirty = true;
            }
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
        // Receive-handler meta-data is opt-in (Decision Q8): only emit it when a handler class is
        // explicitly supplied. A default scaffold no longer points at a placeholder handler.
        string handlerMeta = handlerClass == null
            ? ""
            : @"
    <!-- Native receive-time handler (runs even when the app is killed, data-only FCM).
         Points at your generated PushNotificationReceivedHandler implementation. -->
    <meta-data android:name=""" + HandlerMetaKey + @"""
               android:value=""" + handlerClass + @""" />";

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
    </activity>" + handlerMeta + @"
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
