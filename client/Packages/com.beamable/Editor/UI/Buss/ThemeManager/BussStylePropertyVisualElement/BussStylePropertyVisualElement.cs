using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
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
		private BussStyleSheet _styleSheet;
		private BussStyleRule _styleRule;
		private BussPropertyProvider _propertyProvider;
		private BussStyleSheet _externalVariableSource;

		public BussPropertyProvider PropertyProvider => _propertyProvider;
		public string PropertyKey => PropertyProvider.Key;

		public bool PropertyIsInStyle => _styleRule.Properties.Contains(_propertyProvider);

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

			Root.parent.EnableInClassList("exists", PropertyIsInStyle);
			Root.parent.EnableInClassList("doesntExists", !PropertyIsInStyle);
			Refresh();
		}

		private void LabelClicked(MouseDownEvent evt)
		{
			if (_styleSheet.IsReadOnly)
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
						  BussStyleRule styleRule,
						  BussPropertyProvider property,
						  VariableDatabase variableDatabase)
		{
			_variableDatabase = variableDatabase;
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_propertyProvider = property;

			Init();
		}

		private void SetupEditableField()
		{
			var baseType = BussStyle.GetBaseType(_propertyProvider.Key);
			if (_propertyVisualElement != null)
			{
				if (_propertyVisualElement.BaseProperty ==
					_propertyProvider.GetProperty().GetEndProperty(_variableDatabase, _styleRule, out _externalVariableSource))
				{
					_propertyVisualElement.OnPropertyChangedExternally();
					return;
				}

				_propertyVisualElement.RemoveFromHierarchy();
				_propertyVisualElement.Destroy();
			}

			_propertyVisualElement = _propertyProvider.GetVisualElement(_variableDatabase, _styleRule, out _externalVariableSource, baseType);

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
				if (_styleRule.TryAddProperty(_propertyProvider.Key, _propertyProvider.GetProperty(), out _))
				{
					_styleSheet.TriggerChange();
				}
			}
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
