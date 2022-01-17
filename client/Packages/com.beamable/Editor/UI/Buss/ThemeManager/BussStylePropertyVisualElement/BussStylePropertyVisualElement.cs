using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Validation;
using Beamable.UI.Buss;
using Editor.UI.BUSS.ThemeManager;
using Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
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
		public new class UxmlFactory : UxmlFactory<BussStylePropertyVisualElement, UxmlTraits> { }

#if UNITY_2018
		public BussStylePropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussStylePropertyVisualElement/BussStylePropertyVisualElement.2018.uss") { }
#elif UNITY_2019_1_OR_NEWER
		public BussStylePropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussStylePropertyVisualElement/BussStylePropertyVisualElement.uss") { }
#endif

		private VariableDatabase _variableDatabase;
		private BussStyleSheet _styleSheet;
		private BussPropertyProvider _propertyProvider;
		
		private VisualElement _valueParent;
		private VisualElement _variableParent;
		private BussPropertyVisualElement _propertyVisualElement;
		private VariableConnectionVisualElement _variableConnection;
		private Label _labelComponent;

		public override void Refresh()
		{
			base.Refresh();

			_valueParent = Root.Q<VisualElement>("value");
			_variableParent = Root.Q<VisualElement>("globalVariable");

			_labelComponent = Root.Q<Label>("label");
			Update();
		}

		private void Update() 
		{
			_labelComponent.text = _propertyProvider.Key;

			SetupEditableField();
			SetupVariableConnection();
		}

		public void Setup(BussStyleSheet styleSheet, BussPropertyProvider property, VariableDatabase variableDatabase)
		{
			RemoveStyleSheetListener();
			_variableDatabase = variableDatabase;
			_styleSheet = styleSheet;
			_propertyProvider = property;
			Refresh();
			AddStyleSheetListener();
		}

		private void SetupEditableField()
		{
			var baseType = BussStyle.GetBaseType(_propertyProvider.Key);
			if (_propertyVisualElement != null)
			{
				if (_propertyVisualElement.BaseProperty == _propertyProvider.GetProperty().GetEndProperty(_variableDatabase))
				{
					return;
				}
				
				_propertyVisualElement.RemoveFromHierarchy();
				_propertyVisualElement.Destroy();
			}
			_propertyVisualElement = _propertyProvider.GetVisualElement(_variableDatabase, baseType);
			
			if (_propertyVisualElement != null)
			{
				_propertyVisualElement.UpdatedStyleSheet = _styleSheet;
				_valueParent.Add(_propertyVisualElement);
				_propertyVisualElement.Refresh();
			}
		}

		private void SetupVariableConnection()
		{
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
				_styleSheet.OnChange += Update;
			}
		}

		private void RemoveStyleSheetListener()
		{
			if (_styleSheet != null)
			{
				_styleSheet.OnChange -= Update;
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			RemoveStyleSheetListener();
		}
	}
}
