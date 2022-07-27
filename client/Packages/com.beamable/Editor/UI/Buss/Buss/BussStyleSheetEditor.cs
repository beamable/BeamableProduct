using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using UnityEditor;
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
		private BussStyleSheet _styleSheet;

		public override VisualElement CreateInspectorGUI()
		{
			_styleSheet = (BussStyleSheet)target;
			VisualElement root = new VisualElement();
			_list = new BussStyleListVisualElement {StyleSheets = new[] {_styleSheet}};

#if BEAMABLE_DEVELOPER
			LabeledCheckboxVisualElement readonlyCheckbox = new LabeledCheckboxVisualElement("Readonly");
			readonlyCheckbox.OnValueChanged -= OnReadonlyValueChanged;
			readonlyCheckbox.OnValueChanged += OnReadonlyValueChanged;
			readonlyCheckbox.Refresh();
			readonlyCheckbox.SetWithoutNotify(_styleSheet.IsReadOnly);
			root.Add(readonlyCheckbox);
#endif

			if (!_styleSheet.IsReadOnly)
			{
				AddSelectorButton(root, _list);
			}

			root.Add(_list);
			return root;
		}

#if BEAMABLE_DEVELOPER
		private void OnReadonlyValueChanged(bool value)
		{
			_styleSheet.SetReadonly(value);
		}
#endif

		private void AddSelectorButton(VisualElement parent, BussStyleListVisualElement list)
		{
			AddStyleButton button = new AddStyleButton();
			button.Setup(list, _ => list.RefreshStyleCards());
			button.CheckEnableState();
			parent.Add(button);
		}

		private void OnDestroy()
		{
			if (_list != null)
			{
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
