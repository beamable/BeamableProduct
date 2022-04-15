using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using Editor.UI.Buss.ThemeManager;
using System;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class BussStylePropertyVisualElement : BeamableBasicVisualElement
	{
		private BussPropertyVisualElement _propertyVisualElement;
		private VariableConnectionVisualElement _variableConnection;
		private VisualElement _valueParent;
		private VisualElement _variableParent;
		private VisualElement _removeButton;
		private TextElement _labelComponent;

		private VariableDatabase _variableDatabase;
		private PropertySourceTracker _propertySourceTracker;
		private BussStyleSheet _styleSheet;
		private BussStyleRule _styleRule;
		private BussPropertyProvider _propertyProvider;
		private BussStyleSheet _externalVariableSource = null;
		private bool _editMode;

		public BussPropertyProvider PropertyProvider => _propertyProvider;
		public string PropertyKey => PropertyProvider.Key;

		public bool PropertyIsInStyle => _styleRule.Properties.Contains(_propertyProvider);

		public string VariableSource
		{
			get => _labelComponent.tooltip;
			set => _labelComponent.tooltip = value;
		}

		public BussStylePropertyVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/BussStylePropertyVisualElement/BussStylePropertyVisualElement.uss")
		{ }

		public override void Init()
		{
			base.Init();

			VisualElement buttonContainer = new VisualElement();
			buttonContainer.name = "removeButtonContainer";

			_removeButton = new VisualElement();
			_removeButton.name = "removeButton";
			buttonContainer.Add(_removeButton);
			Root.Add(buttonContainer);

			_removeButton.RegisterCallback<MouseDownEvent>(OnRemoveButtonClicked);
			buttonContainer.SetHidden(!_editMode);

			_labelComponent = new TextElement();
			_labelComponent.name = "propertyLabel";
			Root.Add(_labelComponent);

			_valueParent = new VisualElement();
			_valueParent.name = "value";
			Root.Add(_valueParent);

			_variableParent = new VisualElement();
			_variableParent.name = "globalVariable";
			Root.Add(_variableParent);

			var overrideIndicator = new VisualElement();
			overrideIndicator.AddToClassList("overrideIndicator");
			Root.Add(overrideIndicator);

			Root.parent.EnableInClassList("exists", PropertyIsInStyle);
			Root.parent.EnableInClassList("doesntExists", !PropertyIsInStyle);
			Refresh();
		}

		public override void Refresh()
		{
			_labelComponent.text = _propertyProvider.Key;

			SetupEditableField();
			SetupVariableConnection();
			CheckIfIsReadOnly();
		}

		public void Setup(BussStyleSheet styleSheet,
						  BussStyleRule styleRule,
						  BussPropertyProvider property,
						  VariableDatabase variableDatabase,
						  PropertySourceTracker propertySourceTracker,
						  bool editMode)
		{
			_editMode = editMode;
			_variableDatabase = variableDatabase;
			_propertySourceTracker = propertySourceTracker;
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_propertyProvider = property;

			Init();
		}

		public void SetPropertySourceTracker(PropertySourceTracker tracker)
		{
			_propertySourceTracker = tracker;
			SetupEditableField();
		}

		private void SetupEditableField()
		{
			PropertySourceTracker context = null;
			if (_propertySourceTracker != null && _propertySourceTracker.Element != null)
			{
				if (_styleRule.Selector?.CheckMatch(_propertySourceTracker.Element) ?? false)
				{
					context = _propertySourceTracker;
				}
			}
			
			var result =
				BussStylePropertyVisualElementUtility.TryGetProperty(_propertyProvider, _styleRule, _variableDatabase,
				                                                     context, out var property, out var variableSource);

			SetVariableSource(variableSource);

			SetOverritenClass(context, result);

			if (result != BussStylePropertyVisualElementUtility.PropertyValueState.SingleResult)
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

		private void SetOverritenClass(PropertySourceTracker context, BussStylePropertyVisualElementUtility.PropertyValueState result) {
			var overriten = false;
			if (context != null && result == BussStylePropertyVisualElementUtility.PropertyValueState.SingleResult) {
				overriten = _propertyProvider != context.GetUsedPropertyProvider(_propertyProvider.Key);
			}

			EnableInClassList("overriten", overriten);
		}

		private void CreateMessageField(BussStylePropertyVisualElementUtility.PropertyValueState result) {
			string text;
			switch (result) {
				case BussStylePropertyVisualElementUtility.PropertyValueState.MultipleResults:
					text = "Multiple possible values.";
					break;
				case BussStylePropertyVisualElementUtility.PropertyValueState.NoResult:
					text = "No possible value.";
					break;
				case BussStylePropertyVisualElementUtility.PropertyValueState.VariableLoopDetected:
					text = "Variable loop-reference detected.";
					break;
				default:
					text = "Something is wrong here.";
					break;
			}

			CreateMessageField(text);
		}

		private void SetVariableSource(VariableDatabase.PropertyReference variableSource) {
			if (_propertyProvider.HasVariableReference && variableSource.propertyProvider != null) {
				if (variableSource.styleSheet == null) {
					VariableSource = $"Variable: {variableSource.propertyProvider.Key}\n" +
					                 "Declared in inline style.";
				}
				else {
					VariableSource = $"Variable: {variableSource.propertyProvider.Key}\n" +
					                 $"Selector: {variableSource.styleRule.SelectorString}\n" +
					                 $"Style sheet: {variableSource.styleSheet.name}";
				}
			}
			else {
				VariableSource = null;
			}
		}

		private void DestroyEditableField()
		{
			if (_propertyVisualElement == null) return;
			_propertyVisualElement.RemoveFromHierarchy();
			_propertyVisualElement.Destroy();
			_propertyVisualElement = null;
		}

		private void CreateEditableField(IBussProperty property) {
			if (_propertyVisualElement != null)
			{
				DestroyEditableField();
			}
			_propertyVisualElement = property.GetVisualElement();

			if (_propertyVisualElement != null) {
				_propertyVisualElement.UpdatedStyleSheet = PropertyIsInStyle ? _styleSheet : null;
				_valueParent.Add(_propertyVisualElement);
				_propertyVisualElement.Init();
				_propertyVisualElement.OnValueChanged -= HandlePropertyChanged;
				_propertyVisualElement.OnValueChanged += HandlePropertyChanged;
			}
		}

		private void CreateMessageField(string text)
		{
			if (_propertyVisualElement != null)
			{
				DestroyEditableField();
			}

			_propertyVisualElement = new CustomMessageBussPropertyVisualElement(text);
			_valueParent.Add(_propertyVisualElement);
			_propertyVisualElement.Init();
		}

		void HandlePropertyChanged()
		{
			if (_propertyProvider.IsVariable)
			{
				_variableDatabase.SetVariableDirty(_propertyProvider.Key);
			}
			else if (_propertyProvider.GetProperty() is VariableProperty vp)
			{
				_variableDatabase.SetVariableDirty(vp.VariableName);
			}
			else
			{
				_variableDatabase.SetPropertyDirty(_styleSheet, _styleRule, _propertyProvider);
			}

			if (!PropertyIsInStyle)
			{
				_styleRule.TryAddProperty(_propertyProvider.Key, _propertyProvider.GetProperty(), out _);
				_styleSheet.TriggerChange();
			}
		}

		protected override void OnDestroy()
		{
			if (_propertyVisualElement != null)
			{
				_propertyVisualElement.OnValueChanged -= HandlePropertyChanged;
			}
			_removeButton?.UnregisterCallback<MouseDownEvent>(OnRemoveButtonClicked);
		}

		private void OnRemoveButtonClicked(MouseDownEvent evt)
		{
			IBussProperty bussProperty = _propertyProvider.GetProperty();
			_styleSheet.RemoveStyleProperty(bussProperty, _styleRule.SelectorString);
		}

		private void SetupVariableConnection()
		{
			if (_propertyProvider.IsVariable)
				return;

			if (_variableConnection == null)
			{
				_variableConnection = new VariableConnectionVisualElement();
				_variableParent.Add(_variableConnection);
				_variableConnection.Refresh();
			}

			_variableConnection.Setup(_styleSheet, _styleRule, _propertyProvider, _variableDatabase);
		}

		private void CheckIfIsReadOnly()
		{
			var styleSheet = _externalVariableSource != null ? _externalVariableSource : _styleSheet;
			var isReadOnly = styleSheet.IsReadOnly;

			_labelComponent.SetEnabled(!isReadOnly);
			_propertyVisualElement.SetEnabled(!isReadOnly);

			if (_variableConnection != null)
			{
				_variableConnection.SetEnabled(!_styleSheet.IsReadOnly);
			}
		}
	}
}
