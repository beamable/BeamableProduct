using Beamable.NewTestingTool.Core.Models;
using NewTestingTool.Helpers;
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
			TestHelper.SetTestResult(_testResult, RegisteredTest.TestResult);
		}
	}
}
