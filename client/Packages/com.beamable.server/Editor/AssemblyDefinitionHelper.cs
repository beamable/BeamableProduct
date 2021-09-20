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
      //
      [MenuItem("Assets/Beamable", false, BEAMABLE_PRIORITY)]
      public static void BeamableSubMenu()
      {

      }
      [MenuItem(ADD_MONGO, false, BEAMABLE_PRIORITY)]
      public static void AddMongoLibraries() {
         if (Selection.activeObject is AssemblyDefinitionAsset asm)
         {
            asm.AddPrecompiledReferences(MongoLibraries);
         }
      }

      [MenuItem(REMOVE_MONGO, false, BEAMABLE_PRIORITY)]
      public static void RemoveMongoLibraries() {
         if (Selection.activeObject is AssemblyDefinitionAsset asm)
         {
            asm.RemovePrecompiledReferences(MongoLibraries);
         }
      }

      [MenuItem(ADD_MONGO, true, BEAMABLE_PRIORITY )]
      [MenuItem(REMOVE_MONGO, true, BEAMABLE_PRIORITY)]
      public static bool DoSomethingValidation() {
         var isAsm = Selection.activeObject.GetType() == typeof(AssemblyDefinitionAsset);
         if (!isAsm) return false;

         Microservices.Descriptors.

         return true;
      }

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
      }
   }
}