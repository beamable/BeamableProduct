using Beamable.Common;
using Beamable.Editor;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor
{

	/// <summary>
	/// Helper class that handles the assembly definitions we generate when creating C#MS and StorageObjects.
	/// Also, contains a bunch of helper functions to manage data inside local StorageObject and some other C#MS stuff.
	/// TODO: Refactor the non-assembly-definition helper functions into more appropriate files.
	/// </summary>
	public static class AssemblyDefinitionHelper
	{
		const string PRECOMPILED = "precompiledReferences";
		const string REFERENCES = "references";
		const string OVERRIDE_REFERENCES = "overrideReferences";
		const string AUTO_REFERENCED = "autoReferenced";
		const string INCLUDE_PLATFORMS = "includePlatforms";
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

		
		public static AssemblyDefinitionInfo ConvertToInfo(this AssemblyDefinitionAsset asm)
		{
			var jsonData = Json.Deserialize(asm.text) as ArrayDict;
			var path = AssetDatabase.GetAssetPath(asm);

			var assemblyDefInfo = new AssemblyDefinitionInfo { Location = path };

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

		private static string[] _assemblyDefGuidsCache;
		public static IEnumerable<AssemblyDefinitionAsset> EnumerateAssemblyDefinitionAssets()
		{
			if (_assemblyDefGuidsCache == null)
				_assemblyDefGuidsCache = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)}");

			for (int i = 0; i < _assemblyDefGuidsCache.Length; i++)
			{
				var assemblyDefPath = AssetDatabase.GUIDToAssetPath(_assemblyDefGuidsCache[i]);
				var assemblyDef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyDefPath);
				if (assemblyDef == null) continue;

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

		public static ArrayDict AddPrecompiledReferences(ArrayDict asmJsonData, string asmPath, params string[] libraryNames)
		{
			var dllReferences = GetReferences(PRECOMPILED, asmJsonData);

			foreach (var lib in libraryNames)
			{
				dllReferences.Add(lib);
			}

			asmJsonData[PRECOMPILED] = dllReferences.ToArray();

			if (dllReferences.Count > 0)
			{
				asmJsonData[OVERRIDE_REFERENCES] = true;
			}
			else
			{
				asmJsonData.Remove(OVERRIDE_REFERENCES);
			}

			WriteAssembly(asmPath, asmJsonData);
			return asmJsonData;
		}

		public static ArrayDict AddPrecompiledReferences(this AssemblyDefinitionAsset asm, params string[] libraryNames)
		{
			var jsonData = Json.Deserialize(asm.text) as ArrayDict;
			var path = AssetDatabase.GetAssetPath(asm);
			return AddPrecompiledReferences(jsonData, path, libraryNames);
		}

		public static void CreateAssetDefinitionAssetOnDisk(string filePath, AssemblyDefinitionInfo info)
		{
			var dict = new ArrayDict
			{
				[REFERENCES] = info.References,
				[NAME] = info.Name,
				[AUTO_REFERENCED] = info.AutoReferenced
			};
			if (info.DllReferences.Length > 0) // don't include the field if there are no values.
			{
				dict[PRECOMPILED] = info.DllReferences;
				dict[OVERRIDE_REFERENCES] = true;
			}

			if (info.IncludePlatforms.Length > 0) // don't include the field at all if there are no values
			{
				dict[INCLUDE_PLATFORMS] = info.IncludePlatforms;
			}

			var json = Json.Serialize(dict, new StringBuilder());
			json = Json.FormatJson(json);
			File.WriteAllText(filePath, json);
			AssetDatabase.ImportAsset(filePath);
		}

		public static ArrayDict AddAndRemoveReferences(ArrayDict asmArrayDict,
												  string asmPath,
												  List<string> toAddReferences,
												  List<string> toRemoveReferences)
		{
			var dllReferences = GetReferences(REFERENCES, asmArrayDict);

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

			asmArrayDict[REFERENCES] = dllReferences.ToArray();
			WriteAssembly(asmPath, asmArrayDict);
			return asmArrayDict;
		}

		public static void RemovePrecompiledReferences(this AssemblyDefinitionAsset asm, params string[] libraryNames)
		{
			var jsonData = Json.Deserialize(asm.text) as ArrayDict;
			var dllReferences = GetReferences(PRECOMPILED, jsonData);

			foreach (var lib in libraryNames)
			{
				dllReferences.Remove(lib);
			}

			jsonData[PRECOMPILED] = dllReferences.ToArray();
			if (dllReferences.Count > 0)
			{
				jsonData[OVERRIDE_REFERENCES] = true;
			}
			else
			{
				jsonData.Remove(OVERRIDE_REFERENCES);
			}
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

		private static void WriteAssembly(string asmPath, ArrayDict jsonData)
		{
			var json = Json.Serialize(jsonData, new StringBuilder());
			json = Json.FormatJson(json);
			File.WriteAllText(asmPath, json);
			AssetDatabase.ImportAsset(asmPath);
		}

		private static void WriteAssembly(AssemblyDefinitionAsset asm, ArrayDict jsonData)
		{
			var path = AssetDatabase.GetAssetPath(asm);
			WriteAssembly(path, jsonData);
		}

	}
}
