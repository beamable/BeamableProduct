using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class AddStyleButton : BeamableBasicVisualElement
	{
		private VisualElement _addStyleButton;
		private BussThemeManager _manager;
		private BussElementHierarchyVisualElement _navigationWindow;
		private Action<BussStyleRule> _onSelectorAdded;
		public AddStyleButton() : base($"{BUSS_THEME_MANAGER_PATH}/AddStyleButton/AddStyleButton.uss") { }

		public void Setup(BussThemeManager manager, BussElementHierarchyVisualElement navigationWindow, Action<BussStyleRule> onSelectorAdded)
		{
			_manager = manager;
			_navigationWindow = navigationWindow;
			_onSelectorAdded = onSelectorAdded;

			Init();
		}

		public override void Init()
		{
			base.Init();

			_addStyleButton = new VisualElement { name = "addStyleButton" };
			_addStyleButton.AddToClassList("button");
			_addStyleButton.Add(new Label("Add Style"));

			_addStyleButton.UnregisterCallback<MouseDownEvent>(_ => OnClick());
			_addStyleButton.RegisterCallback<MouseDownEvent>(_ => OnClick());

			_addStyleButton.UnregisterCallback<MouseEnterEvent>(_ => CheckEnableState());
			_addStyleButton.RegisterCallback<MouseEnterEvent>(_ => CheckEnableState());

			Root.Add(_addStyleButton);
		}

		public void CheckEnableState()
		{
			if (_addStyleButton == null) return;

			_addStyleButton.tooltip = string.Empty;
			_manager.ActiveStyleSheets.Clear();

#if BEAMABLE_DEVELOPER
			_manager.ActiveStyleSheets.AddRange(_navigationWindow.StyleSheets);
#else
			_manager.ActiveStyleSheets.AddRange(_navigationWindow.StyleSheets.Where(bussStyleSheet => !bussStyleSheet.IsReadOnly));
#endif

			if (_manager.ActiveStyleSheets.Count == 0)
			{
				_addStyleButton.tooltip = NO_BUSS_STYLE_SHEET_AVAILABLE;
				_addStyleButton.SetInactive(true);
			}
			else
			{
				_addStyleButton.tooltip = String.Empty;
				_addStyleButton.SetInactive(false);

				if (_manager.ActiveStyleSheets.Count == 1) { }
				else if (_manager.ActiveStyleSheets.Count > 1) { }
			}
		}

		private void OnClick()
		{
			List<BussStyleSheet> bussStyleSheets = _manager.ActiveStyleSheets.ToList();

			if (bussStyleSheets.Count == 0)
			{
				return;
			}

			if (bussStyleSheets.Count == 1)
			{
				CreateEmptyStyle(bussStyleSheets[0]);
			}
			else if (bussStyleSheets.Count > 1)
			{
				OpenMenu(bussStyleSheets);
			}
		}

		private void OpenMenu(List<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();

			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					CreateEmptyStyle(styleSheet);
				});
			}

			context.ShowAsContext();
		}

		private void CreateEmptyStyle(BussStyleSheet selectedStyleSheet)
		{
			BussStyleRule selector = BussStyleRule.Create(String.Empty, new List<BussPropertyProvider>());
			selectedStyleSheet.Styles.Add(selector);
			selectedStyleSheet.TriggerChange();
			_onSelectorAdded?.Invoke(selector);
			AssetDatabase.SaveAssets();
		}
	}
}
