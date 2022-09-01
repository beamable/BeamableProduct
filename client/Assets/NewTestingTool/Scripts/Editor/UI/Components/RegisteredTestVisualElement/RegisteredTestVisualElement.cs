using Beamable.NewTestingTool.Core;
using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Helpers;
using UnityEngine.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class RegisteredTestVisualElement : TestingToolComponent
	{
		public RegisteredTest RegisteredTest { get; set; }
		
		private Label _testableName;
		private VisualElement _testResult;
		
		public RegisteredTestVisualElement() : base(nameof(RegisteredTestVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();
			_testableName = Root.Q<Label>("testableName");
			_testableName.text = RegisteredTest.TestClassName;
			_testResult = Root.Q("testResult");
			
			RegisteredTest.OnTestResultChanged -= HandleTestResultChange;
			RegisteredTest.OnTestResultChanged += HandleTestResultChange;
			TestHelper.SetTestResult(_testResult, RegisteredTest?.TestResult ?? TestResult.NotSet);
		}
		
		private void HandleTestResultChange() 
			=> TestHelper.SetTestResult(_testResult, RegisteredTest.TestResult);
	}
}
