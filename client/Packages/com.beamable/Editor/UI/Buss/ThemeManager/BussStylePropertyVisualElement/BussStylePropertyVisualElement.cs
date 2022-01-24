using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.BUSS.ThemeManager;
using Beamable.Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
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

		public BussStyleSheet StyleSheet => _styleSheet;
		public BussStyleRule StyleRule => _styleRule;
		public BussPropertyProvider PropertyProvider => _propertyProvider;
		public string PropertyKey => PropertyProvider.Key;

		public bool PropertyIsInStyle
		{
			get;
			private set;
		}

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

			Root.parent.EnableInClassList("exists", PropertyIsInStyle);
			Root.parent.EnableInClassList("doesntExists", !PropertyIsInStyle);
			Refresh();
		}

		public override void Refresh()
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
			_variableDatabase = variableDatabase;
			_styleSheet = styleSheet;
			_styleRule = styleRule;
			_propertyProvider = property;
			PropertyIsInStyle = _styleRule.Properties.Contains(_propertyProvider);

			Init();
		}

		private void SetupEditableField()
		{
			var baseType = BussStyle.GetBaseType(_propertyProvider.Key);
			if (_propertyVisualElement != null)
			{
				if (_propertyVisualElement.BaseProperty ==
					_propertyProvider.GetProperty().GetEndProperty(_variableDatabase, _styleRule))
				{
					_propertyVisualElement.OnPropertyChangedExternally();
					return;
				}

				_propertyVisualElement.RemoveFromHierarchy();
				_propertyVisualElement.Destroy();
			}

			_propertyVisualElement = _propertyProvider.GetVisualElement(_variableDatabase, _styleRule, baseType);

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

			_variableConnection.OnConnectionChange -= Refresh;
			_variableConnection.Setup(_styleSheet, _propertyProvider, _variableDatabase);
			_variableConnection.OnConnectionChange += Refresh;
		}
	}
}
