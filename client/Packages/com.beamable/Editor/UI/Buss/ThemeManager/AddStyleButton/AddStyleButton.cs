using Beamable.Editor.UI.Common;
using System;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class AddStyleButton : BeamableBasicVisualElement
	{
		private VisualElement _addStyleButton;
		private Action _onButtonClicked;

		public AddStyleButton() : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(AddStyleButton)}/{nameof(AddStyleButton)}.uss") { }

		public void Setup(Action onButtonClicked)
		{
			_onButtonClicked = onButtonClicked;
			Init();
		}

		public override void Init()
		{
			base.Init();

			_addStyleButton = new VisualElement {name = "addStyleButton"};
			_addStyleButton.AddToClassList("button");
			_addStyleButton.Add(new Label(ADD_STYLE_BUTTON_LABEL));

			_addStyleButton.UnregisterCallback<MouseDownEvent>(OnClick);
			_addStyleButton.RegisterCallback<MouseDownEvent>(OnClick);

			_addStyleButton.UnregisterCallback<MouseEnterEvent>(CheckEnableState);
			_addStyleButton.RegisterCallback<MouseEnterEvent>(CheckEnableState);

			Root.Add(_addStyleButton);
		}

		private void OnClick(MouseDownEvent _)
		{
			_onButtonClicked?.Invoke();
		}

		public void CheckEnableState(MouseEnterEvent evt = null)
		{
			if (_addStyleButton == null) return;

			_addStyleButton.tooltip = string.Empty;

			// int styleSheetCount = _styleList.WritableStyleSheets?.Count() ?? 0;
			//
			// if (styleSheetCount == 0)
			// {
			// 	_addStyleButton.tooltip = NO_BUSS_STYLE_SHEET_AVAILABLE;
			// 	_addStyleButton.SetInactive(true);
			// }
			// else
			// {
			// 	_addStyleButton.tooltip = String.Empty;
			// 	_addStyleButton.SetInactive(false);
			// }
		}
	}
}
