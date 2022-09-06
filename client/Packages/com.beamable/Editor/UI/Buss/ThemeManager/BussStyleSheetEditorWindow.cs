using Beamable.Editor.UI.Components;
using System;
using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.StyleSheets;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class BussStyleSheetEditorWindow : EditorWindow
	{
		private BussStyleListVisualElement _styleList;
		private AddStyleButton _addStyleButton;
		private VisualElement _parent;
		private ScrollView _scroll;

		[SerializeField] private BussStyleSheet _styleSheet;

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
			//_styleList = new BussStyleListVisualElement();
			_parent = new VisualElement();
			_parent.style.SetLeft(0f);
			_parent.style.SetRight(0f);

			AddSelectorButton(
				_parent, _styleList);

			_parent.contentContainer.Add(_styleList);

			_scroll = new ScrollView();
			_scroll.style.SetFlexGrow(1f);

			this.GetRootVisualContainer().Add(_scroll);
#if UNITY_2019_1_OR_NEWER
			_scroll.contentContainer.Add(_parent);
#else
			_scroll.SetContents(_parent);
#endif

			if (_styleSheet != null)
			{
				SetStyleSheet(_styleSheet);
			}
		}

		private void OnDisable()
		{
			_styleList.Destroy();
			_styleList = null;

			_addStyleButton.Destroy();
			_addStyleButton = null;

			_scroll.RemoveFromHierarchy();
			_scroll = null;
		}

		public void SetStyleSheet(BussStyleSheet styleSheet)
		{
			_styleSheet = styleSheet;
			//_styleList.StyleSheets = new[] { styleSheet };
			_addStyleButton.CheckEnableState();
		}

		private void AddSelectorButton(VisualElement parent, BussStyleListVisualElement list)
		{
			_addStyleButton = new AddStyleButton();
			//_addStyleButton.Setup(list, _ => list.RefreshStyleCards());
			_addStyleButton.CheckEnableState();
			parent.Add(_addStyleButton);
		}
	}
}
