using Beamable.Common;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class VariableConnectionVisualElement : BeamableBasicVisualElement
	{
		private Button _button;
		private IBussProperty _cachedProperty;
		private DropdownVisualElement _dropdown;

		private readonly List<string> _dropdownOptions = new List<string>();
		private VisualElement _mainElement;

		private readonly StylePropertyModel _model;

		public VariableConnectionVisualElement(StylePropertyModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(StylePropertyVisualElement)}/" +
			$"{nameof(VariableConnectionVisualElement)}/{nameof(VariableConnectionVisualElement)}.uss")
		{
			_model = model;
			_model.Change += Refresh;
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
		}

		protected override void OnDestroy()
		{
			_button.clickable.clicked -= _model.OnButtonClick;
			_model.Change -= Refresh;
		}

		public override void Refresh()
		{
			_button.EnableInClassList("whenConnected", _model.HasVariableConnected);

			var baseType = BussStyle.GetBaseType(_model.PropertyProvider.Key);
			_dropdownOptions.Clear();
			_dropdownOptions.Add(Constants.Features.Buss.MenuItems.NONE);
			_dropdownOptions.AddRange(_model.VariablesDatabase.GetVariableNames()
			                                .Where(key => _model.VariablesDatabase.GetVariableData(key)
			                                                    .HasTypeDeclared(baseType)));

			_dropdown.visible = _model.HasVariableConnected;
			_dropdown.Setup(_dropdownOptions, OnVariableSelected, false);

			// if (_model.PropertyProvider.GetProperty() is VariableProperty property)
			// {
			// 	var index = _dropdownOptions.IndexOf(property.VariableName);
			// 	if (index < 0)
			// 	{
			// 		index = 0;
			// 	}
			//
			// 	_dropdown.Set(index);
			// }
			// else
			// {
			// 	_dropdown.Set(0);
			// }
		}

		private void OnVariableSelected(int index)
		{
			if (_model.PropertyProvider.GetProperty() is VariableProperty variableProperty)
			{
				var option = _dropdownOptions[index];

				variableProperty.VariableName = option == Constants.Features.Buss.MenuItems.NONE ? "" : option;

				// _variableDatabase.SetPropertyDirty(_styleSheet, _styleRule, _propertyProvider);

				if (_model.StyleSheet != null)
				{
					_model.StyleSheet.TriggerChange();
				}

				// ConnectionChange?.Invoke();
			}
		}
	}
}
