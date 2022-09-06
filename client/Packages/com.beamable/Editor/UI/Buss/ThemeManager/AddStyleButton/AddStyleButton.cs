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
			_addStyleButton.Add(new Label(ADD_STYLE_BUTTON_LABEL));

			_addStyleButton.UnregisterCallback<MouseDownEvent>(OnClick);
			_addStyleButton.RegisterCallback<MouseDownEvent>(OnClick);

			_addStyleButton.UnregisterCallback<MouseEnterEvent>(CheckEnableState);
			_addStyleButton.RegisterCallback<MouseEnterEvent>(CheckEnableState);

			Root.Add(_addStyleButton);
		}

		private void OnClick(MouseDownEvent evt = null)
		{
			// int styleSheetCount = _styleList.WritableStyleSheets.Count();
			//
			// if (styleSheetCount == 0)
			// {
			// 	return;
			// }
			//
			// if (styleSheetCount == 1)
			// {
			// 	CreateEmptyStyle(_styleList.WritableStyleSheets.First());
			// }
			// else if (styleSheetCount > 1)
			// {
			// 	OpenMenu(_styleList.WritableStyleSheets);
			// }
		}

		public void CheckEnableState(MouseEnterEvent evt = null)
		{
			if (_addStyleButton == null) return;

			_addStyleButton.tooltip = string.Empty;
			
			// int styleSheetCount = _styleList.WritableStyleSheets?.Count() ?? 0;
			int styleSheetCount = 0;


			if (styleSheetCount == 0)
			{
				_addStyleButton.tooltip = NO_BUSS_STYLE_SHEET_AVAILABLE;
				_addStyleButton.SetInactive(true);
			}
			else
			{
				_addStyleButton.tooltip = String.Empty;
				_addStyleButton.SetInactive(false);
			}
		}

		private void OpenMenu(IEnumerable<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();
			context.AddItem(new GUIContent(ADD_STYLE_OPTIONS_HEADER), false, () => { });
			context.AddSeparator(string.Empty);
			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					CreateEmptyStyle(styleSheet);
				});
			}

			context.ShowAsContext();
		}

		private void CreateEmptyStyle(BussStyleSheet selectedStyleSheet, string newName = "*")
		{
			BussStyleRule selector = BussStyleRule.Create(newName, new List<BussPropertyProvider>());
			selectedStyleSheet.Styles.Add(selector);
			selectedStyleSheet.TriggerChange();
			_onSelectorAdded?.Invoke(selector);
			AssetDatabase.SaveAssets();
		}
	}
}
