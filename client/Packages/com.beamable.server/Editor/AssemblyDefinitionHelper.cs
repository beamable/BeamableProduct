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

      [MenuItem(ADD_MONGO, true, BEAMABLE_PRIORITY )]
      [MenuItem(REMOVE_MONGO, true, BEAMABLE_PRIORITY)]
      public static bool ValidateSelectionIsMicroservice() {
         if (!(Selection.activeObject is AssemblyDefinitionAsset asm))
         {
            return false;
         }

         var info = asm.ConvertToInfo();
         var descriptor = Microservices.Descriptors.FirstOrDefault(d => d.IsContainedInAssemblyInfo(info));

         return descriptor != null;
      }

      public static void AddMongoLibraries(this MicroserviceDescriptor service) =>
         service.AddPrecompiledReferences(MongoLibraries);

      public static void RemoveMongoLibraries(this MicroserviceDescriptor service) =>
         service.RemovePrecompiledReferences(MongoLibraries);

      public static void AddMongoLibraries(this AssemblyDefinitionAsset asm) =>
         asm.AddPrecompiledReferences(MongoLibraries);

      public static void RemoveMongoLibraries(this AssemblyDefinitionAsset asm) =>
         asm.RemovePrecompiledReferences(MongoLibraries);

      public static bool IsContainedInAssemblyInfo(this MicroserviceDescriptor service, AssemblyDefinitionInfo asm)
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

         if (jsonData.TryGetValue("name", out var nameObject) && nameObject is string name)
         {
            assemblyDefInfo.Name = name;
         }

         if (jsonData.TryGetValue("references", out var referencesObject) &&
             referencesObject is IEnumerable<object> references)
         {
            assemblyDefInfo.References = references
               .Cast<string>()
               .Where(s => !string.IsNullOrEmpty(s))
               .ToArray();
         }

         if (jsonData.TryGetValue("precompiledReferences", out var referencesDllObject) &&
             referencesDllObject is IEnumerable<object> dllReferences)
         {
            assemblyDefInfo.DllReferences = dllReferences
               .Cast<string>()
               .Where(s => !string.IsNullOrEmpty(s))
               .ToArray();
         }

         return assemblyDefInfo;
      }

      public static AssemblyDefinitionAsset ConvertToAsset(this MicroserviceDescriptor service)
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