using Beamable.Editor.UI.Components;
using Beamable.BSAT.Core;
using Beamable.BSAT.Core.Models;
using Beamable.BSAT;
using UnityEngine.UIElements;

namespace Beamable.BSAT.Editor.UI.Components
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

		public RegisteredTestRuleMethodVisualElement() : base(nameof(RegisteredTestRuleMethodVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();

			_title = Root.Q<LabeledTextField>("title");
			_title.Setup(TITLE_LABEL, string.Empty, s => UpdateData(s));
			_title.Refresh();
			
			_description = Root.Q<LabeledTextField>("description");
			_description.Setup(DESCRIPTION_LABEL, string.Empty, s => UpdateData(s), isMultiline: true);
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
		private void UpdateData(string value)
		{
			RegisteredTestRuleMethod.Title = _title.Value;
			RegisteredTestRuleMethod.Description = _description.Value;
		}

		private void HandleTestResultChange() 
			=> TestHelper.SetTestResult(_testResult, RegisteredTestRuleMethod.TestResult);
	}
}
