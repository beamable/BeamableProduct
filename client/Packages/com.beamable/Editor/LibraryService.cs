using Beamable.Common.Dependencies;
using Beamable.Editor.Environment;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Editor
{
	[Serializable]
	public class LightbeamSampleInfo
	{
		#region JSON PROPERTIES
		public string name;
		public string subText;
		public string description;
		public string texturePath;
		public string relativeMainAssetPath;
		public string samplePath;
		public string docsUrl;
		#endregion

		#region COMPUTED PROPERTIES
		public bool isLocal { get; set; }
		
		/// <summary>
		/// The path to where the root asset would be if it was in the project
		/// </summary>
		public string localCandidateAssetPath { get; set; }

		/// <summary>
		/// When a sample is copied into a project, it lives at a specific path,
		/// which is "Assets/Samples/Beamable/VERSION/SAMPLE_NAME
		/// </summary>
		public string localCandidateFolder { get; set; }

		public string localCandidateFolderMetafile { get; set; }
		
		#endregion
	}
	
	public class LibraryService : IStorageHandler<LibraryService>, Beamable.Common.Dependencies.IServiceStorable
	{
		private StorageHandle<LibraryService> _handle;

		public List<LightbeamSampleInfo> lightbeams = new List<LightbeamSampleInfo>();
		
		public void ReceiveStorageHandle(StorageHandle<LibraryService> handle)
		{
			_handle = handle;
		}

		public void OnBeforeSaveState()
		{
		}

		public void OnAfterLoadState()
		{
		}

		public void OpenDocumentation(LightbeamSampleInfo lb)
		{
			
		}
		
		public void OpenSample(LightbeamSampleInfo lb)
		{
			var needsRefresh = false;
			try
			{
				AssetDatabase.DisallowAutoRefresh();
				if (!lb.isLocal)
				{
					needsRefresh = true;
					CopySampleIntoProject(lb);
				}
				
				// if the scene has changes, we don't want to just SWITCH.
				if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				{
					EditorSceneManager.OpenScene(lb.localCandidateAssetPath, OpenSceneMode.Single);
				}
			}
			finally
			{
				AssetDatabase.AllowAutoRefresh();
				if (needsRefresh)
				{
					AssetDatabase.Refresh();
				}
			}
			
		}

		public void CopySampleIntoProject(LightbeamSampleInfo lb)
		{

			// clean up old code.
			RemoveSampleFromProject(lb);

			// the actual sample path may need modification in a DEV environment.
			var samplePath = lb.samplePath;
#if BEAMABLE_DEVELOPER
			samplePath = samplePath.Replace("SAMPLES_PATH", "Samples");
#endif

			// the path needs to be relative to the location of the package
			samplePath = Path.Combine("Packages/com.beamable", samplePath);

			FileUtils.CopyDirectory(samplePath, lb.localCandidateFolder, true);

			{
				// during development, adding the sample to /Assets would cause a compiler duplication, 
				//  because the code exists in our dev package, and in Assets. 
				// To correct this, ONLY IN DEV, there is a hack to change the name of all assemblies 
				//  in the destination copy. 
#if BEAMABLE_DEVELOPER
				var assemblyPaths =
					Directory.GetFiles(lb.localCandidateFolder, "*.asmdef", SearchOption.AllDirectories);
				foreach (var assemblyPath in assemblyPaths)
				{
					var json = File.ReadAllText(assemblyPath);
					var dict = (ArrayDict)Json.Deserialize(json);
					dict["name"] = dict["name"] + ".Dev";
					json = Json.Serialize(dict, new StringBuilder());
					File.WriteAllText(assemblyPath, json);
				}

				var metaPaths = Directory.GetFiles(lb.localCandidateFolder, "*.meta", SearchOption.AllDirectories);
				foreach (var metaPath in metaPaths)
				{
					var yaml = File.ReadAllText(metaPath);
					var lines = yaml.Split(System.Environment.NewLine);
					for (var i = 0; i < lines.Length; i++)
					{
						if (!lines[i].StartsWith("guid:"))
							continue;
						lines[i] = $"guid: {Guid.NewGuid():N}";
					}

					yaml = string.Join(System.Environment.NewLine, lines);
					File.WriteAllText(metaPath, yaml);
				}
#endif
			}

		}

		public void RemoveSampleFromProject(LightbeamSampleInfo lb)
		{
			// delete the entire folder
			// Directory.Delete(lb.LocalCandidateFolder, true);
			FileUtil.DeleteFileOrDirectory(lb.localCandidateFolder);
			
			// and delete the .meta folder that goes with it
			File.Delete(lb.localCandidateFolderMetafile);
		}
		
		public void Reload()
		{
			ResetSamples();
		}

		void ResetSamples()
		{
			lightbeams.Clear();
			
			var packageFile = $"Packages/{BeamablePackages.BeamablePackageName}/package.json";
			if (!File.Exists(packageFile)) 
				return;

			var json = File.ReadAllText(packageFile);
			var dict = (ArrayDict)Json.Deserialize(json);
			if (!dict.TryGetValue("samples", out var samples))
				return;

			var sampleList = (List<object>)samples;
			foreach (var sampleObj in sampleList)
			{
				var sampleDict = (ArrayDict)sampleObj;

				var lightbeam = new LightbeamSampleInfo
				{
					name = sampleDict["displayName"].ToString(),
					description = sampleDict["description"].ToString(),
					samplePath = sampleDict["path"].ToString(),
				};
				if (sampleDict.TryGetValue("subText", out var subText))
				{
					lightbeam.subText = subText.ToString();
				}
				if (sampleDict.TryGetValue("texturePath", out var texturePath))
				{
					lightbeam.texturePath = texturePath.ToString();
				}
				if (sampleDict.TryGetValue("docsUrl", out var docsUrl))
				{
					lightbeam.docsUrl = docsUrl.ToString();
				}
				if (sampleDict.TryGetValue("relativeMainAssetPath", out var relativeMainAssetPath))
				{
					lightbeam.relativeMainAssetPath = relativeMainAssetPath.ToString();
				}

				lightbeam.localCandidateFolder =
					Path.Combine("Assets", "Samples", "Beamable", BeamableEnvironment.SdkVersion.ToString(), lightbeam.name);
				lightbeam.localCandidateAssetPath = lightbeam.relativeMainAssetPath == null
					? null
					: Path.Combine(lightbeam.localCandidateFolder, lightbeam.relativeMainAssetPath);
				lightbeam.isLocal = File.Exists(lightbeam.localCandidateAssetPath);
				lightbeam.localCandidateFolderMetafile = lightbeam.localCandidateFolder + ".meta";
				lightbeams.Add(lightbeam);
			}
		}

		public void ShowInProject(LightbeamSampleInfo lightBeam)
		{
			throw new NotImplementedException();
		}

		public void RemoveSample(LightbeamSampleInfo lightBeam)
		{
			throw new NotImplementedException();
		}
	}
}