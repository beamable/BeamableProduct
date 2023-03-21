using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using System;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class AddStyleButton : ThemeManagerBasicComponent
	{
		private VisualElement _addStyleButton;
		private Action _onButtonClicked;

		public AddStyleButton() : base(nameof(AddStyleButton)) { }

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

			Root.Add(_addStyleButton);
		}

		private void OnClick(MouseDownEvent _)
		{
			_onButtonClicked?.Invoke();
		}
	}
}
