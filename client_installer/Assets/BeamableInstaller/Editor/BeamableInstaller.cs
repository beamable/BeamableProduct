using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beamable.Installer.SmallerJSON;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace Beamable.Installer.Editor
{
    public static class BeamableInstaller
    {
        private const string ManifestPath = "Packages/manifest.json";

        private const string BeamableScope = "com.beamable";
        private const string BeamablePackageName = "com.beamable";
        private const string BeamableServerPackageName = "com.beamable.server";
        private const string LogPrefix = "Installing Beamable...";
        private const string BeamableTitle = "Beamable";
        private const string SessionStateKey_FrozenAssets = "Beamable_Installer_FrozeAssets";
        private const string BeamVersionsCacheKey = "Beamable_versions_registryAll";
        private const string BeamVersionsLatestVersionFormatKey = "Beamable_versions_latest{0}BuildKey";
        private const string BeamableVersionKey = "Beamable_version_installed";
        private const string BeamableRegistryUrl_UnityAll =
            "https://nexus.beamable.com/nexus/content/repositories/unity-all";

        private const string BeamableRegistryUrl_UnityDev =
            "https://nexus.beamable.com/nexus/content/repositories/unity-dev";

        private const string BeamableRegistryUrl_UnityRC =
            "https://nexus.beamable.com/nexus/content/repositories/unity-preview";

        private const string BeamableRegistryUrl_UnityStable =
            "https://nexus.beamable.com/nexus/content/repositories/unity";

        public const string BeamableMenuPath = "Window/Beamable/Utilities/SDK Installer/";

        /// True if `com.beamable` is installed in any version
        public static bool Installed => !string.IsNullOrWhiteSpace(InstalledVersion);
        /// Installed version of `com.beamable` package
        public static string InstalledVersion => SessionState.GetString(BeamableVersionKey, string.Empty);
        

        private static readonly HashSet<Request> _pendingCommands = new HashSet<Request>();
        public static bool IsBusy => _pendingCommands.Count > 0;
        private static bool _isUninstalling;
        public static bool InstallServerPackage = true;
        public static bool InstallDependencies  = true;
        private static BeamVersion[] _versionCache = null;
        public static BeamVersion[] BeamVersionsCache
        {
            get
            {
                if (_versionCache != null) return _versionCache;
                var value = SessionState.GetString(BeamVersionsCacheKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _versionCache = Json.Deserialize(value) as BeamVersion[];
                }

                return _versionCache;
            }
            set
            {
                _versionCache = value;
                var versions = Json.Serialize(value, new StringBuilder());
                SessionState.SetString(BeamVersionsCacheKey, versions);
            }
        }

        [Serializable]
        public class BeamVersion
        {
            public string version;
            public VersionType versionType;
            public DateTime Date;
        }


        static BeamableInstaller()
        {
            UpdateInstalledVersionInfo(_ => {});
            UpdateBeamableVersionsCache();
        }

        public static readonly ArrayDict BeamableRegistryDict_UnityAll = new ArrayDict
        {
            { "name", BeamableTitle },
            { "url", BeamableRegistryUrl_UnityAll },
            { "scopes", new[] { BeamableScope } }
        };

        public static readonly ArrayDict BeamableRegistryDict_UnityRC = new ArrayDict
        {
            { "name", BeamableTitle }, { "url", BeamableRegistryUrl_UnityRC }, { "scopes", new[] { BeamableScope } }
        };

        public static readonly ArrayDict BeamableRegistryDict_UnityDev = new ArrayDict
        {
            { "name", BeamableTitle },
            { "url", BeamableRegistryUrl_UnityDev },
            { "scopes", new[] { BeamableScope } }
        };

        public static readonly ArrayDict BeamableRegistryDict_UnityStable = new ArrayDict
        {
            { "name", BeamableTitle },
            { "url", BeamableRegistryUrl_UnityStable },
            { "scopes", new[] { BeamableScope } }
        };


        [MenuItem(BeamableMenuPath + "Install Beamable SDK", true)]
        public static bool InstallValidate()
        {
            return !Installed;
        }

        [MenuItem(BeamableMenuPath + "Install Beamable SDK", priority = 110)]
        public static void Install()
        {
            InstallStable();
        }

        public static void InstallRegistryAndPackage(ArrayDict registry = null, string version = null,
            Action onFail = null)
        {
            Debug.Log($"{LogPrefix}");
            CheckManifest(registry);
            CheckPackage(registry, version, onFail);
            SessionState.SetBool("BEAM_INSTALL_DEPS", InstallDependencies);
        }

        public static void InstallDev()
        {
            _ = TryGetLatestVersionForType(VersionType.Nightly, out var version);
            InstallRegistryAndPackage(BeamableRegistryDict_UnityDev, version);
        }

        public static void InstallRC()
        {
            InstallRegistryAndPackage(BeamableRegistryDict_UnityRC);
        }

        public static void InstallStable()
        {
            InstallRegistryAndPackage(BeamableRegistryDict_UnityStable, null, () =>
            {
                Debug.Log($"{LogPrefix} Stable build not available. Installing Rc build instead.");
                InstallRC(); // if the stable build failed, we should at least try the rc.
            });
        }

        public static void InstallAll()
        {
            InstallRegistryAndPackage(BeamableRegistryDict_UnityAll);
        }

        public static void InstallSpecific(string version)
        {
            InstallRegistryAndPackage(BeamableRegistryDict_UnityAll, version);
        }

        public static void RunAction(InstallerActionType type)
        {
            switch (type)
            {
                case InstallerActionType.Install:
                    Install();
                    break;
                case InstallerActionType.Remove:
                    RemoveSelf();
                    break;
                case InstallerActionType.OpenToolbox:
                    ShowToolboxWindow();
                    break;
            }
        }

        [MenuItem(BeamableMenuPath + "Remove Installer Asset Package", priority = 120)]
        public static void RemoveSelf()
        {
            if (_isUninstalling)
                return;
            _isUninstalling = true;

            EditorUtility.DisplayDialog("Beamable Package Installer",
                "Unity is currently removing the Beamable Package Installer", "Ok");

            var assets = AssetDatabase.FindAssets($"Beamable.Installer t:{nameof(AssemblyDefinitionAsset)}");
            var asset = assets[0];
            var selfPath = AssetDatabase.GUIDToAssetPath(asset);
            var parentDir = Directory.GetParent(selfPath);
            var metaFile = parentDir + ".meta";
            File.Delete(metaFile);
            Directory.Delete(parentDir.FullName, true);
            AssetDatabase.Refresh();
        }

        private static void CheckPackage(ArrayDict registry, string version = null, Action onFail = null)
        {
            UpdateInstalledVersionInfo(beamableVersion =>
            {
                if (!string.IsNullOrWhiteSpace(beamableVersion))
                {
                    Debug.Log($"{LogPrefix} Beamable is already installed. ");
                    return;
                }

                var shouldInstallServer = InstallServerPackage;

                if (shouldInstallServer)
                {
                    PrefetchPackageManifestFile(registry, BeamableServerPackageName, beamableVersion, serverManifest =>
                    {
                        var isServerObsolete = serverManifest["description"]?.ToString()?.ToLowerInvariant()
                            .Contains("obsolete") ?? false;
                        InstallPackageHelper(!isServerObsolete);
                    });
                }
                else
                {
                    InstallPackageHelper(false);
                }

                void InstallPackageHelper(bool installServer)
                {
                    InstallPackage(installServer, err =>
                    {
                        if (err != null)
                        {
                            Debug.LogError(err);
                            onFail?.Invoke();
                        }
                    }, version);
                }
                
            });
        }

        private static void RunLater(Action continuation, int framesLater)
        {
            void Check()
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

                framesLater--;
                if (framesLater < 0)
                {
                    EditorApplication.update -= Check;
                    continuation();
                }
            }

            EditorApplication.update += Check;
        }

        private static bool FrozenAssets
        {
            get => SessionState.GetBool(SessionStateKey_FrozenAssets, false);
            set => SessionState.SetBool(SessionStateKey_FrozenAssets, value);
        }

        private static void InstallPackage(bool installServerPackage, Action<Exception> onComplete, string version = null)
        {
            var versionString = string.IsNullOrEmpty(version) ? "" : $"@{version}";


            var packages = new Queue<string>();
            packages.Enqueue(BeamablePackageName + versionString);

            if (installServerPackage)
            {
                packages.Enqueue(BeamableServerPackageName + versionString);
            }

            Debug.Log($"{LogPrefix} Beamable is not installed yet. Adding. {string.Join(" and ", packages)}");

            void InstallOne()
            {
                if (packages.Count == 1 && FrozenAssets)
                {
                    FrozenAssets = false;
                    AssetDatabase.StopAssetEditing();
                    RunLater(InstallOne, 1);
                }
                if (packages.Count == 0)
                {
                    RunLater(() => onComplete(null), 1);
                    return; // done installing.
                }

                var packageString = packages.Dequeue();

                var installReq = Client.Add(packageString);

                EditorUtility.DisplayProgressBar($"Installing Beamable package=[{packageString}]...", "", .2f);

                _pendingCommands.Add(installReq);
                EditorApplication.update += Check;

                void Check()
                {
                    EditorUtility.DisplayProgressBar($"Installing Beamable package=[{packageString}]...", "", .2f);

                    if (!installReq.IsCompleted) return;

                    EditorUtility.ClearProgressBar();

                    _pendingCommands.Remove(installReq);
                    EditorApplication.update -= Check;
                    var isSuccess = installReq.Status == StatusCode.Success;

                    if (!isSuccess)
                    {
                        var err = new Exception("Unable to add Beamable package. " + packageString + " -> " + installReq.Error.message);
                        if (FrozenAssets)
                        {
                            FrozenAssets = false;
                            AssetDatabase.StopAssetEditing();
                        }
                        onComplete?.Invoke(err);
                        return;
                    }

                    Debug.Log($"{LogPrefix} Installed Beamable package! package=[{packageString}] version=[{installReq.Result.version}]");

                    RunLater(InstallOne, 1);
                }
            }

            if (packages.Count > 1)
            {
                AssetDatabase.StartAssetEditing(); // freeze the assets...
                FrozenAssets = true;
            }
            RunLater(InstallOne, 1);
        }

        public static void PrefetchPackageManifestFile(ArrayDict registry, string package, string versionString, Action<ArrayDict> packageManifestCallback)
        {
            var registryUrl = registry["url"].ToString();
            var request = UnityWebRequest.Get($"{registryUrl}/{package}/{versionString}");

            request.SendWebRequest();
            EditorApplication.update += Check;
                
            void Check()
            {
                if (!request.isDone) return;

                try
                {
                    var manifest = Json.Deserialize(request.downloadHandler.text) as ArrayDict;
                    packageManifestCallback?.Invoke(manifest);
                }
                finally
                {
                    EditorApplication.update -= Check;
                }
            }
        }
        
        public static void UpdateInstalledVersionInfo(Action<string> beamableVersionCallback)
        {
            var listReq = Client.List(true);
            _pendingCommands.Add(listReq);
            EditorApplication.update += Check;

            void Check()
            {
                if (!listReq.IsCompleted) return;

                _pendingCommands.Remove(listReq);
                EditorApplication.update -= Check;

                var isSuccess = listReq.Status == StatusCode.Success;
                if (!isSuccess)
                {
                    throw new Exception("Unable to list local packages. " + listReq.Error.message);
                }
                var version = listReq.Result.FirstOrDefault(p => p.name.Equals(BeamablePackageName));
                var versionString = version == null ? string.Empty : version.version;
                SessionState.SetString(BeamableVersionKey, versionString);
                beamableVersionCallback(versionString);
            }
        }

        private static void ShowToolboxWindow()
        {
            var list = new (string,string, string)[]
            {
                ("Beamable.Editor.Login.UI", "LoginWindow", "FocusOrShow") ,
                ("Beamable.Editor.Toolbox.UI", "ToolboxWindow", "Init") ,
            };
            foreach (var p in list)
            {
                if (TryGetType(p.Item1, p.Item2, out var windowType))
                {
                    var initMethodInfo = windowType.GetMethod(p.Item3, new Type[]{});
                    initMethodInfo?.Invoke(null, new object[] { });
                    return;
                }
            }
        }

        private static bool TryGetType(string namespaceName, string className, out Type type)
        {
            type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes())
                .FirstOrDefault(t => t.IsClass && t.Namespace == namespaceName && t.Name == className);
            return type != null;
        }

        private static void CheckManifest(ArrayDict registry = null)
        {
            var manifestJson = File.ReadAllText(ManifestPath);
            Debug.Log($"{LogPrefix} loading manifest.json ... \n{manifestJson}");

            var manifestWithBeamable = EnsureScopedRegistryJson(manifestJson, registry);
            try
            {
                CheckoutPath(ManifestPath);
                File.WriteAllText(ManifestPath, manifestWithBeamable);
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Couldn't write manifest file. {ex.Message}");
            }
        }

        private static void UpdateBeamableVersionsCache()
        {
            if(BeamVersionsCache != null)
                return;
            var requests = new List<UnityWebRequest>
            {
                UnityWebRequest.Get($"{BeamableRegistryUrl_UnityStable}/-/all"),
                UnityWebRequest.Get($"{BeamableRegistryUrl_UnityDev}/-/all"),
                UnityWebRequest.Get($"{BeamableRegistryUrl_UnityRC}/-/all")
            };
            foreach (var webRequest in requests)
            {
                webRequest.SendWebRequest();
            }

            EditorApplication.update += Check;
            void Check()
            {
                if(!requests.All(request => request.isDone)) return;
                EditorApplication.update -= Check;

                var versionsAll = new List<BeamVersion>();
                foreach (var webRequest in requests)
                {
                    var manifest = Json.Deserialize(webRequest.downloadHandler.text) as ArrayDict;
                    manifest.TryGetValue(BeamableScope, out var packageScope);
                    if (!(packageScope is ArrayDict scope)) continue;
                    scope.TryGetValue("versions", out var versions);
                    if (!(versions is ArrayDict dict)) continue;
                    // versionsAll.AddRange(dict.Keys);
                    var gotVersionType = TryGetVersionTypeFromUrl(webRequest.url, out var versionType);
                    if (TryGetLatestBuild(scope, out var latestBuild) && gotVersionType)
                    {
                        SetLatestVersionForType(versionType, latestBuild);
                    }
                    scope.TryGetValue("time", out var time);
                    if (!(time is ArrayDict times)) continue;
                    foreach(var k in dict.Keys)
                    {
                        if (times.TryGetValue(k, out var timeValue) && DateTime.TryParse(timeValue as string, out var date))
                        {
                            versionsAll.Add(new BeamVersion()
                            {
                                Date = date,
                                version = k,
                                versionType = versionType
                            });
                        }
                    }
                }
                BeamVersionsCache = versionsAll.OrderByDescending(v=> v.Date).ToArray();
            }

            bool TryGetLatestBuild(ArrayDict scope, out string key)
            {
                key = null;
                if (!scope.TryGetValue("time", out var time))return false;

                if (!(time is ArrayDict d)) return false;
                if(!d.TryGetValue("modified", out var modifiedKey)) return false;
                if (!(modifiedKey is string k)) return false;
                var latestBuild = d.FirstOrDefault(pair => (string)pair.Value == k && !pair.Key.Equals("modified"));
                if (latestBuild.Key == null) return false;
                key = latestBuild.Key;
                return true;
            }
        }

        public static string EnsureScopedRegistryJson(string manifestJson, ArrayDict registry = null)
        {
            if (registry == null) registry = BeamableRegistryDict_UnityStable;

            var manifest = Json.Deserialize(manifestJson) as ArrayDict;

            if (!manifest.TryGetValue("scopedRegistries", out var scopedRegistries))
            {
                // need to add empty scoped registries..
                scopedRegistries = new[] { registry };
                manifest["scopedRegistries"] = scopedRegistries;
            }
            else if (scopedRegistries is IList scopedRegistryList)
            {
                var foundBeamable = false;
                for (var i = 0; i < scopedRegistryList.Count; i++)
                {
                    var scopedRegistry = scopedRegistryList[i];
                    if (!(scopedRegistry is ArrayDict scopedRegistryDict) ||
                        !scopedRegistryDict.TryGetValue("name", out var scopedRegistryName) ||
                        !scopedRegistryName.Equals(BeamableTitle)) continue;

                    foundBeamable = true;
                    scopedRegistryList[i] = registry;
                    break;
                }

                if (!foundBeamable) scopedRegistryList.Add(registry);
            }
            else
            {
                throw new Exception("Invalid manifest json file.");
            }


            var reJsonified = Json.Serialize(manifest, new StringBuilder());

            return reJsonified;
        }

        public static void CheckoutPath(string path)
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                fileInfo.IsReadOnly = false;
            }

            if (!Provider.enabled) return;
            var vcTask = Provider.Checkout(path, CheckoutMode.Asset);
            vcTask.Wait();
            if (!vcTask.success) Debug.LogWarning($"Unable to checkout: {path}");
        }

        public static void SetLatestVersionForType(VersionType versionType, string value)
        {
            SessionState.SetString(LatestVersionKeyForType(versionType), value);
        }

        public static bool TryGetLatestVersionForType(VersionType versionType, out string value)
        {
            value = SessionState.GetString(LatestVersionKeyForType(versionType), null);
            return !string.IsNullOrEmpty(value);
        }

        static string LatestVersionKeyForType(VersionType versionType)
        {
            return string.Format(BeamVersionsLatestVersionFormatKey, versionType.ToString());
        }

        static bool TryGetVersionTypeFromUrl(string url, out VersionType versionType)
        {
            versionType = VersionType.Any;
            if(url.Contains(BeamableRegistryUrl_UnityAll))
            {
                return false;
            }
            if(url.Contains(BeamableRegistryUrl_UnityDev)){
                versionType = VersionType.Nightly;
            }
            else if(url.Contains(BeamableRegistryUrl_UnityRC)){
                versionType = VersionType.Rc;
            }else if (url.Contains(BeamableRegistryUrl_UnityStable))
            {
                versionType = VersionType.Stable;
            }
            else
            {
                return false;
            }
            return true;
        }
        
        [Serializable]
        public enum VersionType
        {
            Stable,
            Rc,
            Nightly,
            Any
        }
    }
}