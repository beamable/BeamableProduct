#if UNITY_ANDROID
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Drives the Beamable native Android library entirely from editor code — no committed
/// Gradle template files needed.
///
///  - <see cref="IPreprocessBuildWithReport"/>: at build start, auto-applies fixable settings
///    (scaffolds the manifest if missing, minSdk, ProjectSettings toggles) and validates.
///  - <see cref="IPostGenerateGradleAndroidProject"/>: after Unity generates the Gradle project,
///    injects the library's transitive Maven deps, ensures AndroidX, and wires Firebase
///    (google-services) only when a google-services.json is present.
///
/// The unified beamable-notifications-release.aar ships with the com.beamable.notifications package
/// (Plugins/Android), so no .aar needs to live in the consuming project.
/// </summary>
public class BeamableAndroidBuildProcessor
    : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    private const string DepsMarker = "BEAMABLE-DEPS";
    private const string GoogleServicesVersion = "4.4.2";
    private const string KotlinVersion = "1.9.22";
    private const string FirebaseBom = "33.7.0";

    // ---- Pre-build: auto-check + auto-apply --------------------------------

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        foreach (var change in BeamableAndroidSetup.ApplySettings())
            Debug.Log("[BeamableAndroid] setup: " + change);

        foreach (var r in BeamableAndroidSetup.Validate())
        {
            switch (r.Level)
            {
                case BeamableAndroidSetup.Level.Error:
                    Debug.LogError("[BeamableAndroid] " + r.Title + " — " + r.Detail);
                    break;
                case BeamableAndroidSetup.Level.Warn:
                    Debug.LogWarning("[BeamableAndroid] " + r.Title + " — " + r.Detail);
                    break;
                default:
                    Debug.Log("[BeamableAndroid] " + r.Title + " — " + r.Detail);
                    break;
            }
        }
    }

    // ---- Post-generate: inject gradle config -------------------------------

    public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
    {
        string root = Directory.GetParent(unityLibraryPath).FullName;

        InjectDependencies(Path.Combine(unityLibraryPath, "build.gradle"));
        EnsureAndroidX(Path.Combine(root, "gradle.properties"));
        ConfigureFirebase(root, unityLibraryPath);
    }

    private static void InjectDependencies(string buildGradle)
    {
        if (!File.Exists(buildGradle))
        {
            Debug.LogWarning("[BeamableAndroid] unityLibrary build.gradle not found at " + buildGradle);
            return;
        }

        string content = File.ReadAllText(buildGradle);
        if (content.Contains(DepsMarker))
            return; // already injected

        // Append a standalone dependencies{} block at end-of-file. This is robust regardless of
        // the template's block ordering — by EOF the com.android.library plugin is applied, so
        // the `implementation` configuration exists. (Inserting after the first "dependencies {"
        // is unsafe: in some templates that match is the buildscript{} block, where only
        // `classpath` is valid — which fails with "Could not find method implementation()".)
        string insertion =
            "\n// " + DepsMarker + " — transitive deps of the prebuilt beamable-notifications-release.aar\n" +
            "dependencies {\n" +
            "    implementation 'org.jetbrains.kotlin:kotlin-stdlib:" + KotlinVersion + "'\n" +
            "    implementation 'androidx.core:core-ktx:1.12.0'\n" +
            "    implementation platform('com.google.firebase:firebase-bom:" + FirebaseBom + "')\n" +
            "    implementation 'com.google.firebase:firebase-messaging-ktx'\n" +
            "}\n";

        File.AppendAllText(buildGradle, insertion);
        Debug.Log("[BeamableAndroid] injected transitive deps into unityLibrary/build.gradle");
    }

    private static void EnsureAndroidX(string gradleProperties)
    {
        var lines = File.Exists(gradleProperties)
            ? new List<string>(File.ReadAllLines(gradleProperties))
            : new List<string>();

        SetProperty(lines, "android.useAndroidX", "true");
        SetProperty(lines, "android.enableJetifier", "true");
        File.WriteAllLines(gradleProperties, lines);
    }

    private static void SetProperty(List<string> lines, string key, string value)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith(key + "="))
            {
                lines[i] = key + "=" + value;
                return;
            }
        }
        lines.Add(key + "=" + value);
    }

    private static void ConfigureFirebase(string root, string unityLibraryPath)
    {
        string json = Path.GetFullPath(BeamableAndroidSetup.GoogleServicesPath);
        if (!File.Exists(json))
        {
            Debug.Log("[BeamableAndroid] no google-services.json — local-only build (FCM disabled).");
            return;
        }

        // Copy the config into the modules that may host the google-services plugin.
        CopyJson(json, Path.Combine(unityLibraryPath, "google-services.json"));
        string launcher = Path.Combine(root, "launcher");
        if (Directory.Exists(launcher))
            CopyJson(json, Path.Combine(launcher, "google-services.json"));

        AddGoogleServicesPlugin(Path.Combine(root, "build.gradle"));
        ApplyGoogleServices(Path.Combine(launcher, "build.gradle"));
    }

    private static void CopyJson(string src, string dest)
    {
        File.Copy(src, dest, overwrite: true);
        Debug.Log("[BeamableAndroid] copied google-services.json -> " + dest);
    }

    // Add the google-services plugin to the root project build.gradle. Handles BOTH Unity
    // base-template formats: the plugins{} DSL (Unity 2021.3.40+/2022.3, AGP 7.4.2) and the
    // older buildscript{ dependencies { classpath ... } } form (earlier 2021.3, AGP 7.1.2).
    private static void AddGoogleServicesPlugin(string rootBuildGradle)
    {
        if (!File.Exists(rootBuildGradle))
            return;
        string content = File.ReadAllText(rootBuildGradle);
        if (content.Contains("com.google.gms.google-services") || content.Contains("com.google.gms:google-services"))
            return;

        // Preferred: plugins{} DSL block.
        int pluginsIdx = content.IndexOf("plugins {");
        if (pluginsIdx >= 0)
        {
            int insertAt = content.IndexOf('\n', pluginsIdx) + 1;
            string line = "    id 'com.google.gms.google-services' version '" +
                          GoogleServicesVersion + "' apply false\n";
            content = content.Insert(insertAt, line);
            File.WriteAllText(rootBuildGradle, content);
            Debug.Log("[BeamableAndroid] added google-services to root plugins{} block");
            return;
        }

        // Fallback: legacy buildscript{ dependencies { classpath ... } }.
        int depsIdx = content.IndexOf("dependencies {");
        if (depsIdx >= 0)
        {
            int insertAt = content.IndexOf('\n', depsIdx) + 1;
            string line = "        classpath 'com.google.gms:google-services:" +
                          GoogleServicesVersion + "'\n";
            content = content.Insert(insertAt, line);
            File.WriteAllText(rootBuildGradle, content);
            Debug.Log("[BeamableAndroid] added google-services classpath to buildscript{}");
            return;
        }

        Debug.LogWarning("[BeamableAndroid] root build.gradle has neither plugins{} nor a buildscript dependencies{} block; could not wire google-services.");
    }

    private static void ApplyGoogleServices(string launcherBuildGradle)
    {
        if (!File.Exists(launcherBuildGradle))
            return;
        string content = File.ReadAllText(launcherBuildGradle);
        if (content.Contains("apply plugin: 'com.google.gms.google-services'"))
            return;
        content += "\napply plugin: 'com.google.gms.google-services'\n";
        File.WriteAllText(launcherBuildGradle, content);
        Debug.Log("[BeamableAndroid] applied google-services in launcher/build.gradle");
    }
}
#endif
