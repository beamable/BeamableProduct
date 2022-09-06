using Beamable.BSAT.Core;
using Beamable.BSAT.Core.Models;
using Beamable.BSAT;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Beamable.BSAT.Editor.UI.Components
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
			
			RegisteredTestRule.OnTestResultChanged -= HandleTestResultChange;
			RegisteredTestRule.OnTestResultChanged += HandleTestResultChange;
			TestHelper.SetTestResult(_testResult, RegisteredTestRule?.TestResult ?? TestResult.NotSet);
		}
		
		private void HandleTestResultChange() 
			=> TestHelper.SetTestResult(_testResult, RegisteredTestRule.TestResult);
	}
}
