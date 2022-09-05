using System.IO;
using UnityEngine;

using static Beamable.BSAT.Constants.TestConstants.Paths;

namespace Beamable.BSAT.Extensions
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
		
		public static string WrapWithColor(this object message, Color color) =>
			$"<color=#{(byte)(color.r * 255f):X2}{(byte)(color.g * 255f):X2}{(byte)(color.b * 255f):X2}>{message}</color>";
	}
}
