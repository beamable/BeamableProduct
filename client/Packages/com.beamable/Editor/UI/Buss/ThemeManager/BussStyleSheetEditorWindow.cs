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
			this.GetRootVisualContainer().Add(_styleList);
		}

		public void SetStyleSheet(BussStyleSheet styleSheet)
		{
			_styleList.StyleSheets = new[] {styleSheet};
		}
	}
}
