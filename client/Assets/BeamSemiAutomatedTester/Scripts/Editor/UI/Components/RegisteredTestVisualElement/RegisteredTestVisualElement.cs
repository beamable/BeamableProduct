using Beamable.BSAT.Core;
using Beamable.BSAT.Core.Models;
using Beamable.BSAT;
using UnityEngine.UIElements;

namespace Beamable.BSAT.Editor.UI.Components
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
