using Beamable.Editor.UI.Components;
using System;
using Beamable.UI.Buss;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussStyleSheetEditorWindow : EditorWindow
	{
		private BussStyleListVisualElement _styleList;
		private AddStyleButton _addStyleButton;

		public static void Open(BussStyleSheet styleSheet)
		{
			Type inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			var wnd = GetWindow<BussStyleSheetEditorWindow>(styleSheet.name, true, inspector);
			wnd.minSize = THEME_MANAGER_WINDOW_SIZE;
			wnd.SetStyleSheet(styleSheet);
			wnd.Show();
		}

		private void OnEnable()
		{
			_styleList = new BussStyleListVisualElement();
			
			AddSelectorButton(this.GetRootVisualContainer(), _styleList);
			
			this.GetRootVisualContainer().Add(_styleList);
		}

		private void OnDisable()
		{
			_styleList.Destroy();
			_styleList = null;
		}

		public void SetStyleSheet(BussStyleSheet styleSheet)
		{
			_styleList.StyleSheets = new[] {styleSheet};
			_addStyleButton.CheckEnableState();
		}

		private void AddSelectorButton(VisualElement parent, BussStyleListVisualElement list)
		{
			_addStyleButton = new AddStyleButton();
			_addStyleButton.Setup(list, _ => list.RefreshStyleCards());
			_addStyleButton.CheckEnableState();
			parent.Add(_addStyleButton);
		}
	}
}
