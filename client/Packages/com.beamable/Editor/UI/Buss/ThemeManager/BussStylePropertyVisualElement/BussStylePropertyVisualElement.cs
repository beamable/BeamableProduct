using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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
		private BussStyleDescription _styleDescription;
		private BussStyleRule _styleRule;
		private BussPropertyProvider _propertyProvider;

		public BussElement InlineStyleOwner { get; set; }

		public event Action PropertyChanged;

		public BussPropertyProvider PropertyProvider => _propertyProvider;
		public string PropertyKey => PropertyProvider.Key;

		public bool PropertyIsInStyle => _styleDescription.Properties.Contains(_propertyProvider);

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

			_labelComponent = new TextElement();
			_labelComponent.name = "propertyLabel";
			_labelComponent.RegisterCallback<MouseDownEvent>(LabelClicked);
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

		private void LabelClicked(MouseDownEvent evt)
		{
			if (_styleSheet != null && !_styleSheet.IsWritable)
			{
				return;
			}

			List<GenericMenuCommand> commands = new List<GenericMenuCommand>();
			commands.Add(new GenericMenuCommand(Constants.Features.Buss.MenuItems.REMOVE, RemoveProperty));

			GenericMenu context = new GenericMenu();

			foreach (GenericMenuCommand command in commands)
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
		}

		public override void Refresh()
		{
			_labelComponent.text = _propertyProvider.Key;

			SetupEditableField();
			SetupVariableConnection();
			CheckIfIsReadOnly();
		}

		public void Setup(BussStyleSheet styleSheet,
						  BussStyleDescription styleRule,
						  BussPropertyProvider property,
						  VariableDatabase variableDatabase,
						  PropertySourceTracker propertySourceTracker)
		{
			_variableDatabase = variableDatabase;
			_propertySourceTracker = propertySourceTracker;
			_styleSheet = styleSheet;
			_styleDescription = styleRule;
			_styleRule = styleRule as BussStyleRule;
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
				if (_styleRule?.Selector?.CheckMatch(_propertySourceTracker.Element) ?? false)
				{
					context = _propertySourceTracker;
				}
			}

			BussStylePropertyVisualElementUtility.PropertyValueState result =
				BussStylePropertyVisualElementUtility.TryGetProperty(_propertyProvider, _styleDescription, _variableDatabase,
																	 context, out IBussProperty property, out VariableDatabase.PropertyReference variableSource);

			SetVariableSource(variableSource);

			SetOverridenClass(context, result);

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

		private void SetOverridenClass(PropertySourceTracker context, BussStylePropertyVisualElementUtility.PropertyValueState result)
		{
			bool overriden = false;
			if (context != null && result == BussStylePropertyVisualElementUtility.PropertyValueState.SingleResult)
			{
				overriden = _propertyProvider != context.GetUsedPropertyProvider(_propertyProvider.Key);
			}

			EnableInClassList("overriden", overriden);
		}

		private void CreateMessageField(BussStylePropertyVisualElementUtility.PropertyValueState result)
		{
			string text;
			switch (result)
			{
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

			if (_propertyVisualElement != null)
			{
				DestroyEditableField();
			}

			_propertyVisualElement = new CustomMessageBussPropertyVisualElement(text);
			_valueParent.Add(_propertyVisualElement);
			_propertyVisualElement.Init();
		}

		private void SetVariableSource(VariableDatabase.PropertyReference variableSource)
		{
			if (_propertyProvider.HasVariableReference && variableSource.PropertyProvider != null)
			{
				if (variableSource.StyleSheet == null)
				{
					VariableSource = $"Variable: {variableSource.PropertyProvider.Key}\n" +
									 "Declared in inline style.";
				}
				else
				{
					VariableSource = $"Variable: {variableSource.PropertyProvider.Key}\n" +
									 $"Selector: {variableSource.StyleRule.SelectorString}\n" +
									 $"Style sheet: {variableSource.StyleSheet.name}";
				}
			}
			else
			{
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

		private void CreateEditableField(IBussProperty property)
		{
			if (_propertyVisualElement != null)
			{
				DestroyEditableField();
			}
			_propertyVisualElement = property.GetVisualElement();

			if (_propertyVisualElement != null)
			{
				_propertyVisualElement.UpdatedStyleSheet = PropertyIsInStyle ? _styleSheet : null;
				_valueParent.Add(_propertyVisualElement);
				_propertyVisualElement.Init();
				_propertyVisualElement.OnValueChanged -= HandlePropertyChanged;
				_propertyVisualElement.OnValueChanged += HandlePropertyChanged;
			}
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
				if (_styleRule.TryAddProperty(_propertyProvider.Key, _propertyProvider.GetProperty()))
				{
					_styleSheet.TriggerChange();
				}
			}

			PropertyChanged?.Invoke();
		}

		protected override void OnDestroy()
		{
			if (_propertyVisualElement != null)
			{
				_propertyVisualElement.OnValueChanged -= HandlePropertyChanged;
				_labelComponent.UnregisterCallback<MouseDownEvent>(LabelClicked);
			}
		}

		private void RemoveProperty()
		{
			if (InlineStyleOwner != null)
			{
				InlineStyleOwner.InlineStyle.Properties.Remove(_propertyProvider);
				PropertyChanged?.Invoke();
			}
			else
			{
				IBussProperty bussProperty = _propertyProvider.GetProperty();
				_styleSheet.RemoveStyleProperty(bussProperty, _styleRule);
			}
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
				_variableConnection.OnConnectionChange += () => PropertyChanged?.Invoke();
			}

			_variableConnection.Setup(_styleSheet, _styleRule, _propertyProvider, _variableDatabase);
		}

		private void CheckIfIsReadOnly()
		{
			bool isWritable = _styleSheet != null && _styleSheet.IsWritable;

			_labelComponent.SetEnabled(isWritable);
			_propertyVisualElement.SetEnabled(isWritable);

			if (_variableConnection != null)
			{
				_variableConnection.SetEnabled(isWritable);
			}
		}
	}
}
