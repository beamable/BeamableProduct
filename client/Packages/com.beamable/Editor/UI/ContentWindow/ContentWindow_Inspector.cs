using Beamable.Common.Inventory;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private void DrawContentInspector()
		{
			EditorGUILayout.BeginVertical();
			if (!nestedEditor)
			{
				ScriptableObject scriptableObject = ScriptableObject.CreateInstance(typeof(CurrencyContent));
				UnityEditor.Editor.CreateCachedEditor(scriptableObject, null, ref nestedEditor);
			}

			nestedEditor.OnInspectorGUI();
			EditorGUILayout.EndVertical();
		}
	}
}
