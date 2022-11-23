using Beamable.Theme;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Modules.Theme
{

	[CustomPropertyDrawer(typeof(ThemeObject))]
	public class ThemeConfigurationPropertyDrawer : PropertyDrawer // TODO: We can VisualElements to make new propertydrawers
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 40;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var rect = new Rect(position.x + 5, position.y, position.width - 5, position.height);
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

				if (ThemeConfiguration.Instance.Style == style)
					selectedIndex = i;
			}
			var outputIndex = EditorGUI.Popup(rect, "Theme", selectedIndex, names);
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
					ThemeConfiguration.Instance.Style = styles[outputIndex];
					EditorUtility.SetDirty(ThemeConfiguration.Instance);
				}

			}

		}
	}
}
