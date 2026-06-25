using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Richer single setup/validation window for the Beamable Notifications package (§5.4). Supersedes
/// the old Android-only BeamableAndroidSetupWindow. Menu: Tools/Beamable/Notifications.
///
/// Sections:
///  1. Deeplink / intent-data schema setup and change (§3.3, §3.4).
///  2. Validation + "Run Setup (All)" plus per-item run buttons, covering Android and iOS
///     post-build setup (wraps BeamableAndroidSetup.Validate()/the per-step API).
///  3. Push-received handler SAMPLE generation (generate + open; no auto-wire — Decision Q8).
///  4. google-services.json guidance with an inline guide + link.
/// </summary>
public class BeamableNotificationsWindow : EditorWindow
{
    private const string SchemePrefKey = "Beamable.Notifications.DeepLinkScheme";

    // The canonical §3.3 intent-data schema fields, shown read-only as a contract reference.
    private static readonly string[] IntentSchemaFields =
    {
        "campaignId  (string)   — campaign identifier (enables funnel tracking with nodeId)",
        "nodeId      (string)   — campaign node identifier",
        "gamerTag    (string)   — Beamable dbid",
        "accountId   (string)   — Beamable account id",
        "cidPid      (string)   — \"<cid>.<pid>\" realm scope",
        "deeplink    (string)   — raw deeplink (schema-less, passed through verbatim)",
        "offers[]    (array)    — { itemId, value, customData{} }  (stringified on the wire)",
        "campaignData(object)   — free-form object (stringified on the wire)",
    };

    private List<BeamableAndroidSetup.CheckResult> _results;
    private List<BeamableAndroidSetup.SetupStep> _steps;
    private Vector2 _scroll;
    private string _lastSummary;

    private bool _showSchema = true;
    private bool _showSetup = true;
    private bool _showHandler = true;
    private bool _showFcm = true;

    private string _scheme;

    private const string FcmGuideUrl =
        "https://firebase.google.com/docs/cloud-messaging/android/client#add_a_firebase_configuration_file";

    [MenuItem("Tools/Beamable/Notifications")]
    public static void Open()
    {
        var w = GetWindow<BeamableNotificationsWindow>(true, "Beamable Notifications", true);
        w.minSize = new Vector2(520, 560);
        w.Refresh();
    }

    private void OnEnable() => Refresh();

    private void Refresh()
    {
        _results = BeamableAndroidSetup.Validate();
        _steps = BeamableAndroidSetup.GetSetupSteps();
        _scheme = EditorPrefs.GetString(SchemePrefKey, BeamableAndroidSetup.DeepLinkScheme);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Beamable Notifications", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Push + Deeplink setup, validation, and tooling (iOS + Android)",
            EditorStyles.miniLabel);
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSchemaSection();
        EditorGUILayout.Space();
        DrawSetupSection();
        EditorGUILayout.Space();
        DrawHandlerSection();
        EditorGUILayout.Space();
        DrawFcmSection();

        EditorGUILayout.EndScrollView();
    }

    // ---- 1. Deeplink / intent-data schema -----------------------------------

    private void DrawSchemaSection()
    {
        _showSchema = EditorGUILayout.BeginFoldoutHeaderGroup(_showSchema, "1. Deeplink / Intent-Data Schema");
        if (_showSchema)
        {
            EditorGUILayout.HelpBox(
                "The deeplink scheme is the custom URL scheme your app responds to (e.g. " +
                _scheme + "://...). It is written into the AndroidManifest VIEW intent-filter.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                _scheme = EditorGUILayout.TextField("Deeplink scheme", _scheme);
                if (GUILayout.Button("Save", GUILayout.Width(60)))
                {
                    EditorPrefs.SetString(SchemePrefKey, string.IsNullOrEmpty(_scheme)
                        ? BeamableAndroidSetup.DeepLinkScheme : _scheme.Trim());
                    _lastSummary = "Saved deeplink scheme: " + _scheme;
                }
            }

            EditorGUILayout.HelpBox(
                "Note: the manifest scaffold currently uses the package default scheme '" +
                BeamableAndroidSetup.DeepLinkScheme + "'. A saved override here is used as guidance " +
                "for your own manifest edits; full per-project scheme injection is tracked for a " +
                "follow-up.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notification intent-data schema (§3.3) — shared contract",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Carried in the FCM data / APNs userInfo payload; parsed into NotificationData.CampaignIntent.",
                EditorStyles.wordWrappedMiniLabel);
            foreach (var field in IntentSchemaFields)
                EditorGUILayout.LabelField("• " + field, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ---- 2. Validation + Run Setup ------------------------------------------

    private void DrawSetupSection()
    {
        _showSetup = EditorGUILayout.BeginFoldoutHeaderGroup(_showSetup, "2. Validation & Setup (Android + iOS)");
        if (_showSetup)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Setup (All)", GUILayout.Height(26)))
                {
                    var all = new List<string>();
                    foreach (var step in _steps)
                        all.AddRange(step.Run());
                    _lastSummary = string.Join("\n", all);
                    Refresh();
                }
                if (GUILayout.Button("Validate", GUILayout.Height(26)))
                    Refresh();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Run individual steps", EditorStyles.boldLabel);
            foreach (var step in _steps)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(step.Title, EditorStyles.boldLabel);
                        if (GUILayout.Button("Run", GUILayout.Width(60)))
                        {
                            _lastSummary = string.Join("\n", step.Run());
                            Refresh();
                        }
                    }
                    EditorGUILayout.LabelField(step.Description, EditorStyles.wordWrappedMiniLabel);
                }
            }

            if (!string.IsNullOrEmpty(_lastSummary))
                EditorGUILayout.HelpBox("Last action:\n" + _lastSummary, MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation report", EditorStyles.boldLabel);
            if (_results != null)
                foreach (var r in _results)
                    EditorGUILayout.HelpBox(r.Title + "\n" + r.Detail, ToMessageType(r.Level));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ---- 3. Push-received handler sample ------------------------------------

    private void DrawHandlerSection()
    {
        _showHandler = EditorGUILayout.BeginFoldoutHeaderGroup(_showHandler, "3. Push-Received Handler (sample)");
        if (_showHandler)
        {
            EditorGUILayout.HelpBox(
                "Receive-time analytics now run natively in the Beamable library, so the package no " +
                "longer auto-wires a handler (Decision Q8). If you need custom closed-app logic on " +
                "push receipt, generate a SAMPLE PushNotificationReceivedHandler to customize, then " +
                "wire its <meta-data> into your AndroidManifest yourself.",
                MessageType.None);

            if (GUILayout.Button("Generate & Open Sample Handler", GUILayout.Height(26)))
            {
                string path = BeamableAndroidSetup.GenerateSampleReceivedHandler();
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset != null) AssetDatabase.OpenAsset(asset);
                _lastSummary = "Generated sample handler at " + path +
                               " — edit it, then add its class to the manifest meta-data '" +
                               BeamableAndroidSetup.HandlerMetaKey + "' to activate.";
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ---- 4. google-services.json guidance -----------------------------------

    private void DrawFcmSection()
    {
        _showFcm = EditorGUILayout.BeginFoldoutHeaderGroup(_showFcm, "4. google-services.json (remote / FCM)");
        if (_showFcm)
        {
            bool present = System.IO.File.Exists(BeamableAndroidSetup.GoogleServicesPath);
            EditorGUILayout.HelpBox(
                present
                    ? "google-services.json found — remote push (FCM) will be wired automatically at build."
                    : "No google-services.json — the app builds in LOCAL-ONLY mode (local notifications " +
                      "still work, remote/FCM push is disabled).",
                present ? MessageType.Info : MessageType.Warning);

            EditorGUILayout.LabelField("How to enable remote push:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "1. Create (or open) a Firebase project at console.firebase.google.com.\n" +
                "2. Add an Android app whose package name matches your build's applicationId.\n" +
                "3. Download the generated google-services.json.\n" +
                "4. Place it at: " + BeamableAndroidSetup.GoogleServicesPath + "\n" +
                "5. Rebuild — the build processor copies it in and applies the google-services plugin.",
                EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Open Firebase setup guide", GUILayout.Width(220)))
                Application.OpenURL(FcmGuideUrl);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private static MessageType ToMessageType(BeamableAndroidSetup.Level level)
    {
        switch (level)
        {
            case BeamableAndroidSetup.Level.Error: return MessageType.Error;
            case BeamableAndroidSetup.Level.Warn: return MessageType.Warning;
            default: return MessageType.Info;
        }
    }
}
