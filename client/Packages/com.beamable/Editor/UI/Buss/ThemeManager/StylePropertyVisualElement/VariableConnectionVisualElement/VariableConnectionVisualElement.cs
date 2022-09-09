using Beamable.Common;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class VariableConnectionVisualElement : BeamableVisualElement
	{
		// public event Action ConnectionChange;

		private Button _button;
		private IBussProperty _cachedProperty;
		private DropdownVisualElement _dropdown;

		private readonly List<string> _dropdownOptions = new List<string>();
		private VisualElement _mainElement;
		private BussPropertyProvider _propertyProvider;
		// private BussStyleRule _styleRule;
		private BussStyleSheet _styleSheet;

		private VariableDatabase _variableDatabase;

		public bool IsConnected => _propertyProvider.GetProperty() is VariableProperty;

		public VariableConnectionVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(StylePropertyVisualElement)}/" +
			$"{nameof(VariableConnectionVisualElement)}/{nameof(VariableConnectionVisualElement)}") { }

		public override void Refresh()
		{
			base.Refresh();

			_mainElement = Root.Q("variableConnectionElement");
			_button = _mainElement.Q<Button>("button");
			_dropdown = _mainElement.Q<DropdownVisualElement>("dropdown");
			_mainElement.style.SetFlexDirection(FlexDirection.Row);

			_dropdown.Refresh();
			_dropdown.Q("valueContainer").style.SetWidth(_dropdown.Q("valueContainer").style.GetWidth() - 30f);
		}

		public void Setup(BussStyleSheet styleSheet,
		                  BussStyleRule styleRule,
		                  BussPropertyProvider propertyProvider,
		                  VariableDatabase variableDatabase) // temporary parameter
		{
			_variableDatabase = variableDatabase;
			_styleSheet = styleSheet;
			// _styleRule = styleRule;
			_propertyProvider = propertyProvider;

			Update();
		}

		private void Update()
		{
			_button.clickable.clicked -= OnButtonClick;
			_button.clickable.clicked += OnButtonClick;
			_button.EnableInClassList("whenConnected", IsConnected);

			var baseType = BussStyle.GetBaseType(_propertyProvider.Key);
			_dropdownOptions.Clear();
			_dropdownOptions.Add(Constants.Features.Buss.MenuItems.NONE);
			_dropdownOptions.AddRange(_variableDatabase
			                          .GetVariableNames()
			                          .Where(key => _variableDatabase.GetVariableData(key).HasTypeDeclared(baseType)));

			_dropdown.visible = IsConnected;
			_dropdown.Setup(_dropdownOptions, OnVariableSelected, false);

			if (_propertyProvider.GetProperty() is VariableProperty property)
			{
				var index = _dropdownOptions.IndexOf(property.VariableName);
				if (index < 0)
				{
					index = 0;
				}

				_dropdown.Set(index);
			}
			else
			{
				_dropdown.Set(0);
			}
		}

		private void OnButtonClick()
		{
			if (_cachedProperty == null)
			{
				_cachedProperty = IsConnected
					? BussStyle.GetDefaultValue(_propertyProvider.Key).CopyProperty()
					: new VariableProperty();
			}

			var temp = _cachedProperty;
			_cachedProperty = _propertyProvider.GetProperty();
			_propertyProvider.SetProperty(temp);
			if (_styleSheet != null)
			{
				// _styleSheet.TriggerChange();
			}

			AssetDatabase.SaveAssets();
			//ConnectionChange?.Invoke();
		}

		private void OnVariableSelected(int index)
		{
			if (_propertyProvider.GetProperty() is VariableProperty variableProperty)
			{
				var option = _dropdownOptions[index];

				variableProperty.VariableName = option == Constants.Features.Buss.MenuItems.NONE ? "" : option;

				// _variableDatabase.SetPropertyDirty(_styleSheet, _styleRule, _propertyProvider);

				if (_styleSheet != null)
				{
					_styleSheet.TriggerChange();
				}

				// ConnectionChange?.Invoke();
			}
		}
	}
}
