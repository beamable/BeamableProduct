using System.IO;
using UnityEngine;

using static Beamable.NewTestingTool.Constants.TestConstants.Paths;

namespace Beamable.NewTestingTool.Extensions
{
	public static class TestExtensions
	{
		public static T LoadScriptableObject<T>(string fileName, string pathFromResources, string savePath) where T : ScriptableObject
		{
			if (!Directory.Exists(PATH_TO_RESOURCES))
				Directory.CreateDirectory(PATH_TO_RESOURCES);
			
			var scriptable = Resources.Load<T>(string.IsNullOrWhiteSpace(pathFromResources) ? fileName : $"{pathFromResources}/{fileName}");
			if (scriptable == null)
			{
				if (!Directory.Exists(savePath))
					Directory.CreateDirectory(savePath);
				
				scriptable = ScriptableObject.CreateInstance<T>();
				#if UNITY_EDITOR
				UnityEditor.AssetDatabase.CreateAsset(scriptable, $"{savePath}/{fileName}.asset");
				#endif
			}
			return scriptable;
		}
	}
}
