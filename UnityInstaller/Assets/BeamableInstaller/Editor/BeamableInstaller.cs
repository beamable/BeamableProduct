using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Beamable.Installer.SmallerJSON;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using Json = Beamable.Installer.SmallerJSON.Json;

namespace Beamable.Installer.Editor
{
   public static class BeamableInstaller
   {
      private const string ManifestPath = "Packages/manifest.json";
//      private const string BeamableScope = "com.disruptorbeam";
//      private const string BeamablePackageName = "com.disruptorbeam.engine";
      private const string BeamableScope = "com.beamable";
      private const string BeamablePackageName = "com.beamable";
      private const string LogPrefix = "Installing Beamable...";
      private const string BeamableTitle = "Beamable";
      private const string BeamableRegistryUrl_UnityAll = "https://nexus.beamable.com/nexus/content/repositories/unity-all";
      private const string BeamableRegistryUrl_UnityDev = "https://nexus.beamable.com/nexus/content/repositories/unity-dev";
      private const string BeamableRegistryUrl_UnityRC = "https://nexus.beamable.com/nexus/content/repositories/unity-preview";
      private const string BeamableRegistryUrl_UnityStable = "https://nexus.beamable.com/nexus/content/repositories/unity";

      public const string BeamableMenuPath = "Window/Beamable/Utilities/SDK Installer/";

      private static bool Installed = true; // true until validated false...

      private static HashSet<Request> _pendingCommands = new HashSet<Request>();
      public static bool IsBusy => _pendingCommands.Count > 0;

      static BeamableInstaller()
      {
         HasBeamableInstalled(installed => Installed = installed);
      }

      public static readonly ArrayDict BeamableRegistryDict_UnityAll = new ArrayDict
      {
         {"name", BeamableTitle},
         {"url", BeamableRegistryUrl_UnityAll},
         {"scopes", new []
         {
            BeamableScope
         }},
      };
      public static readonly ArrayDict BeamableRegistryDict_UnityRC = new ArrayDict
      {
         {"name", BeamableTitle},
         {"url", BeamableRegistryUrl_UnityRC},
         {"scopes", new []
         {
            BeamableScope
         }},
      };
      public static readonly ArrayDict BeamableRegistryDict_UnityDev = new ArrayDict
      {
         {"name", BeamableTitle},
         {"url", BeamableRegistryUrl_UnityDev},
         {"scopes", new []
         {
            BeamableScope
         }},
      };
      public static readonly ArrayDict BeamableRegistryDict_UnityStable = new ArrayDict
      {
         {"name", BeamableTitle},
         {"url", BeamableRegistryUrl_UnityStable},
         {"scopes", new []
         {
            BeamableScope
         }},
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

      public static void InstallRegistryAndPackage(ArrayDict registry=null, string version=null, Action onFail=null)
      {
         Debug.Log($"{LogPrefix}");
         CheckManifest(registry);
         CheckPackage(version, onFail);
      }

      public static void InstallDev() => InstallRegistryAndPackage(BeamableRegistryDict_UnityDev);
      public static void InstallRC() => InstallRegistryAndPackage(BeamableRegistryDict_UnityRC);

      public static void InstallStable()
      {

         InstallRegistryAndPackage(BeamableRegistryDict_UnityStable, version: null, onFail: () =>
         {
            Debug.Log($"{LogPrefix} Stable build not available. Installing Rc build instead.");
            InstallRC(); // if the stable build failed, we should at least try the rc.
         });
      }

      public static void InstallAll() => InstallRegistryAndPackage(BeamableRegistryDict_UnityAll);

      public static void InstallSpecific(string version)
      {
         InstallRegistryAndPackage(BeamableRegistryDict_UnityAll, version);
      }

      public static void RunAction(InstallerActionType type)
      {
         AssetDatabase.StartAssetEditing();
         try
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
                  OpenToolboxWindow();
                  break;
               default:
                  break;
            }
         }
         finally
         {
            AssetDatabase.StopAssetEditing();
         }
      }

      private static void OpenToolboxWindow()
      {
         const string namespaceName = "Beamable.Editor.Toolbox.UI";
         const string className = "ToolboxWindow";
         const string methodName = "Init";

         var toolboxWindowType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(t => t.GetTypes())
            .FirstOrDefault(t => t.IsClass && t.Namespace == namespaceName && t.Name == className);

         if (toolboxWindowType != null)
         {
            var initMethodInfo = toolboxWindowType.GetMethod(methodName);
            initMethodInfo?.Invoke(null, new object[] { });
         }
      }

      [MenuItem(BeamableMenuPath + "Remove Installer Asset Package", priority = 120)]
      public static void RemoveSelf()
      {
         var assets = AssetDatabase.FindAssets($"Beamable.Installer t:{nameof(AssemblyDefinitionAsset)}");
         var asset = assets[0];
         var selfPath = AssetDatabase.GUIDToAssetPath(asset);
         var parentDir = Directory.GetParent(selfPath);
         var metaFile = parentDir + ".meta";
         File.Delete(metaFile);
         Directory.Delete(parentDir.FullName, true);
         AssetDatabase.Refresh();
      }

      static void CheckPackage(string version=null, Action onFail=null)
      {
         HasBeamableInstalled(hasBeamable =>
         {
            if (hasBeamable)
            {
               Debug.Log($"{LogPrefix} Beamable is already installed. ");
               return;
            }

            InstallPackage((err) =>
            {
               if (err != null)
               {
                 Debug.LogError(err);
                 onFail?.Invoke();
                 return;
               }
               Debug.Log($"{LogPrefix} Beamable is now installed. ");
            }, version);
         });
      }

      static void InstallPackage(Action<Exception> onComplete, string version=null)
      {
         var versionString = string.IsNullOrEmpty(version) ? "" : $"@{version}";

         var packageString = BeamablePackageName + versionString;
         Debug.Log($"{LogPrefix} Beamable is not installed yet. Adding package. {packageString}");


         var installReq = Client.Add(packageString);

         EditorUtility.DisplayProgressBar("Installing Beamable...", "", .2f);

         _pendingCommands.Add(installReq);
         EditorApplication.update += Check;

         Check();
         void Check()
         {
            EditorUtility.DisplayProgressBar("Installing Beamable...", "", .2f);

            if (!installReq.IsCompleted)
            {
               return;
            }
            EditorUtility.ClearProgressBar();

            _pendingCommands.Remove(installReq);
            EditorApplication.update -= Check;
            var isSuccess = installReq.Status == StatusCode.Success;
            if (!isSuccess)
            {
               var err = new Exception("Unable to add Beamable package. " + installReq.Error.message);
               onComplete?.Invoke(err);
               return;
            }

            Debug.Log($"{LogPrefix} Installed Beamable package! version=[{installReq.Result.version}]");
            onComplete(null);
         }

      }

      public static void HasBeamableInstalled(Action<bool> hasBeamableCallback)
      {
         var listReq = Client.List(true);
         _pendingCommands.Add(listReq);
         EditorApplication.update += Check;

         void Check()
         {
            if (!listReq.IsCompleted)
            {
               return;
            }

            _pendingCommands.Remove(listReq);
            EditorApplication.update -= Check;

            var isSuccess = listReq.Status == StatusCode.Success;
            if (!isSuccess)
            {
               throw new Exception("Unable to list local packages. " + listReq.Error.message);
            }

            var hasBeamable = listReq.Result.FirstOrDefault(p => p.name.Equals(BeamablePackageName)) != null;
            hasBeamableCallback(hasBeamable);
         }

      }

      static void CheckManifest(ArrayDict registry=null)
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

      public static string EnsureScopedRegistryJson(string manifestJson, ArrayDict registry=null)
      {
         if (registry == null) registry = BeamableRegistryDict_UnityStable;

         var manifest = Json.Deserialize(manifestJson) as ArrayDict;

         if (!manifest.TryGetValue("scopedRegistries", out var scopedRegistries))
         {
            // need to add empty scoped registries..
            scopedRegistries = new[] {registry};
            manifest["scopedRegistries"] = scopedRegistries;
         }
         else if (scopedRegistries is IList scopedRegistryList)
         {
            var foundBeamable = false;
            for (var i = 0 ; i < scopedRegistryList.Count ; i ++)
            {
               var scopedRegistry = scopedRegistryList[i];
               if (!(scopedRegistry is ArrayDict scopedRegistryDict) ||
                   !scopedRegistryDict.TryGetValue("name", out var scopedRegistryName) ||
                   !scopedRegistryName.Equals(BeamableTitle)) continue;

               foundBeamable = true;
               scopedRegistryList[i] = registry;
               break;
            }

            if (!foundBeamable)
            {
               scopedRegistryList.Add(registry);
            }
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
            var fileInfo = new System.IO.FileInfo(path);
            fileInfo.IsReadOnly = false;
         }

         if (!Provider.enabled) return;
         var vcTask = Provider.Checkout(path, CheckoutMode.Asset);
         vcTask.Wait();
         if (!vcTask.success)
         {
            Debug.LogWarning($"Unable to checkout: {path}");
         }
      }

   }
}