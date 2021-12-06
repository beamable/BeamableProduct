using Beamable.Editor.UI.Buss;
using Beamable.UI.Buss;
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
	public class BussStyleCardVisualElement : BeamableVisualElement
	{
		public new class UxmlFactory : UxmlFactory<BussStyleCardVisualElement, UxmlTraits> { }

		public BussStyleCardVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStyleCardVisualElement)}/{nameof(BussStyleCardVisualElement)}") { }

		private BussStyleRule _styleRule;
		private TextElement _styleId;
		private VisualElement _properties;

		public override void Refresh()
		{
			base.Refresh();

			_styleId = Root.Q<TextElement>("styleId");
			_styleId.text = _styleRule.SelectorString;

			_properties = Root.Q<VisualElement>("properties");

			CreateProperties();
		}

		public void Setup(BussStyleRule styleRule)
		{
			_styleRule = styleRule;
			Refresh();
		}

		private void CreateProperties()
		{
			foreach (BussPropertyProvider property in _styleRule.Properties)
			{
				BussStylePropertyVisualElement element = new BussStylePropertyVisualElement();
				element.Setup(property);
				_properties.Add(element);
			}
		}
	}
}
