using Beamable.Editor.UI.Components;
using Beamable.NewTestingTool.Core;
using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Helpers;
using UnityEngine.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class RegisteredTestRuleMethodVisualElement : TestingToolComponent
	{
		private const string TITLE_LABEL = "Title";
		private const string DESCRIPTION_LABEL = "Description";
		
		private TestConfiguration TestConfiguration { get; set; }
		private RegisteredTestRuleMethod RegisteredTestRuleMethod { get; set; }
		
		private LabeledTextField _title;
		private LabeledTextField _description;
		private VisualElement _testResult;
		
		public new class UxmlFactory : UxmlFactory<RegisteredTestRuleMethodVisualElement, UxmlTraits> { }

		public RegisteredTestRuleMethodVisualElement() : base(nameof(RegisteredTestRuleMethodVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();

			_title = Root.Q<LabeledTextField>("title");
			_title.Setup(TITLE_LABEL, string.Empty, UpdateData);
			_title.Refresh();
			
			_description = Root.Q<LabeledTextField>("description");
			_description.Setup(DESCRIPTION_LABEL, string.Empty, UpdateData, isMultiline: true);
			_description.Refresh();
			
			Root.Q("methodInvocationsBody").SetEnabled(false);

			_testResult = Root.Q("testResult");
			TestHelper.SetTestResult(_testResult, RegisteredTestRuleMethod?.TestResult ?? TestResult.NotSet);

			SetEnabled(false);
		}

		public void Setup(TestConfiguration testConfiguration, RegisteredTestRuleMethod registeredTestRuleMethod)
		{
			TestConfiguration = testConfiguration;
			RegisteredTestRuleMethod = registeredTestRuleMethod;
			
			_title.SetWithoutNotify(registeredTestRuleMethod.Title);
			_description.SetWithoutNotify(registeredTestRuleMethod.Description);
			
			RegisteredTestRuleMethod.OnTestResultChanged -= HandleTestResultChange;
			RegisteredTestRuleMethod.OnTestResultChanged += HandleTestResultChange;
			TestHelper.SetTestResult(_testResult, registeredTestRuleMethod.TestResult);
			
			SetEnabled(true);
		}
		public void ClearData()
		{
			if (RegisteredTestRuleMethod == null)
				return;

			_title.SetWithoutNotify(string.Empty);
			_description.SetWithoutNotify(string.Empty);

			RegisteredTestRuleMethod = null;
			SetEnabled(false);
		}
		private void UpdateData()
		{
			RegisteredTestRuleMethod.Title = _title.Value;
			RegisteredTestRuleMethod.Description = _description.Value;
		}

		private void HandleTestResultChange() 
			=> TestHelper.SetTestResult(_testResult, RegisteredTestRuleMethod.TestResult);
	}
}
