using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class VariableConnectionVisualElement : BeamableBasicVisualElement
	{
		private Button _button;
		private IBussProperty _cachedProperty;
		private DropdownVisualElement _dropdown;
		private VisualElement _mainElement;

		private readonly StylePropertyModel _model;

		public VariableConnectionVisualElement(StylePropertyModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(StylePropertyVisualElement)}/" +
			$"{nameof(VariableConnectionVisualElement)}/{nameof(VariableConnectionVisualElement)}.uss")
		{
			_model = model;
		}

		public override void Init()
		{
			base.Init();

			_mainElement = new VisualElement {name = "variableConnectionElement"};
			_mainElement.style.SetFlexDirection(FlexDirection.Row);
			Root.Add(_mainElement);

			_button = new Button {name = "button"};
			_button.clickable.clicked += _model.OnButtonClick;
			_mainElement.Add(_button);

			_dropdown = new DropdownVisualElement {name = "dropdown"};
			_dropdown.Refresh();
			_dropdown.Q("valueContainer").style.SetWidth(_dropdown.Q("valueContainer").style.GetWidth() - 30f);
			_mainElement.Add(_dropdown);
			
			Refresh();
		}

		protected override void OnDestroy()
		{
			_button.clickable.clicked -= _model.OnButtonClick;
		}

		public override void Refresh()
		{
			_button.EnableInClassList("whenConnected", _model.HasVariableConnected);
			_dropdown.visible = _model.HasVariableConnected;
			_dropdown.Setup(_model.DropdownOptions, _model.OnVariableSelected, _model.VariableDropdownOptionIndex,
			                false);
		}
	}
}
