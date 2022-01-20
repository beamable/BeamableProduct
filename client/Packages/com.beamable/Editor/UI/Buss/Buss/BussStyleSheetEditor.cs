using Beamable.UI.Buss;
using Beamable.UI.BUSS;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
	[CustomEditor(typeof(BussStyleSheet))]	
	public class BussStyleSheetEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 3f);
			if (GUI.Button(rect, "Open Theme Manager"))
			{
				BussThemeManager.Init();
			}
#if BEAMABLE_DEVELOPER
			base.OnInspectorGUI();
#endif
		}
	}
}
