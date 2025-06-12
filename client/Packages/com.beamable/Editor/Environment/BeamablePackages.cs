using Beamable.Common;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Beamable.Editor.Environment
{
	public static class BeamablePackages
	{
		public const string BeamablePackageName = "com.beamable";
		public const string ServerPackageName = "com.beamable.server";

		public static List<string> CliPathsToIgnore = new List<string>()
		{
			"Packages/" + BeamablePackageName,
			"Library/PackageCache/" + BeamablePackageName,
			
			// leave the com.beamable.server exclusion so that older versions don't
			//  accidentally include older C#MS instances
			"Library/PackageCache/" + ServerPackageName
		};
		
		public static List<string> GetManifestFileReferences()
		{
			var referencePaths = new List<string>();
			var filePath = "Packages/manifest.json";
			if (!File.Exists(filePath))
			{
				return referencePaths;
			}
			var json = File.ReadAllText(filePath);
			var manifest = (ArrayDict)Json.Deserialize(json);
			if (!manifest.TryGetValue("dependencies", out var deps) )
			{
				return referencePaths;
			}

			var depDict = deps as ArrayDict;
			if (depDict == null)
			{
				return referencePaths;
			}

			foreach (var kvp in depDict)
			{
				var value = kvp.Value?.ToString() ?? "";
				if (!value.StartsWith("file://")) continue;
				
				referencePaths.Add(value.Substring("file://".Length));
			}

			return referencePaths;
		}
		public static Promise<PackageInfo> GetPackageInfo(string packageName)
		{
			var listReq = Client.List(true);
			var promise = new Promise<PackageInfo>();

			void Check()
			{
				if (!listReq.IsCompleted)
				{
					return;
				}

				var isSuccess = listReq.Status == StatusCode.Success;
				if (!isSuccess)
				{
					promise.CompleteError(new Exception("Unable to list local packages. " + listReq.Error.message));
				}

				var package = listReq.Result.FirstOrDefault(p => p.name.Equals(packageName));
				promise.CompleteSuccess(package);
			}

			EditorApplication.update += Check;
			promise.Recover(_ => null).Then(_ =>
			{
				EditorApplication.update -= Check;
			});
			return promise;
		}
	}
}
