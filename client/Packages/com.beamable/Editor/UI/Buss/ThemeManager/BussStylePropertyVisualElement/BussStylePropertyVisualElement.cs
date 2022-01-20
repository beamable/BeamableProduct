using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using Beamable.Editor.UI.BUSS.ThemeManager;
using Beamable.Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
using System;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class BussStylePropertyVisualElement : BeamableBasicVisualElement
	{
#if UNITY_2018
		public BussStylePropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussStylePropertyVisualElement/BussStylePropertyVisualElement.2018.uss") { }
#elif UNITY_2019_1_OR_NEWER
		public BussStylePropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussStylePropertyVisualElement/BussStylePropertyVisualElement.uss") { }
#endif

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

		public override void Refresh()
		{
			base.Refresh();

			VisualElement buttonContainer = new VisualElement();
			buttonContainer.name = "removeButtonContainer";

			_removeButton = new VisualElement();
			_removeButton.name = "removeButton";
			buttonContainer.Add(_removeButton);
			Root.Add(buttonContainer);

			_removeButton.RegisterCallback<MouseDownEvent>(OnRemoveButtonClicked);
			buttonContainer.SetHidden(!_styleRule.EditMode);

			_labelComponent = new TextElement();
			_labelComponent.name = "propertyLabel";
			Root.Add(_labelComponent);

			_valueParent = new VisualElement();
			_valueParent.name = "value";
			Root.Add(_valueParent);

			_variableParent = new VisualElement();
			_variableParent.name = "globalVariable";
			Root.Add(_variableParent);

			Update();
		}

		// TODO: should be a part of base class and overriden here, TBD later
		private void Update()
		{
			_labelComponent.text = _propertyProvider.Key;

			SetupEditableField();
			SetupVariableConnection();
		}

		public void Setup(BussStyleSheet styleSheet,
		                  BussStyleRule styleRule,
		                  BussPropertyProvider property,
		                  VariableDatabase variableDatabase)
		{
			RemoveStyleSheetListener();

			_variableDatabase = variableDatabase;
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_propertyProvider = property;

			Refresh();
			AddStyleSheetListener();
		}

		private void SetupEditableField()
		{
			var baseType = BussStyle.GetBaseType(_propertyProvider.Key);
			if (_propertyVisualElement != null)
			{
				if (_propertyVisualElement.BaseProperty ==
				    _propertyProvider.GetProperty().GetEndProperty(_variableDatabase, _styleRule))
				{
					return;
				}

				_propertyVisualElement.RemoveFromHierarchy();
				_propertyVisualElement.Destroy();
			}

			_propertyVisualElement = _propertyProvider.GetVisualElement(_variableDatabase, _styleRule, baseType);

			if (_propertyVisualElement != null)
			{
				_propertyVisualElement.UpdatedStyleSheet = _styleSheet;
				_valueParent.Add(_propertyVisualElement);
				_propertyVisualElement.Refresh();
			}
		}

		protected override void OnDestroy()
		{
			_removeButton?.UnregisterCallback<MouseDownEvent>(OnRemoveButtonClicked);
			RemoveStyleSheetListener();
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

			_variableConnection.OnConnectionChange -= Update;
			_variableConnection.Setup(_styleSheet, _propertyProvider, _variableDatabase);
			_variableConnection.OnConnectionChange += Update;
		}

		private void AddStyleSheetListener()
		{
			if (_styleSheet != null)
			{
				_styleSheet.Change += Update;
			}
		}

		private void RemoveStyleSheetListener()
		{
			if (_styleSheet != null)
			{
				_styleSheet.Change -= Update;
			}
		}
	}
}
