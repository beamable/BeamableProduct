using Beamable.NewTestingTool.Core.Models;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class RegisteredTestRuleVisualElement : TestingToolComponent
	{
		public RegisteredTestRule RegisteredTestRule { get; set; }
		
		private Label _ruleName;
		
		public new class UxmlFactory : UxmlFactory<RegisteredTestRuleVisualElement, UxmlTraits> { }

		public RegisteredTestRuleVisualElement() : base(nameof(RegisteredTestRuleVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();
			_ruleName = Root.Q<Label>("ruleName");
			_ruleName.text = RegisteredTestRule.TestMethodName;
		}
	}
}
