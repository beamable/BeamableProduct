using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Buss
{
	[CustomEditor(typeof(BussStyleSheet))]
	public class BussStyleSheetEditor : UnityEditor.Editor
	{

#if UNITY_2019_1_OR_NEWER
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			var list = new BussStyleListVisualElement();
			list.StyleSheets = new[] {(BussStyleSheet)target};
			root.Add(list);
			return root;
		}
#else
		public override void OnInspectorGUI()
		{
			var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 3f);
			if (GUI.Button(rect, "Open Editor"))
			{
				BussStyleSheetEditorWindow.Open((BussStyleSheet)target);
			}
#if BEAMABLE_DEVELOPER
			base.OnInspectorGUI();
#endif
		}
#endif
	}
}
