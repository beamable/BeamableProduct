using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
namespace Beamable.Editor.Toolbox.Components
{
	public class NewRuleVisualElement : BeamableVisualElement
	{
		public NewRuleVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_COMPONENTS_PATH}/{nameof(NewRuleVisualElement)}/{nameof(NewRuleVisualElement)}")
		{
		}

		private VisualElement _rulesContainer;

		public override void Refresh()
		{
			base.Refresh();

			var selectStyleSheet = Root.Q<LabeledDropdownVisualElement>("selectStyleSheet");
			selectStyleSheet.Setup(new List<string>() {"Test", "Kek"}, i => Debug.LogWarning(""));
			selectStyleSheet.Refresh();

			_rulesContainer = Root.Q("rulesContainer");
			
			Root.Q<Button>("addNewRuleButton").clickable.clicked += AddNewRule;
		}

		private void AddNewRule()
		{
			var rule = new RuleVisualElement();
			rule.Refresh();

			var propertyValue = rule.Q("propertyValue");
			var property = new ColorButtPropertyVisualElement(new SingleColorBussProperty());
			property.Refresh();

			var button = rule.Q<Button>("propertyRemoveButton");
			button.clickable.clicked += () => _rulesContainer.Remove(rule);

			propertyValue.Add(property);
			_rulesContainer.Add(rule);
		}
	}
}
