using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beamable.Serialization.SmallerJSON;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor
{
   public static class AssemblyDefinitionHelper
   {
      const string PRECOMPILED = "precompiledReferences";
      const string REFERENCES = "references";
      const string NAME = "name";
      private const string ASSETS_BEAMABLE = "Assets/Beamable/";
      private const string ADD_MONGO = ASSETS_BEAMABLE + "Add Mongo Libraries";
      private const string REMOVE_MONGO = ASSETS_BEAMABLE + "Remove Mongo Libraries";
      private const int BEAMABLE_PRIORITY = 190;

      private static readonly string[] MongoLibraries = new[]
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

      public static void AddPrecompiledReferences(this AssemblyDefinitionAsset asm, params string[] libraryNames)
      {
         var jsonData = Json.Deserialize(asm.text) as ArrayDict;
         var dllReferences = GetPrecompiledAssemblies(jsonData);

         foreach (var lib in libraryNames)
         {
            dllReferences.Add(lib);
         }

         jsonData[PRECOMPILED] = dllReferences.ToArray();
         WriteAssembly(asm, jsonData);

      }

      public static void RemovePrecompiledReferences(this MicroserviceDescriptor service, params string[] libraryNames)
         => service.ConvertToAsset().RemovePrecompiledReferences(libraryNames);

      public static void RemovePrecompiledReferences(this AssemblyDefinitionAsset asm, params string[] libraryNames)
      {
         var jsonData = Json.Deserialize(asm.text) as ArrayDict;
         var dllReferences = GetPrecompiledAssemblies(jsonData);

         foreach (var lib in libraryNames)
         {
            dllReferences.Remove(lib);
         }

         jsonData[PRECOMPILED] = dllReferences.ToArray();
         WriteAssembly(asm, jsonData);
      }

      private static HashSet<string> GetPrecompiledAssemblies(ArrayDict jsonData)
      {
         var dllReferences = new HashSet<string>();
         if (jsonData.TryGetValue(PRECOMPILED, out var referencesDllObject) &&
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