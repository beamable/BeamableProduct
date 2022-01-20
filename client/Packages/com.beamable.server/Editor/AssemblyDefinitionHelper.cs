using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Editor.DockerCommands;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor
{
   public static class AssemblyDefinitionHelper
   {
      const string PRECOMPILED = "precompiledReferences";
      const string REFERENCES = "references";
      const string OVERRIDE_REFERENCES = "overrideReferences";
      const string NAME = "name";
      private const string ASSETS_BEAMABLE = "Assets/Beamable/";
      private const string ADD_MONGO = ASSETS_BEAMABLE + "Add Mongo Libraries";
      private const string REMOVE_MONGO = ASSETS_BEAMABLE + "Remove Mongo Libraries";
      // private const string OPEN_MONGO = ASSETS_BEAMABLE + "Open Mongo Data Explorer"; // TODO: Delete this when we have a UI
      // private const string RUN_MONGO = ASSETS_BEAMABLE + "Run Mongo"; // TODO: Delete this when we have a UI
      // private const string KILL_MONGO = ASSETS_BEAMABLE + "Kill Mongo"; // TODO: Delete this when we have a UI
      // private const string CLEAR_MONGO = ASSETS_BEAMABLE + "Clear Mongo Data"; // TODO: Delete this when we have a UI
      // private const string SNAPSHOT_MONGO = ASSETS_BEAMABLE + "Create Mongo Snapshot"; // TODO: Delete this when we have a UI
      // private const string RESTORE_MONGO = ASSETS_BEAMABLE + "Restore Mongo Snapshot"; // TODO: Delete this when we have a UI
      private const int BEAMABLE_PRIORITY = 190;

      public static readonly string[] MongoLibraries = new[]
      {
         "DnsClient.dll",
         "MongoDB.Bson.dll",
         "MongoDB.Driver.Core.dll",
         "MongoDB.Driver.dll",
         "MongoDB.Libmongocrypt.dll",
         "System.Buffers.dll",
         "System.Runtime.CompilerServices.Unsafe.dll",
         "SharpCompress.dll"
      };

      public static void RestoreMongo(StorageObjectDescriptor descriptor)
      {
         var dest = EditorUtility.OpenFolderPanel("Select where to load mongo", "", "default");
         if (string.IsNullOrEmpty(dest)) return;
         Debug.Log("Starting restore...");
         Microservices.RestoreMongoSnapshot(descriptor, dest).Then(res =>
         {
            if (res)
            {
               Debug.Log("Finished restoring");
            }
            else
            {
               Debug.Log("Failed.");
            }
         });
      }

      public static void SnapshotMongo(StorageObjectDescriptor descriptor)
      {
         var dest = EditorUtility.OpenFolderPanel("Select where to save mongo", "", "default");
         if (string.IsNullOrEmpty(dest)) return;
         Debug.Log("Starting snapshot...");
         Microservices.SnapshotMongo(descriptor, dest).Then(res =>
         {
            if (res)
            {
               Debug.Log("Finished Snapshot");
               EditorUtility.OpenWithDefaultApp(dest);
            }
            else
            {
               Debug.Log("Failed.");
            }
         });
      }

      public static void ClearMongo(StorageObjectDescriptor descriptor)
      {
         var work= Microservices.ClearMongoData(descriptor);
         work.Then(success =>
         {
            if (success)
            {
               Debug.Log($"Cleared {descriptor.Name} database.");
            }
            else
            {
               Debug.LogWarning($"Failed to clear {descriptor.Name} database.");
            }
         });
      }

      public static void OpenMongoExplorer(StorageObjectDescriptor descriptor)
      {
         Debug.Log("opening tool");
         var work = Microservices.OpenLocalMongoTool(descriptor);
         work.Then(success =>
         {
            if (success)
            {
               Debug.Log("Opened tool.");

            }
            else
            {
               Debug.LogWarning("Failed to open tool.");
            }
         });
      }

      [MenuItem(ADD_MONGO, false, BEAMABLE_PRIORITY)]
      public static void AddMongoLibraries() {
         if (Selection.activeObject is AssemblyDefinitionAsset asm)
         {
            asm.AddMongoLibraries();
         }
      }

      [MenuItem(REMOVE_MONGO, false, BEAMABLE_PRIORITY)]
      public static void RemoveMongoLibraries() {
         if (Selection.activeObject is AssemblyDefinitionAsset asm)
         {
            asm.RemoveMongoLibraries();
         }
      }

      [MenuItem(ADD_MONGO, true, BEAMABLE_PRIORITY)]
      public static bool ValidateAddMongo()
      {
         return ValidateSelectionIsMicroservice(out var asm) && !asm.HasMongoLibraries();
      }

      [MenuItem(REMOVE_MONGO, true, BEAMABLE_PRIORITY)]
      public static bool ValidateRemoveMongo()
      {
         return ValidateSelectionIsMicroservice(out var asm) && asm.HasMongoLibraries();
      }

      public static bool ValidateSelectionIsMicroservice(out AssemblyDefinitionAsset assembly)
      {
         assembly = null;
         if (!(Selection.activeObject is AssemblyDefinitionAsset asm))
         {
            return false;
         }

         assembly = asm;
         var info = asm.ConvertToInfo();
         var descriptor = Microservices.Descriptors.FirstOrDefault(d => d.IsContainedInAssemblyInfo(info));

         var isService = descriptor != null;
         return isService;
      }

      public static IEnumerable<StorageObjectDescriptor> GetStorageReferences(this MicroserviceDescriptor service)
      {
         //TODO: This won't work for nested relationships.

         var serviceInfo = service.ConvertToInfo();
         var storages = Microservices.StorageDescriptors.ToDictionary(kvp => kvp.AttributePath);
         var infos = Microservices.StorageDescriptors.Select(s => new Tuple<AssemblyDefinitionInfo, StorageObjectDescriptor>(s.ConvertToInfo(), s)).ToDictionary(kvp => kvp.Item1.Name);
         foreach (var reference in serviceInfo.References)
         {
            if (infos.TryGetValue(reference, out var storageInfo))
            {
               yield return storageInfo.Item2;
            }
         }
      }

      public static bool HasMongoLibraries(this MicroserviceDescriptor service) =>
         service.ConvertToAsset().HasMongoLibraries();

      public static bool HasMongoLibraries(this AssemblyDefinitionAsset asm)
      {
         var existingRefs = new HashSet<string>(asm.ConvertToInfo().DllReferences);
         foreach (var required in MongoLibraries)
         {
            if (!existingRefs.Contains(required)) return false;
         }
         return true;
      }

      public static void AddMongoLibraries(this MicroserviceDescriptor service) =>
         service.AddPrecompiledReferences(MongoLibraries);

      public static void RemoveMongoLibraries(this MicroserviceDescriptor service) =>
         service.RemovePrecompiledReferences(MongoLibraries);

      public static void AddMongoLibraries(this AssemblyDefinitionAsset asm) =>
         asm.AddPrecompiledReferences(MongoLibraries);

      public static void RemoveMongoLibraries(this AssemblyDefinitionAsset asm) =>
         asm.RemovePrecompiledReferences(MongoLibraries);

      public static bool IsContainedInAssemblyInfo(this IDescriptor service, AssemblyDefinitionInfo asm)
      {
         var assembly = service.Type.Assembly;
         var moduleName = assembly.Modules.FirstOrDefault().Name.Replace(".dll", "");

         return string.Equals(moduleName, asm.Name);
      }

      public static AssemblyDefinitionInfo ConvertToInfo(this AssemblyDefinitionAsset asm)
      {
         var jsonData = Json.Deserialize(asm.text) as ArrayDict;
         var path = AssetDatabase.GetAssetPath(asm);

         var assemblyDefInfo = new AssemblyDefinitionInfo {Location = path};

         if (jsonData.TryGetValue(NAME, out var nameObject) && nameObject is string name)
         {
            assemblyDefInfo.Name = name;
         }

         if (jsonData.TryGetValue(REFERENCES, out var referencesObject) &&
             referencesObject is IEnumerable<object> references)
         {
            assemblyDefInfo.References = references
               .Cast<string>()
               .Where(s => !string.IsNullOrEmpty(s))
               .ToArray();
         }

         if (jsonData.TryGetValue(PRECOMPILED, out var referencesDllObject) &&
             referencesDllObject is IEnumerable<object> dllReferences)
         {
            assemblyDefInfo.DllReferences = dllReferences
               .Cast<string>()
               .Where(s => !string.IsNullOrEmpty(s))
               .ToArray();
         }

         return assemblyDefInfo;
      }

      public static AssemblyDefinitionInfo ConvertToInfo(this IDescriptor service)
         => service.ConvertToAsset().ConvertToInfo();
      public static AssemblyDefinitionAsset ConvertToAsset(this IDescriptor service)
         => EnumerateAssemblyDefinitionAssets()
            .FirstOrDefault(asm => service.IsContainedInAssemblyInfo(asm.ConvertToInfo()));

      public static IEnumerable<AssemblyDefinitionAsset> EnumerateAssemblyDefinitionAssets()
      {
         // TODO: We could add a static cache here, because by definition, a recompile will be executed anytime a new ASMDEF shows up.
         var assemblyDefGuids = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)}");
         foreach (var assemblyDefGuid in assemblyDefGuids)
         {
            var assemblyDefPath = AssetDatabase.GUIDToAssetPath(assemblyDefGuid);
            var assemblyDef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyDefPath);
            yield return assemblyDef;
         }
      }

      public static IEnumerable<AssemblyDefinitionInfo> EnumerateAssemblyDefinitionInfos()
      {
         foreach (var asm in EnumerateAssemblyDefinitionAssets())
         {
            var assemblyDefInfo = asm.ConvertToInfo();
            if (!string.IsNullOrEmpty(assemblyDefInfo.Name))
            {
               yield return assemblyDefInfo;
            }
         }
      }

      public static void AddPrecompiledReferences(this MicroserviceDescriptor service, params string[] libraryNames)
         => service.ConvertToAsset().AddPrecompiledReferences(libraryNames);

      public static void AddAndRemoveReferences(this MicroserviceDescriptor service, List<string> toAddReferences, List<string> toRemoveReferences)
          => service.ConvertToAsset().AddAndRemoveReferences(toAddReferences, toRemoveReferences);

      public static void AddPrecompiledReferences(this AssemblyDefinitionAsset asm, params string[] libraryNames)
      {
         var jsonData = Json.Deserialize(asm.text) as ArrayDict;
         var dllReferences = GetReferences(PRECOMPILED, jsonData);

         foreach (var lib in libraryNames)
         {
            dllReferences.Add(lib);
         }

         jsonData[PRECOMPILED] = dllReferences.ToArray();
         WriteAssembly(asm, jsonData);
      }

      public static void CreateAssetDefinitionAssetOnDisk(string filePath, AssemblyDefinitionInfo info)
      {
	      var dict = new ArrayDict
	      {
		      [PRECOMPILED] = info.DllReferences,
		      [REFERENCES] = info.References,
		      [NAME] = info.Name,
		      [OVERRIDE_REFERENCES] = info.DllReferences.Length > 0
	      };
	      var json = Json.Serialize(dict, new StringBuilder());
	      json = Json.FormatJson(json);
	      File.WriteAllText(filePath,json);
	      AssetDatabase.ImportAsset(filePath);
      }

      public static void AddAndRemoveReferences(this AssemblyDefinitionAsset asm, List<string> toAddReferences, List<string> toRemoveReferences)
      {
          var jsonData = Json.Deserialize(asm.text) as ArrayDict;
          var dllReferences = GetReferences(REFERENCES, jsonData);

          if (toAddReferences != null)
          {
	          foreach (var toAdd in toAddReferences)
	          {
		          dllReferences.Add(toAdd);
	          }
          }

          if (toRemoveReferences != null)
          {
	          foreach (var toRemove in toRemoveReferences)
	          {
		          dllReferences.Remove(toRemove);
	          }
          }

          jsonData[REFERENCES] = dllReferences.ToArray();
          WriteAssembly(asm, jsonData);
      }

      public static void RemovePrecompiledReferences(this MicroserviceDescriptor service, params string[] libraryNames)
         => service.ConvertToAsset().RemovePrecompiledReferences(libraryNames);

      public static void RemovePrecompiledReferences(this AssemblyDefinitionAsset asm, params string[] libraryNames)
      {
         var jsonData = Json.Deserialize(asm.text) as ArrayDict;
         var dllReferences = GetReferences(PRECOMPILED, jsonData);

         foreach (var lib in libraryNames)
         {
            dllReferences.Remove(lib);
         }

         jsonData[PRECOMPILED] = dllReferences.ToArray();
         WriteAssembly(asm, jsonData);
      }

      private static HashSet<string> GetReferences(string referenceType, ArrayDict jsonData)
      {
          var dllReferences = new HashSet<string>();
          if (jsonData.TryGetValue(referenceType, out var referencesDllObject) &&
              referencesDllObject is IEnumerable<object> existingReferences)
          {
              dllReferences = new HashSet<string>(existingReferences
                  .Cast<string>()
                  .Where(s => !string.IsNullOrEmpty(s))
                  .ToArray());
          }

          return dllReferences;
      }

      private static void WriteAssembly(AssemblyDefinitionAsset asm, ArrayDict jsonData)
      {
         var json = Json.Serialize(jsonData, new StringBuilder());
         json = Json.FormatJson(json);
         var path = AssetDatabase.GetAssetPath(asm);
         File.WriteAllText(path,json);
         AssetDatabase.ImportAsset(path);
      }
   }
}
