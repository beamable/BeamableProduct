using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor helper that generates a ready-to-build scene for <see cref="BeamableNativeSample"/>
/// and registers it as the first (boot) scene in Build Settings, so an Android build launches
/// straight into the native test harness. Re-runnable; de-dupes its own build-settings entry.
/// </summary>
public static class BeamableNativeSampleSceneCreator
{
    private const string SceneDir = "Assets/Beamable/Scenes";
    private const string ScenePath = SceneDir + "/BeamableNativeSample.unity";

    [MenuItem("Tools/Beamable/Android/Create Native Sample Scene")]
    public static void CreateSampleScene()
    {
        // Don't silently discard the user's current scene edits.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var go = new GameObject("BeamableNativeSampleUI");
        go.AddComponent<BeamableNativeSample>();

        if (!Directory.Exists(SceneDir))
            Directory.CreateDirectory(SceneDir);

        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            EditorUtility.DisplayDialog("Beamable Native Sample",
                "Failed to save the sample scene at " + ScenePath, "OK");
            return;
        }

        AddSceneToBuildSettingsFirst(ScenePath);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Beamable Native Sample",
            "Created " + ScenePath + " and set it as the first scene in Build Settings.\n\n" +
            "Switch the platform to Android and Build to produce the APK.", "OK");
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
