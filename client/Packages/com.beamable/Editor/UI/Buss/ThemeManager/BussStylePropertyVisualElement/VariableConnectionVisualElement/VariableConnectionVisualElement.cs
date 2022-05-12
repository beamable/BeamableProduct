using Beamable.UI.Buss;
using System;
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
		private VisualElement _mainElement;
		private Button _button;
		private DropdownVisualElement _dropdown;

		private VariableDatabase _variableDatabase;
		private BussStyleSheet _styleSheet;
		private BussStyleRule _styleRule;
		private BussPropertyProvider _propertyProvider;
		private IBussProperty _cachedProperty;

		private List<string> _dropdownOptions = new List<string>();

		private const string _noneOption = "None";
		private const string _addNewOption = "Add New Variable";

		public event Action OnConnectionChange;

		public bool IsConnected => _propertyProvider.GetProperty() is VariableProperty;

		public VariableConnectionVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(BussStylePropertyVisualElement)}/" +
			$"{nameof(VariableConnectionVisualElement)}/{nameof(VariableConnectionVisualElement)}")
		{ }

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

		public void Update()
		{
			_button.clickable.clicked -= OnButtonClick;
			_button.clickable.clicked += OnButtonClick;
			_button.EnableInClassList("whenConnected", IsConnected);

			var baseType = BussStyle.GetBaseType(_propertyProvider.Key);
			_dropdownOptions.Clear();
			_dropdownOptions.Add(_noneOption);
			// _dropdownOptions.Add(_addNewOption);
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

		public void Setup(BussStyleSheet styleSheet,
						  BussStyleRule styleRule,
						  BussPropertyProvider propertyProvider,
						  VariableDatabase variableDatabase) // temporary parameter
		{
			_variableDatabase = variableDatabase;
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_propertyProvider = propertyProvider;
			Update();
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
				_styleSheet.TriggerChange();
			}
			AssetDatabase.SaveAssets();
			OnConnectionChange?.Invoke();
		}

		private void OnVariableSelected(int index)
		{
			if (_propertyProvider.GetProperty() is VariableProperty variableProperty)
			{
				var option = _dropdownOptions[index];

				if (option == _noneOption)
				{
					variableProperty.VariableName = "";
				}
				else if (option == _addNewOption)
				{
					//TODO: Open new variable window here.
				}
				else
				{
					variableProperty.VariableName = option;
				}

				_variableDatabase.SetPropertyDirty(_styleSheet, _styleRule, _propertyProvider);
				if (_styleSheet != null)
				{
					_styleSheet.TriggerChange();
				}

				OnConnectionChange?.Invoke();
			}
		}
	}
}
