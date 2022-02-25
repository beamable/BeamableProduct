using System;
using Beamable.UI.Buss;
using UnityEditor;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussStyleSheetEditorWindow : EditorWindow
	{
		private BussStyleListVisualElement _styleList;
		
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
			rootVisualElement.Add(_styleList);
		}

		public void SetStyleSheet(BussStyleSheet styleSheet)
		{
			_styleList.StyleSheets = new[] {styleSheet};
		}
	}
}
