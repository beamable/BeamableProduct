using Beamable.UI.Layouts;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Style
{
	[CustomPropertyDrawer(typeof(MediaQueryObject), true)]
	public class MediaQueryObjectPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var paths = BeamableAssetDatabase.FindAssetPaths<MediaQueryObject>().ToArray();
			var names = new string[paths.Length + 1];
			names[0] = "<none>";

			var mediaQueryObjects = new MediaQueryObject[paths.Length];
			var selectedIndex = 0;
			for (int i = 0; i < paths.Length; i++)
			{
				string path = paths[i];
				string name = Path.GetFileNameWithoutExtension(path);
				var mediaQueryObject = AssetDatabase.LoadAssetAtPath<MediaQueryObject>(path);
				names[i + 1] = name;
				mediaQueryObjects[i] = mediaQueryObject;

				if (mediaQueryObject == property.objectReferenceValue)
				{
					selectedIndex = i + 1;
				}

			}

			var output = EditorGUI.Popup(position, fieldInfo.Name, selectedIndex, names.ToArray());
			if (output != selectedIndex)
			{
				property.objectReferenceValue = output == 0 ? null : mediaQueryObjects[output - 1];
			}
		}
	}
}
