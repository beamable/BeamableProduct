using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
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
	public class BussSelectorLabelVisualElement : BeamableBasicVisualElement
	{
		private TextField _editableLabel;
		
#if UNITY_2018
		public BussSelectorLabelVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussStyleCardVisualElement/BussSelectorLabelVisualElement/BussSelectorLabelVisualElement.2018.uss") { }
#elif UNITY_2019_1_OR_NEWER
		public BussSelectorLabelVisualElement(string ussPath) : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussStyleCardVisualElement/BussSelectorLabelVisualElement/BussSelectorLabelVisualElement.uss") { }
#endif

		public void Setup(BussStyleCardVisualElement.MODE currentMode, BussStyleRule styleRule)
		{
			base.Refresh();
			
			switch (currentMode)
			{
				case BussStyleCardVisualElement.MODE.NORMAL:
					TextElement textLabel = new TextElement();
					textLabel.name = "styleId";
					textLabel.text = styleRule.SelectorString;
					Root.Add(textLabel);
					break;
				case BussStyleCardVisualElement.MODE.EDIT:
					_editableLabel = new TextField();
					_editableLabel.name = "styleId";
					_editableLabel.value = styleRule.SelectorString;
					_editableLabel.RegisterValueChangedCallback(StyleIdChanged);
					Root.Add(_editableLabel);
					break;
			}
		}

		protected override void OnDestroy()
		{
			_editableLabel.UnregisterValueChangedCallback(StyleIdChanged);
		}

		private void StyleIdChanged(ChangeEvent<string> evt)
		{
			
		}
	}
}
