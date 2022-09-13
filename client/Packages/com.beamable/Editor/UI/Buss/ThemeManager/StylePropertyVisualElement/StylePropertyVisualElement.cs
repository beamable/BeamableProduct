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

			_model.Change += Refresh;

			Refresh();
		}

		public override void Refresh()
		{
			_labelComponent.text = _model.PropertyProvider.Key;

			SetupEditableField();
			SetupVariableConnection();
			CheckIfIsReadOnly();
		}

		protected override void OnDestroy()
		{
			_model.Change -= Refresh;

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

			// TODO: set tooltip text 
		}

		private void CreateEditableField(IBussProperty property)
		{
			if (_propertyVisualElement != null)
			{
				DestroyEditableField();
			}

			_propertyVisualElement = property.GetVisualElement();

			if (_propertyVisualElement != null)
			{
				_propertyVisualElement.UpdatedStyleSheet = _model.IsInStyle ? _model.StyleSheet : null;
				_valueParent.Add(_propertyVisualElement);
				_propertyVisualElement.Init();
			}
		}

		private void CreateMessageField(StylePropertyVisualElementUtility.PropertyValueState result)
		{
			string text;
			switch (result)
			{
				case StylePropertyVisualElementUtility.PropertyValueState.MultipleResults:
					text = "Multiple possible values.";
					break;
				case StylePropertyVisualElementUtility.PropertyValueState.NoResult:
					text = "No possible value.";
					break;
				case StylePropertyVisualElementUtility.PropertyValueState.VariableLoopDetected:
					text = "Variable loop-reference detected.";
					break;
				default:
					text = "Something is wrong here.";
					break;
			}

			if (_propertyVisualElement != null)
			{
				DestroyEditableField();
			}

			_propertyVisualElement = new CustomMessageBussPropertyVisualElement(text);
			_valueParent.Add(_propertyVisualElement);
			_propertyVisualElement.Init();
		}

		private void DestroyEditableField()
		{
			if (_propertyVisualElement == null) return;
			_propertyVisualElement.RemoveFromHierarchy();
			_propertyVisualElement.Destroy();
			_propertyVisualElement = null;
		}

		private void SetOverridenClass(PropertySourceTracker context,
		                               StylePropertyVisualElementUtility.PropertyValueState result)
		{
			bool overriden = false;
			if (context != null && result == StylePropertyVisualElementUtility.PropertyValueState.SingleResult)
			{
				overriden = _model.PropertyProvider != context.GetUsedPropertyProvider(_model.PropertyProvider.Key);
			}

			EnableInClassList("overriden", overriden);
		}

		private void SetupEditableField()
		{
			PropertySourceTracker context = null;
			if (_model.PropertySourceTracker != null && _model.PropertySourceTracker.Element != null)
			{
				if (_model.StyleRule?.Selector?.CheckMatch(_model.PropertySourceTracker.Element) ?? false)
				{
					context = _model.PropertySourceTracker;
				}
			}

			StylePropertyVisualElementUtility.PropertyValueState result =
				StylePropertyVisualElementUtility.TryGetProperty(_model.PropertyProvider, _model.StyleRule,
				                                                 _model.VariablesDatabase, context,
				                                                 out IBussProperty property,
				                                                 out VariableDatabase.PropertyReference
					                                                 variableSource);

			SetVariableSource(variableSource);

			SetOverridenClass(context, result);

			if (result != StylePropertyVisualElementUtility.PropertyValueState.SingleResult)
			{
				CreateMessageField(result);
				return;
			}

			if (_propertyVisualElement == null)
			{
				CreateEditableField(property);
				return;
			}

			if (property == _propertyVisualElement.BaseProperty)
			{
				_propertyVisualElement.OnPropertyChangedExternally();
			}
			else
			{
				CreateEditableField(property);
			}
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
				_variableConnection.Refresh();
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
