using Beamable.Editor.UI.Components;
using System;
using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss
{
	[CustomEditor(typeof(BussStyleSheet))]
	public class BussStyleSheetEditor : UnityEditor.Editor
	{
#if UNITY_2019_1_OR_NEWER
		private BussStyleListVisualElement _list;
		
		public override VisualElement CreateInspectorGUI()
		{
			var styleSheet = (BussStyleSheet)target;
			var root = new VisualElement();
			_list = new BussStyleListVisualElement();
			_list.StyleSheets = new[] {styleSheet};
			
			if (!styleSheet.IsReadOnly)
			{
				AddSelectorButton(root, _list);
			}
			
			root.Add(_list);
			return root;
		}

		private void AddSelectorButton(VisualElement parent, BussStyleListVisualElement list)
		{
			var button = new AddStyleButton();
			button.Setup(list, _ => list.RefreshStyleCards());
			button.CheckEnableState();
			parent.Add(button);
		}

		private void OnDestroy()
		{
			if(_list != null){
				_list.Destroy();
				_list = null;
			}
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
