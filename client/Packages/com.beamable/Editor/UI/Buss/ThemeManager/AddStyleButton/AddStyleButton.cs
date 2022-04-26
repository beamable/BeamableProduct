using Beamable.Common;
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
		private BussStyleListVisualElement _styleList;
		private Action<BussStyleRule> _onSelectorAdded;
		public AddStyleButton() : base($"{BUSS_THEME_MANAGER_PATH}/AddStyleButton/AddStyleButton.uss") { }

		public void Setup(BussStyleListVisualElement styleList, Action<BussStyleRule> onSelectorAdded)
		{
			_styleList = styleList;
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

			var styleSheetCount = _styleList.WritableStyleSheets?.Count() ?? 0;

			if (styleSheetCount == 0)
			{
				_addStyleButton.tooltip = NO_BUSS_STYLE_SHEET_AVAILABLE;
				_addStyleButton.SetInactive(true);
			}
			else
			{
				_addStyleButton.tooltip = String.Empty;
				_addStyleButton.SetInactive(false);

				if (styleSheetCount == 1) { }
				else if (styleSheetCount > 1) { }
			}
		}

		private void OnClick()
		{
			var styleSheetCount = _styleList.WritableStyleSheets.Count();

			if (styleSheetCount == 0)
			{
				return;
			}

			if (styleSheetCount == 1)
			{
				CreateEmptyStyle(_styleList.WritableStyleSheets.First(), Constants.Features.Buss.NEW_SELECTOR_NAME);
			}
			else if (styleSheetCount > 1)
			{
				OpenMenu(_styleList.WritableStyleSheets);
			}
		}

		private void OpenMenu(IEnumerable<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();

			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					CreateEmptyStyle(styleSheet, Constants.Features.Buss.NEW_SELECTOR_NAME);
				});
			}

			context.ShowAsContext();
		}

		private void CreateEmptyStyle(BussStyleSheet selectedStyleSheet, string newName = "")
		{
			BussStyleRule selector = BussStyleRule.Create(newName, new List<BussPropertyProvider>());
			selectedStyleSheet.Styles.Add(selector);
			selectedStyleSheet.TriggerChange();
			_onSelectorAdded?.Invoke(selector);
			AssetDatabase.SaveAssets();
		}
	}
}
