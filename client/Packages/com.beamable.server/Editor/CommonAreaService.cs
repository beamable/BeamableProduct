using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public class CommonAreaService
	{
		public static void EnsureCommon()
		{
			var cas = new CommonAreaService();
			cas.CreateCommonArea();
		}

		public static void EnsureCommonAssetLock()
		{
			try
			{
				AssetDatabase.StartAssetEditing();
				EnsureCommon();
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}
		}

		public CommonAreaService()
		{
			// TODO: once we have EditorBeamContext, we should actually inject the configuration into the class via constructor.
		}

		public string GetCommonPath()
		{
			return Path.Combine("Assets/Beamable/Common");
		}

		public static string GetCommonAsmDefName() => "Unity.Beamable.Customer.Common";

		public string GetCommonAsmDefPath()
		{
			return Path.Combine(GetCommonPath(), $"{GetCommonAsmDefName()}.asmdef");
		}

		public string GetCommonSourceStartPath()
		{
			return Path.Combine(GetCommonPath(), $"Sample.cs");
		}

		/// <summary>
		/// The Common Area is an assembly definition that can be referenced from Unity, and from a C#MS.
		/// This method will generate the folder, asmdef, and automatically have all existing C#MS's reference the common asmdef.
		/// <para> However, if <see cref="MicroserviceConfiguration.AutoBuildCommonAssembly"/> is false, this method will not do anything. </para>
		/// </summary>
		public void CreateCommonArea()
		{
			if (!MicroserviceConfiguration.Instance.AutoBuildCommonAssembly) return;

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
						},
						AutoReferenced = true
					});

					// make sure there is at least 1 script in that folder. Otherwise; we'll get a warning from Unity saying it won't compile it.
					var originalText = File.ReadAllText(Path.Combine(MicroserviceEditor.TEMPLATE_DIRECTORY, "SharedSample.cs"));
					File.WriteAllText(GetCommonSourceStartPath(), originalText);
				}


				var toAdd = new List<string> { GetCommonAsmDefName() };
				var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
				serviceRegistry.Descriptors.ForEach(d => d.AddAndRemoveReferences(toAdd, null));

			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}
		}
	}
}
