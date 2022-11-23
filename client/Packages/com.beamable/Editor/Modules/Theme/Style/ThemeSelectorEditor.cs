using Beamable.Theme;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Style
{
	[CustomEditor(typeof(ThemeConfiguration), true)]
	public class ThemeSelectorEditor : UnityEditor.Editor
	{
		private ThemeConfiguration _configuration;

		public void OnEnable()
		{
			_configuration = target as ThemeConfiguration;
		}

		public override void OnInspectorGUI()
		{
			if (_configuration == null) return;

			string[] paths = BeamableAssetDatabase.FindAssetPaths<ThemeObject>().ToArray();
			string[] names = new string[paths.Length + 1];
			names[paths.Length] = "New...";
			ThemeObject[] styles = new ThemeObject[paths.Length];
			int selectedIndex = 0;
			for (int i = 0; i < paths.Length; i++)
			{
				string path = paths[i];
				string name = Path.GetFileNameWithoutExtension(path);
				var style = AssetDatabase.LoadAssetAtPath<ThemeObject>(path);
				names[i] = name;
				styles[i] = style;

				if (_configuration.Style == style)
					selectedIndex = i;
			}

			var originalNormal = EditorStyles.label.normal.textColor;
			var originalActive = EditorStyles.label.active.textColor;
			var originalFocus = EditorStyles.label.focused.textColor;

			EditorStyles.label.normal.textColor = Color.white;
			EditorStyles.label.active.textColor = Color.white;
			EditorStyles.label.focused.textColor = Color.white;
			var outputIndex = EditorGUILayout.Popup("Theme", selectedIndex, names);

			EditorStyles.label.normal.textColor = originalNormal;
			EditorStyles.label.active.textColor = originalActive;
			EditorStyles.label.focused.textColor = originalFocus;
			if (outputIndex != selectedIndex)
			{
				if (outputIndex == paths.Length)
				{
					string path = EditorUtility.SaveFilePanelInProject("Create Theme", "Custom", "asset",
					   "Please enter a file name for your new theme");
					var themeName = Path.GetFileNameWithoutExtension(path);
					var newTheme = ScriptableObject.CreateInstance<ThemeObject>();
					newTheme.name = themeName;
					newTheme.Parent = ThemeConfiguration.Instance.Style;
					AssetDatabase.CreateAsset(newTheme, path);
					ThemeConfiguration.Instance.Style = newTheme;
					AssetDatabase.ForceReserializeAssets(new[] { path });



				}
				else
				{
					_configuration.Style = styles[outputIndex];
					EditorUtility.SetDirty(_configuration);
				}

				Selection.SetActiveObjectWithContext(_configuration.Style, _configuration.Style);

			}
		}
	}
}
