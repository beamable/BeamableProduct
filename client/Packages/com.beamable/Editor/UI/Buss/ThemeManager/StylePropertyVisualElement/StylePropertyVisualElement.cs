using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
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

		public StylePropertyVisualElement(StylePropertyModel model) : base(
			$"{BUSS_THEME_MANAGER_PATH}/{nameof(StylePropertyVisualElement)}/{nameof(StylePropertyVisualElement)}.uss")
		{
			_model = model;
		}

		public override void Init()
		{
			base.Init();

			_labelComponent = new TextElement {name = "propertyLabel", tooltip = _model.Tooltip};
			_labelComponent.RegisterCallback<MouseDownEvent>(_model.LabelClicked);
			Root.Add(_labelComponent);

			_valueParent = new VisualElement {name = "value"};
			Root.Add(_valueParent);

			_variableParent = new VisualElement {name = "globalVariable"};
			Root.Add(_variableParent);

			var overrideIndicator = new VisualElement();
			overrideIndicator.AddToClassList("overrideIndicator");
			Root.Add(overrideIndicator);

			Root.parent.EnableInClassList("exists", _model.IsInStyle);
			Root.parent.EnableInClassList("doesntExists", !_model.IsInStyle);

			Refresh();
		}

		public override void Refresh()
		{
			_labelComponent.text = _model.PropertyProvider.Key;

			if (_model.HasVariableConnected)
			{
				string variableName = ((VariableProperty)_model.PropertyProvider.GetProperty()).VariableName;

				if (variableName == String.Empty)
				{
					CreateMessageField(VariableDatabase.PropertyValueState.NoResult);
				}
				else
				{
					var result = _model.GetResult(out PropertySourceTracker propertySourceTracker,
					                              out IBussProperty property,
					                              out VariableDatabase.PropertyReference variableSource);
					
					CreateEditableField(property);
					SetVariableSource(variableSource);
					SetOverridenClass(propertySourceTracker, result);
				}
			}
			else
			{
				CreateEditableField(_model.PropertyProvider.GetProperty());
			}

			SetupVariableConnection();
			CheckIfIsReadOnly();
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

			// TODO: maybe add more details??
			_model.Tooltip = "Overriden";
		}

		private void CreateEditableField(IBussProperty property)
		{
			_propertyVisualElement = property.GetVisualElement();

			if (_propertyVisualElement != null)
			{
				_propertyVisualElement.UpdatedStyleSheet = _model.IsInStyle ? _model.StyleSheet : null;
				_propertyVisualElement.Init();
				_valueParent.Add(_propertyVisualElement);
			}
		}

		private void CreateMessageField(VariableDatabase.PropertyValueState result)
		{
			string text;
			switch (result)
			{
				case VariableDatabase.PropertyValueState.MultipleResults:
					text = "Multiple possible values.";
					break;
				case VariableDatabase.PropertyValueState.NoResult:
					text = "Select variable.";
					break;
				case VariableDatabase.PropertyValueState.VariableLoopDetected:
					text = "Variable loop-reference detected.";
					break;
				default:
					text = "Something is wrong here.";
					break;
			}

			_valueParent.Clear();
			_propertyVisualElement = new CustomMessageBussPropertyVisualElement(text);
			_valueParent.Add(_propertyVisualElement);
			_propertyVisualElement.Init();
		}

		private void SetOverridenClass(PropertySourceTracker context, VariableDatabase.PropertyValueState result)
		{
			bool overriden = false;
			if (context != null && result == VariableDatabase.PropertyValueState.SingleResult)
			{
				overriden = _model.PropertyProvider != context.GetUsedPropertyProvider(_model.PropertyProvider.Key);
			}

			EnableInClassList("overriden", overriden);
		}

		private void SetupVariableConnection()
		{
			if (_model.PropertyProvider.IsVariable)
				return;

			if (_variableConnection == null)
			{
				_variableConnection = new VariableConnectionVisualElement(_model);
				_variableConnection.Init();
				_variableConnection.Refresh();
				_variableParent.Add(_variableConnection);
			}
		}

		private void SetVariableSource(VariableDatabase.PropertyReference variableSource)
		{
			if (_model.PropertyProvider.HasVariableReference && variableSource.PropertyProvider != null)
			{
				if (variableSource.StyleSheet == null)
				{
					_model.Tooltip = $"Variable: {variableSource.PropertyProvider.Key}\n" +
					                 "Declared in inline style.";
				}
				else
				{
					_model.Tooltip = $"Variable: {variableSource.PropertyProvider.Key}\n" +
					                 $"Selector: {variableSource.StyleRule.SelectorString}\n" +
					                 $"Style sheet: {variableSource.StyleSheet.name}";
				}
			}
			else
			{
				_model.Tooltip = String.Empty;
			}
		}
	}
}
