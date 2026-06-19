using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that sets up and validates the Beamable native Android libraries.
/// Menu: Tools/Beamable/Android/Setup &amp; Validation.
///
/// The same checks/fixes run automatically at build via BeamableAndroidBuildProcessor; this
/// window lets you run them on demand and see a readable report.
/// </summary>
public class BeamableAndroidSetupWindow : EditorWindow
{
    private List<BeamableAndroidSetup.CheckResult> _results;
    private Vector2 _scroll;
    private string _lastSetupSummary;

    [MenuItem("Tools/Beamable/Android/Setup & Validation")]
    public static void Open()
    {
        var w = GetWindow<BeamableAndroidSetupWindow>(true, "Beamable Android", true);
        w.minSize = new Vector2(460, 360);
        w.Revalidate();
    }

    private void OnEnable() => Revalidate();

    private void Revalidate() => _results = BeamableAndroidSetup.Validate();

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Beamable Native Android Libraries", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Push Notifications + Deeplink (prebuilt .aar)", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Run Setup", GUILayout.Height(28)))
            {
                var changes = BeamableAndroidSetup.ApplySettings();
                _lastSetupSummary = string.Join("\n", changes);
                Revalidate();
            }
            if (GUILayout.Button("Validate", GUILayout.Height(28)))
                Revalidate();
        }

        if (!string.IsNullOrEmpty(_lastSetupSummary))
            EditorGUILayout.HelpBox("Setup applied:\n" + _lastSetupSummary, MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_results != null)
        {
            foreach (var r in _results)
            {
                EditorGUILayout.HelpBox(r.Title + "\n" + r.Detail, ToMessageType(r.Level));
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "AARs are built from D:\\Repositories\\BeamableProduct\\nativeLibraries\\Android (assembleRelease) and " +
            "dropped into Assets/Plugins/Android. Gradle config (deps, AndroidX, Firebase) is " +
            "injected automatically at build — no custom gradle templates required. Add " +
            "google-services.json to enable remote (FCM) push; omit it for local-only.",
            MessageType.None);
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
