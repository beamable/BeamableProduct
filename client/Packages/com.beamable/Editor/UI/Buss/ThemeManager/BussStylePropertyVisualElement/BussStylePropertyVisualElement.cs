using Beamable.Editor.UI.Buss;
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
	public class BussStylePropertyVisualElement : ValidableVisualElement<string>
	{
		public new class UxmlFactory : UxmlFactory<BussStylePropertyVisualElement, UxmlTraits> { }

		public BussStylePropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStylePropertyVisualElement)}/{nameof(BussStylePropertyVisualElement)}") { }

		private BussStyleRule _styleRule;
		private BussPropertyProvider _property;
		private VisualElement _valueParent;
		private VisualElement _variableParent;

		public override void Refresh()
		{
			base.Refresh();

			_valueParent = Root.Q<VisualElement>("value");
			_variableParent = Root.Q<VisualElement>("globalVariable");

			Label labelComponent = Root.Q<Label>("label");
			labelComponent.text = _property.Key;

			SetupEditableField(_property);
		}

		public void Setup(BussStyleRule styleRule, BussPropertyProvider property)
		{
			_styleRule = styleRule;
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
