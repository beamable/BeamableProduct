using Beamable.NewTestingTool.Core.Models;
using NewTestingTool.Helpers;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class RegisteredTestRuleVisualElement : TestingToolComponent
	{
		public RegisteredTestRule RegisteredTestRule { get; set; }
		
		private Label _ruleName;
		private VisualElement _testResult;
		
		public RegisteredTestRuleVisualElement() : base(nameof(RegisteredTestRuleVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();
			_ruleName = Root.Q<Label>("ruleName");
			_ruleName.text = RegisteredTestRule.TestMethodName;

			_testResult = Root.Q("testResult");
			TestHelper.SetTestResult(_testResult, RegisteredTestRule.TestResult);
		}
	}
}
