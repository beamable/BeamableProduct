#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Beamable.Notifications.Web.Editor
{
    /// <summary>
    /// Post-build step that prepares the generated Xcode project for BeamableNotifications:
    ///   - Push Notifications + Remote-notification background mode on the app
    ///   - An App Group shared by the app and the Notification Service Extension
    ///   - A Notification Service Extension target built from the SDK's NSE sources
    ///   - Swift standard libraries embedded (the core is Swift)
    ///
    /// The xcframework itself is auto-linked because it lives under Plugins/iOS.
    /// Adjust <see cref="AppGroupId"/> / <see cref="ExtensionBundleId"/> for your project.
    /// </summary>
    public static class NotificationsPostProcess
    {
        // TODO: set these to match your team's identifiers. The App Group must be
        // enabled on BOTH the app and the extension in your provisioning profiles.
        private const string AppGroupId = "group.com.beamable.notifications.web";
        private const string ExtensionName = "BeamableNotificationServiceExtension";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var project = new PBXProject();
            project.ReadFromFile(pbxPath);

            string appTargetGuid = project.GetUnityMainTargetGuid();

            ConfigureAppTarget(project, pathToBuiltProject, appTargetGuid);
            AddServiceExtension(project, pathToBuiltProject);

            project.WriteToFile(pbxPath);

            AddBackgroundModesAndPush(pathToBuiltProject, appTargetGuid);
        }

        private static void ConfigureAppTarget(PBXProject project, string buildPath, string appTargetGuid)
        {
            // Swift runtime must be embedded since the core is Swift.
            project.SetBuildProperty(appTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

            // App Group + Push entitlements on the app.
            string entitlementsName = "app.entitlements";
            var caps = new ProjectCapabilityManager(
                PBXProject.GetPBXProjectPath(buildPath), entitlementsName, null, appTargetGuid);
            caps.AddPushNotifications(development: true);
            caps.AddAppGroups(new[] { AppGroupId });
            caps.WriteToFile();
        }

        private static void AddServiceExtension(PBXProject project, string buildPath)
        {
            string extDir = Path.Combine(buildPath, ExtensionName);
            Directory.CreateDirectory(extDir);

            // Copy the SDK's NSE sources into the project. PackageSource resolves the
            // files shipped alongside this package (see ResolveExtensionSourceDir).
            string srcDir = ResolveExtensionSourceDir();
            CopySources(srcDir, extDir);

            WriteExtensionInfoPlist(Path.Combine(extDir, "Info.plist"));
            WriteExtensionEntitlements(Path.Combine(extDir, $"{ExtensionName}.entitlements"));

            string extGuid = project.AddAppExtension(
                project.GetUnityMainTargetGuid(),
                ExtensionName,
                PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS) + "." + ExtensionName,
                Path.Combine(ExtensionName, "Info.plist"));

            foreach (var file in Directory.GetFiles(extDir, "*.swift", SearchOption.AllDirectories))
            {
                string rel = ExtensionName + file.Substring(extDir.Length);
                string fileGuid = project.AddFile(file, rel, PBXSourceTree.Source);
                project.AddFileToBuild(extGuid, fileGuid);
            }

            project.SetBuildProperty(extGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            project.SetBuildProperty(extGuid, "CODE_SIGN_ENTITLEMENTS",
                Path.Combine(ExtensionName, $"{ExtensionName}.entitlements"));
            project.SetBuildProperty(extGuid, "IPHONEOS_DEPLOYMENT_TARGET", "14.0");
            project.SetBuildProperty(extGuid, "SWIFT_VERSION", "5.0");
        }

        private static void CopySources(string srcDir, string destDir)
        {
            if (!Directory.Exists(srcDir))
            {
                Debug.LogWarning($"[BeamableNotifications] NSE sources not found at {srcDir}; " +
                                 "add NotificationService.swift to the extension target manually.");
                return;
            }
            foreach (var file in Directory.GetFiles(srcDir, "*.swift", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
            }
        }

        private static string ResolveExtensionSourceDir()
        {
            // The NSE sources are packaged under the UPM package as `Plugins/iOS~/Extension`.
            // (`~` keeps Unity from importing the .swift as scripts.)
            string packageRoot = Path.GetFullPath("Packages/com.beamable.notifications.web");
            return Path.Combine(packageRoot, "Plugins/iOS~/Extension");
        }

        private static void WriteExtensionInfoPlist(string path)
        {
            var plist = new PlistDocument();
            plist.root.SetString("CFBundleDisplayName", ExtensionName);
            plist.root.SetString("CFBundleExecutable", "$(EXECUTABLE_NAME)");
            plist.root.SetString("CFBundleIdentifier", "$(PRODUCT_BUNDLE_IDENTIFIER)");
            plist.root.SetString("CFBundleName", "$(PRODUCT_NAME)");
            plist.root.SetString("CFBundlePackageType", "$(PRODUCT_BUNDLE_PACKAGE_TYPE)");
            plist.root.SetString("CFBundleShortVersionString", "1.0");
            plist.root.SetString("CFBundleVersion", "1");

            var ext = plist.root.CreateDict("NSExtension");
            ext.SetString("NSExtensionPointIdentifier", "com.apple.usernotifications.service");
            ext.SetString("NSExtensionPrincipalClass", "$(PRODUCT_MODULE_NAME).NotificationService");

            // Make the App Group discoverable to the NSE's SharedConfig.
            plist.root.SetString("BMNAppGroup", AppGroupId);

            plist.WriteToFile(path);
        }

        private static void WriteExtensionEntitlements(string path)
        {
            var entitlements = new PlistDocument();
            var groups = entitlements.root.CreateArray("com.apple.security.application-groups");
            groups.AddString(AppGroupId);
            entitlements.WriteToFile(path);
        }

        private static void AddBackgroundModesAndPush(string buildPath, string appTargetGuid)
        {
            // App Info.plist: remote-notification background mode + the App Group key for
            // the SDK's SharedConfig.
            string plistPath = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementArray modes = plist.root["UIBackgroundModes"] as PlistElementArray
                                      ?? plist.root.CreateArray("UIBackgroundModes");
            bool hasRemote = false;
            foreach (var v in modes.values)
            {
                if (v is PlistElementString s && s.value == "remote-notification") hasRemote = true;
            }
            if (!hasRemote) modes.AddString("remote-notification");

            plist.root.SetString("BMNAppGroup", AppGroupId);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
