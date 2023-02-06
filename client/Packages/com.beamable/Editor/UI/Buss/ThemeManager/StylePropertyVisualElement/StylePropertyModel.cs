using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{

	public class StylePropertyModel
	{
		private readonly ThemeModel _parentModel;
		private readonly Action<string> _removePropertyAction;
		private readonly Action _globalRefresh;
		private readonly Action<string, BussPropertyValueType> _setValueTypeAction;
		private readonly BussPropertyProvider _templateProperty;
		private readonly IVariableNameProvider _variableNameProvider;
		private readonly bool _allowInherited;
		private readonly bool _allowComputed;
		private readonly Func<IBussProperty> _defaultValueFactory;
		private readonly Action<IBussProperty> _changeHandler;
		public IVariableNameProvider VariableNameProvider => _variableNameProvider ?? PropertySourceTracker;
		public BussStyleSheet StyleSheet { get; }
		public BussStyleRule StyleRule { get; }
		public BussPropertyProvider PropertyProvider { get; }
		public PropertySourceTracker PropertySourceTracker { get; }
		public BussElement AppliedToElement { get; }
		public bool HasElementContext => _parentModel.HasElementContext;
		public BussElement InlineStyleOwner { get; }
		public string Tooltip { get; }
		public int VariableDropdownOptionIndex => GetOptionIndex();
		public List<DropdownEntry> DropdownOptions => GetDropdownOptions();

		public bool IsVariable => PropertyProvider.IsVariable;

		private bool _forceIsInStyle;
		public bool IsInStyle => _forceIsInStyle || IsInline || (StyleRule != null && StyleRule.Properties.Contains(PropertyProvider));
		public bool IsWritable => IsInline || (StyleSheet != null && StyleSheet.IsWritable);
		public bool IsInline => InlineStyleOwner != null;

		public bool IsInherited => PropertyProvider.ValueType == BussPropertyValueType.Inherited;
		public bool IsInitial => PropertyProvider.ValueType == BussPropertyValueType.Initial;
		public bool HasVariableConnected => PropertyProvider.HasVariableReference;
		public bool IsComputedProperty => PropertyProvider.IsComputedReference;
		public bool IsVariableConnectionEmpty =>
			(PropertyProvider.GetProperty() is VariableProperty r) && string.IsNullOrEmpty(r.VariableName);
		public bool HasNonValueConnection => IsComputedProperty || HasVariableConnected || IsInherited || IsInitial;

		public bool IsOverriden
		{
			get
			{
				if (PropertySourceTracker == null) return false;
				var appliedProvider = PropertySourceTracker.GetUsedPropertyProvider(PropertyProvider.Key, out var rank);
				if (PropertyProvider.ValueType == BussPropertyValueType.Inherited)
				{
					// I must be the first inherited property...
					var firstProvider = PropertySourceTracker.GetAllSources(PropertyProvider.Key).FirstOrDefault();
					return firstProvider?.PropertyProvider != PropertyProvider;
				}

				return appliedProvider != PropertyProvider;
			}
		}

		public StylePropertyModel(ThemeModel parentModel,
								  BussStyleSheet styleSheet,
								  BussStyleRule styleRule,
								  BussPropertyProvider propertyProvider,
								  PropertySourceTracker propertySourceTracker,
								  BussElement appliedToElement,
								  BussElement inlineStyleOwner,
								  Action<string> removePropertyAction,
								  Action globalRefresh,
								  Action<string, BussPropertyValueType> setValueTypeAction,
								  Action<IBussProperty> onPropertyChanged=null,
								  BussPropertyProvider templateProperty=null,
								  IVariableNameProvider variableNameProvider=null,
								  bool allowInherited=true,
								  bool allowComputed=true,
								  Func<IBussProperty> defaultValueFactory=null)
		{
			_changeHandler = onPropertyChanged ?? DefaultPropertyChangeHandler;
			_variableNameProvider = variableNameProvider;
			_allowInherited = allowInherited;
			_allowComputed = allowComputed;
			_defaultValueFactory = defaultValueFactory ?? (() =>
			{
				var key = _templateProperty?.Key ?? PropertyProvider.Key;
				var nextProp = BussStyle.GetDefaultValue(key).CopyProperty();
				return nextProp;
			});
			_parentModel = parentModel;
			_removePropertyAction = removePropertyAction;
			_globalRefresh = globalRefresh;
			_setValueTypeAction = setValueTypeAction;
			_templateProperty = templateProperty ?? propertyProvider;
			StyleSheet = styleSheet;
			StyleRule = styleRule;
			PropertyProvider = propertyProvider;
			PropertySourceTracker = propertySourceTracker;
			AppliedToElement = appliedToElement;
			InlineStyleOwner = inlineStyleOwner;

			if (IsOverriden && IsInStyle && PropertySourceTracker != null)
			{
				PropertyReference reference =
					PropertySourceTracker.GetUsedPropertyReference(PropertyProvider.Key);

				if (reference.StyleRule == StyleRule)
				{
					Tooltip = $"{PropertyProvider.Key} is not inherited by default.";
				}
				else
				{
					Tooltip = reference.StyleRule != null
						? $"Property is overriden by {reference.StyleRule.SelectorString} rule from {reference.StyleSheet.name} stylesheet"
						: "Property is overriden by inline style";
				}
				
			}
			else
			{
				Tooltip = String.Empty;
			}
		}

		public StylePropertyModel CreateChildModel(ComputedPropertyArg arg, Action<IBussProperty> onChange)
		{
			return new StylePropertyModel(
				_parentModel,
				StyleSheet,
				StyleRule,
				BussPropertyProvider.Create(arg.Name, arg.Property, writeEffects: arg.SetProperty),
				null,
				AppliedToElement,
				InlineStyleOwner,
				null,
				_globalRefresh,
				(_, __) => throw new NotImplementedException(),
				onChange,
				templateProperty: PropertyProvider,
				variableNameProvider: PropertySourceTracker,
				allowComputed:false,
				allowInherited:false,
				defaultValueFactory: () => arg.TemplateProperty.CopyProperty())
			{
				_forceIsInStyle = true
			};
		}


		public void LabelClicked(MouseDownEvent evt)
		{

			if (StyleSheet != null && !StyleSheet.IsWritable)
			{
				return;
			}

			List<GenericMenuCommand> commands = new List<GenericMenuCommand>
			{
				
			};

			var removeCommand = new GenericMenuCommand(Constants.Features.Buss.MenuItems.REMOVE, () =>
			{
				_removePropertyAction?.Invoke(PropertyProvider.Key);
			});

			if (_removePropertyAction != null)
			{
				commands.Add(removeCommand);
			}
			
			GenericMenu context = new GenericMenu();

			foreach (GenericMenuCommand command in commands)
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
		}

		public IBussProperty GetInitialValue()
		{
			return _defaultValueFactory?.Invoke();
		}

		public void OnLinkButtonClicked(MouseDownEvent mouseDownEvent)
		{
			Undo.RecordObject(StyleSheet, "Use keyword");

			var isComputed = PropertyProvider.IsComputedReference;
			var isVariable = PropertyProvider.HasVariableReference;
			var isValueType = PropertyProvider.ValueType == BussPropertyValueType.Value;

			var hasLink = isComputed || isVariable || !isValueType;
			if (hasLink)
			{
				var nextProp = _defaultValueFactory?.Invoke();
				PropertyProvider.SetProperty(nextProp);
				
			}
			else
			{
				PropertyProvider.SetProperty(new VariableProperty());
			}
			

			if (StyleSheet != null)
			{
				StyleSheet.TriggerChange();
			}

			AssetDatabase.SaveAssets();
			_globalRefresh?.Invoke();
		}

		public void OnVariableSelected(int index)
		{
			var option = DropdownOptions[index];

			if (option.DisplayName == Constants.Features.Buss.MenuItems.INHERITED_VALUE)
			{
				Undo.RecordObject(StyleSheet, "Set inherited");
				PropertyProvider.GetProperty().ValueType = BussPropertyValueType.Inherited;
			}
			else if (option.DisplayName == Constants.Features.Buss.MenuItems.INITIAL_VALUE)
			{
				Undo.RecordObject(StyleSheet, "Set initial");
				PropertyProvider.GetProperty().ValueType = BussPropertyValueType.Initial;
			}
			else if (option.DisplayName == Constants.Features.Buss.MenuItems.COMPUTED_VALUE)
			{
				Undo.RecordObject(StyleSheet, "Set computed");
				PropertyProvider.GetProperty().ValueType = BussPropertyValueType.Value;
				
				var baseType = PropertyProvider.GetInitialPropertyType();
				if (!BussStyle.TryGetOperatorBinding(baseType, out var operatorBinding))
				{
					Debug.LogError("Unable to find binding for key value...");
				}

				var initialValue = operatorBinding.Create();
				PropertyProvider.SetProperty(initialValue);
			}
			else
			{
				Undo.RecordObject(StyleSheet, "Set variable");
				var property = PropertyProvider.GetProperty();
				if (property is VariableProperty existingVariable)
				{
					existingVariable.ValueType = BussPropertyValueType.Value;
					existingVariable.VariableName = option.DisplayName;
					PropertyProvider.SetProperty(property);
				}
				else
				{
					PropertyProvider.SetProperty(new VariableProperty(option.DisplayName)
					{
						ValueType = BussPropertyValueType.Value
					});
				}
			}

			if (StyleSheet != null)
			{
				StyleSheet.TriggerChange();
			}

			AssetDatabase.SaveAssets();
			_globalRefresh?.Invoke();
		}

		private List<DropdownEntry> GetDropdownOptions()
		{
			var options = new List<DropdownEntry> { new DropdownEntry(Constants.Features.Buss.MenuItems.INITIAL_VALUE) };

			if (_allowInherited)
			{
				var inheritedOption = new DropdownEntry(Constants.Features.Buss.MenuItems.INHERITED_VALUE, false);
				options.Add(inheritedOption);
			}

			var propType = _templateProperty.GetInitialPropertyType();
			if (_allowComputed && BussStyle.TryGetOperatorBinding(propType,
				                                    out var operatorBinding) && operatorBinding.HasAnyFactories)
			{
				var mathOption = new DropdownEntry(Constants.Features.Buss.MenuItems.COMPUTED_VALUE, false);
				options.Add(mathOption);
			}
			
			

			if (_parentModel.HasElementContext)
			{
				var baseType = GetInitialValue().GetType();
				if (VariableNameProvider != null)
				{
					var variables = VariableNameProvider.GetAllVariableNames(baseType).ToList();
					if (variables.Count > 0)
					{
						options[options.Count - 1].LineBelow = true;
					}
					options.AddRange(variables);
				}
			}
			else
			{
				options.Add(new DropdownEntry("Variable"));
			}

			return options;
		}

		private int GetOptionIndex()
		{
			var options = GetDropdownOptions();

			if (PropertyProvider.ValueType == BussPropertyValueType.Initial)
			{
				return 0;
			}
			if (PropertyProvider.ValueType == BussPropertyValueType.Inherited && _allowInherited)
			{
				return options.FindIndex(entry => entry.DisplayName == Constants.Features.Buss.MenuItems.INHERITED_VALUE);
			}

			if (PropertyProvider.IsComputedReference && _allowComputed)
			{
				return options.FindIndex(entry => entry.DisplayName == Constants.Features.Buss.MenuItems.COMPUTED_VALUE);
			}

			if (_parentModel.HasElementContext)
			{
				string variableName = string.Empty;
				if (HasVariableConnected)
				{
					variableName = ((VariableProperty)PropertyProvider.GetProperty()).VariableName;
				}

				var value = options.FindIndex(option => option.DisplayName.Equals(variableName));
				value = Mathf.Clamp(value, -1, options.Count - 1);
				return value;
			}
			else
			{
				return 3;
			}
		}

		public void OnPropertyChanged(IBussProperty property)
		{
			_changeHandler?.Invoke(property);
		}

		private void DefaultPropertyChangeHandler(IBussProperty property)
		{
			if (StyleRule != null)
			{
				if (!StyleRule.HasProperty(PropertyProvider.Key))
				{
					StyleRule.TryAddProperty(PropertyProvider.Key, property);
				}
				else
				{
					var old = StyleRule.GetPropertyProvider(PropertyProvider.Key).GetProperty();
					if (old is VariableProperty oldVar)
					{
						return;
					}
					StyleRule.GetPropertyProvider(PropertyProvider.Key).SetProperty(property);

					var areDifferentTypes = old.GetType() != property.GetType();
					if (areDifferentTypes)
					{
						StyleSheet.TriggerChange();
						_globalRefresh?.Invoke();
					}
				}
			}
			
			/*
			 * Naively, we could tell the entire style sheet to recompute.
			 * However, this will lead to a large change that scales poorly compared to the total number of elements,
			 *  because every single element will need to checked against every rule.
			 * Ideally, we would only need to update the elements that apply the rule.
			 * This can be done, because we _already_ know which elements apply to which rules. Instead of doing a whole sheet
			 *  refresh, we can simply identify which elements need to be updated.
			 */
			if (StyleRule == null)
			{
				AppliedToElement.ReapplyStyles();
			}
			else
			{
				foreach (var elem in StyleCache.Instance.GetElementsReferencingRule(StyleRule, PropertyProvider.Key))
				{
					elem.ReapplyStyles();
				}
			}
			
			if (StyleSheet != null)
			{
#if UNITY_EDITOR
				EditorUtility.SetDirty(StyleSheet);
				AssetDatabase.SaveAssets();
#endif
			}
		}
	}
}
