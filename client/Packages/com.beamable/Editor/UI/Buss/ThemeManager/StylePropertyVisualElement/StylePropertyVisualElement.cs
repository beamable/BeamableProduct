using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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

			var overrideIndicatorSpacer = new VisualElement();
			overrideIndicatorSpacer.AddToClassList("overrideIndicatorSpacer");
			_overrideIndicatorParent.Add(overrideIndicatorSpacer);

			Root.parent.EnableInClassList("exists", _model.IsInStyle);
			Root.parent.EnableInClassList("doesntExists", !_model.IsInStyle);

			Refresh();
		}

		public override void Refresh()
		{
			_labelComponent.text = ThemeManagerHelper.FormatKey(_model.PropertyProvider.Key);

			_valueParent.Clear();
			
			if (_model.IsInherited)
			{
				if (_model.HasElementContext)
				{
					var srcTracker = _model.PropertySourceTracker;
					if (srcTracker != null)
					{
						var appliedPropertyProvider = srcTracker.GetNextInheritedProperty(_model.PropertyProvider);
						if (appliedPropertyProvider != null)
						{
							var appliedProperty = appliedPropertyProvider.GetProperty();
							var field = CreateEditableField(appliedProperty);
							field.DisableInput();
						}
						else
						{
							CreateMessageField("Unknown inherited property");
						}
					}
				}
				else
				{
					CreateMessageField("No context to show inheritance.");
				}
			}
			else if (_model.IsInitial)
			{
				var initialValue = BussStyle.GetDefaultValue(_model.PropertyProvider.Key);
				var field = CreateEditableField(initialValue);
				field.DisableInput("The initial value cannot be changed.");
			}
			else if (_model.HasVariableConnected)
			{
				string variableName = ((VariableProperty)_model.PropertyProvider.GetProperty()).VariableName;

				if (!_model.HasElementContext)
				{
					var tf = new TextField();
					if (variableName != null && variableName.StartsWith("--"))
					{
						variableName = variableName.Substring(2);
					}
					
					tf.SetValueWithoutNotify(variableName);
					tf.RegisterValueChangedCallback(evt =>
					{
						var newValue = evt.newValue;
						EditorDebouncer.Debounce("buss-manual-variable-name", () =>
						{
							Undo.RecordObject(_model.StyleSheet, "Change variable");
							((VariableProperty)_model.PropertyProvider.GetProperty()).VariableName = "--" +newValue;
							if (_model.StyleSheet != null)
							{
								_model.StyleSheet.TriggerChange();
							}
							AssetDatabase.SaveAssets();
						});
					});
					_valueParent.Add(tf);
				}
				else
				{


					if (variableName == String.Empty)
					{
						CreateMessageField(PropertyValueState.NoResult);
					}
					else
					{
						var srcTracker = _model.PropertySourceTracker;
						if (srcTracker != null)
						{
							var appliedPropertyProvider =
								srcTracker.ResolveVariableProperty(_model.PropertyProvider.Key);



							if (appliedPropertyProvider != null)
							{
								var field = CreateEditableField(appliedPropertyProvider.PropertyProvider.GetProperty());
								field.DisableInput("The field is disabled because it references a variable.");

								void UpdateField()
								{
									if (field.IsRemoved) return;
									field.OnPropertyChangedExternally();
									appliedPropertyProvider.PropertyProvider.GetProperty().OnValueChanged +=
										UpdateField;
								}

								appliedPropertyProvider.PropertyProvider.GetProperty().OnValueChanged += UpdateField;
							}
						}
					}
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

		private BussPropertyVisualElement CreateEditableField(IBussProperty property)
		{
			var element = _propertyVisualElement = property.GetVisualElement();

			
			if (_propertyVisualElement == null)
			{
				return null;
			}

			_propertyVisualElement.OnValueChanged = _model.OnPropertyChanged;
			_propertyVisualElement.OnBeforeChange += () =>
			{
				if (_model.IsInline)
				{
					Undo.RecordObject(_model.AppliedToElement, $"Change {_model.PropertyProvider.Key}");
				}
				else
				{
					Undo.RecordObject(_model.StyleSheet, $"Change {_model.PropertyProvider.Key}");
				}
			};
			
			_propertyVisualElement.UpdatedStyleSheet = _model.StyleSheet;
			_propertyVisualElement.Init();
			_valueParent.Add(_propertyVisualElement);
			return element;
		}

		private void CreateMessageField(string message, bool clearParent = true)
		{
			if (clearParent)
			{
				_valueParent.Clear();
			}

			_propertyVisualElement = new CustomMessageBussPropertyVisualElement(message) { name = "message" };
			_valueParent.Add(_propertyVisualElement);
			_propertyVisualElement.Init();
		}

		private void CreateMessageField(PropertyValueState result)
		{
			string text;
			switch (result)
			{
				case PropertyValueState.NoResult:
					text = "Select variable or keyword";
					break;
				case PropertyValueState.VariableLoopDetected:
					text = "Variable loop-reference detected";
					break;
				default:
					text = "Something is wrong here";
					break;
			}

			CreateMessageField(text);
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
