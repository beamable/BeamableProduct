using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Validation;
using Beamable.UI.Buss;
using Beamable.UI.Sdf;
using Beamable.UI.Sdf.MaterialManagement;
using Beamable.UI.Tweening;
using System;
using Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
using UnityEngine;
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

		private BussPropertyProvider _property;
		private VisualElement _valueParent;
		private VisualElement _variableParent;

		public override void Refresh()
		{
			base.Refresh();

			TextElement labelComponent = new TextElement();
			labelComponent.name = "propertyLabel";
			labelComponent.text = _property.Key;
			Root.Add(labelComponent);
			
			_valueParent = new VisualElement();
			_valueParent.name = "value";
			Root.Add(_valueParent);

			_variableParent = new VisualElement();
			_variableParent.name = "globalVariable";
			Root.Add(_variableParent);

			SetupEditableField(_property);
		}

		public void Setup(BussPropertyProvider property)
		{
			_property = property;
			Refresh();
		}

		private void SetupEditableField(BussPropertyProvider property)
		{
			BussPropertyVisualElement visualElement = property.GetVisualElement();
			
			if (visualElement != null)
			{
				_valueParent.Add(visualElement);
				visualElement.Refresh();
			}
			
			VariableConnectionVisualElement variableConnection = new VariableConnectionVisualElement();
			variableConnection.Setup(false);
			_variableParent.Add(variableConnection);
		}
	}
}
