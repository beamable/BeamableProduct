using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class StylePropertyVisualElement : BeamableBasicVisualElement
	{
		private readonly StylePropertyModel _model;
		private BussPropertyVisualElement _propertyVisualElement;
		private VariableConnectionVisualElement _variableConnection;
		private TextElement _labelComponent;
		private VisualElement _removeButton;
		private VisualElement _valueParent;
		private VisualElement _variableParent;
		private VisualElement _overrideIndicatorParent;

		public StylePropertyVisualElement(StylePropertyModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(StylePropertyVisualElement)}/{nameof(StylePropertyVisualElement)}.uss")
		{
			_model = model;
		}

		public override void Init()
		{
			base.Init();

			_labelComponent = new TextElement { name = "propertyLabel" };
			_labelComponent.RegisterCallback<MouseDownEvent>(_model.LabelClicked);
			Root.Add(_labelComponent);

			_valueParent = new VisualElement { name = "value" };
			Root.Add(_valueParent);

			_variableParent = new VisualElement { name = "globalVariable" };
			Root.Add(_variableParent);

			_overrideIndicatorParent = new VisualElement { name = "overrideIndicatorParent" };
			_overrideIndicatorParent.AddToClassList("overrideIndicatorParent");
			Root.Add(_overrideIndicatorParent);

			var overrideIndicator = new VisualElement();
			overrideIndicator.AddToClassList("overrideIndicator");
			_overrideIndicatorParent.Add(overrideIndicator);

			Root.parent.EnableInClassList("exists", _model.IsInStyle);
			Root.parent.EnableInClassList("doesntExists", !_model.IsInStyle);

			Refresh();
		}

		public override void Refresh()
		{
			_labelComponent.text = ThemeManagerHelper.FormatKey(_model.PropertyProvider.Key);

			if (_model.HasVariableConnected)
			{
				string variableName = ((VariableProperty)_model.PropertyProvider.GetProperty()).VariableName;

				if (variableName == String.Empty)
				{
					CreateMessageField(VariableDatabase.PropertyValueState.NoResult);
				}
				else
				{
					_model.GetResult(out IBussProperty property, out VariableDatabase.PropertyReference variableSource);
					CreateEditableField(property);
				}
			}
			else
			{
				CreateEditableField(_model.PropertyProvider.GetProperty());
			}

			SetupVariableConnection();
			CheckIfIsReadOnly();
			EnableInClassList("overriden", _model.IsOverriden && _model.IsInStyle);

			_overrideIndicatorParent.tooltip = _model.Tooltip;
		}

		protected override void OnDestroy()
		{
			if (_propertyVisualElement != null)
			{
				_labelComponent.UnregisterCallback<MouseDownEvent>(_model.LabelClicked);
			}
		}

		private void CheckIfIsReadOnly()
		{
			_labelComponent?.SetEnabled(_model.IsWritable);
			_propertyVisualElement?.SetEnabled(_model.IsWritable);
			_variableConnection?.SetEnabled(_model.IsWritable);
		}

		private void CreateEditableField(IBussProperty property)
		{
			_propertyVisualElement = property.GetVisualElement();

			if (_propertyVisualElement == null)
			{
				return;
			}

			_propertyVisualElement.OnValueChanged = _model.OnPropertyChanged;

			_propertyVisualElement.UpdatedStyleSheet = _model.StyleSheet;
			_propertyVisualElement.Init();
			_valueParent.Add(_propertyVisualElement);
		}

		private void CreateMessageField(VariableDatabase.PropertyValueState result)
		{
			string text;
			switch (result)
			{
				case VariableDatabase.PropertyValueState.NoResult:
					text = "Select variable";
					break;
				case VariableDatabase.PropertyValueState.VariableLoopDetected:
					text = "Variable loop-reference detected";
					break;
				default:
					text = "Something is wrong here";
					break;
			}

			_valueParent.Clear();
			_propertyVisualElement = new CustomMessageBussPropertyVisualElement(text) { name = "message" };
			_valueParent.Add(_propertyVisualElement);
			_propertyVisualElement.Init();
		}

		private void SetupVariableConnection()
		{
			if (_model.PropertyProvider.IsVariable)
				return;

			if (_variableConnection == null)
			{
				_variableConnection = new VariableConnectionVisualElement(_model);
				_variableConnection.Init();
				_variableParent.Add(_variableConnection);
			}
		}
	}
}
