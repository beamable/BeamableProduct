using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public static class Test
	{
		[MenuItem("BEAM/CREATE COMMON")]
		public static void CreateCommonArea()
		{
			var cas = new CommonAreaService();
			cas.CreateCommonArea();
		}
	}

	public class CommonAreaService
	{


		public CommonAreaService()
		{

		}

		public string GetCommonPath()
		{
			return Path.Combine("Assets/Beamable/CommonCode");
		}

		public string GetCommonAsmDefName() => "Beamable.Shared.UserCode";

		public string GetCommonAsmDefPath()
		{
			return Path.Combine(GetCommonPath(), $"{GetCommonAsmDefName()}.asmdef");
		}

		public void CreateCommonArea()
		{
			if (!MicroserviceConfiguration.Instance.AutoBuildCommonAssembly) return;
			/*
			 * create the folder if it doesn't exist.
			 *
			 * create the assembly definition if it doesn't exist
			 */

			AssetDatabase.StartAssetEditing();
			try
			{
				Directory.CreateDirectory(GetCommonPath());

				var asmPath = GetCommonAsmDefPath();
				var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmPath);
				if (asset == null)
				{
					AssemblyDefinitionHelper.CreateAssetDefinitionAssetOnDisk(asmPath, new AssemblyDefinitionInfo
					{
						Name = GetCommonAsmDefName(),
						References = new[]
						{
							"Unity.Beamable.Runtime.Common", "Unity.Beamable.Server.Runtime.Shared",
							// TODO: Add mongo libraries???
						},
						DllReferences = AssemblyDefinitionHelper.MongoLibraries
					});
				}

				var toAdd = new List<string> {GetCommonAsmDefName()};
				Microservices.Descriptors.ForEach(d => d.AddAndRemoveReferences(toAdd, null));

			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}
		}
	}
}
