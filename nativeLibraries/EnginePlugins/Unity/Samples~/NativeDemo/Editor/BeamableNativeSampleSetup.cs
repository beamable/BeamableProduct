using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click setup for the Native Demo sample. Runs the Android setup (scaffolds/patches the
/// AndroidManifest — wiring the sample's <c>DiscordWebhookPushHandler</c> — sets min SDK, disables
/// stale gradle toggles) and registers the sample's shipped <c>NativeDemo.unity</c> as the first
/// (boot) scene in Build Settings, so an Android build launches straight into the test harness.
/// The scene ships with the sample (no generation); this only configures the project. Idempotent.
/// </summary>
public static class BeamableNativeSampleSetup
{
    // GUID of NativeDemo.unity shipped next to this script — stable wherever the sample is imported.
    private const string SceneGuid = "f68fd1a6a0544c3d8170853189cfedee";

    [MenuItem("Tools/Beamable/Android/Set Up Native Sample")]
    public static void SetUp()
    {
        var setupChanges = BeamableAndroidSetup.ApplySettings(BeamableAndroidSetup.SampleHandlerClass);

        string scenePath = AssetDatabase.GUIDToAssetPath(SceneGuid);
        if (string.IsNullOrEmpty(scenePath))
        {
            EditorUtility.DisplayDialog("Beamable Native Sample",
                "Android project configured:\n• " + string.Join("\n• ", setupChanges) +
                "\n\nThe demo scene wasn't found. Import the \"Native Demo\" sample from " +
                "Package Manager → Beamable Notifications → Samples, then run this again to add it " +
                "to Build Settings.", "OK");
            return;
        }

        AddSceneToBuildSettingsFirst(scenePath);

        EditorUtility.DisplayDialog("Beamable Native Sample",
            "Set up the Android project and added the demo scene to Build Settings.\n\nSetup:\n• " +
            string.Join("\n• ", setupChanges) +
            "\n\nScene: " + scenePath +
            "\n\nSwitch the platform to Android and Build to produce the APK.", "OK");
    }

    // Puts the sample scene at index 0 (enabled) and keeps the rest, removing any prior copy.
    private static void AddSceneToBuildSettingsFirst(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(path, true)
        };

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path != path)
                scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
