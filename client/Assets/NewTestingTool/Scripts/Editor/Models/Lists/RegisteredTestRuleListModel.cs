using Beamable.Editor.NewTestingTool.UI.Components;
using Beamable.NewTestingTool.Core.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Beamable.Editor.NewTestingTool.Models.Lists
{
	internal class RegisteredTestRuleListModel : TestListModelBase<RegisteredTestRuleVisualElement, RegisteredTestRule>
	{
		public RegisteredTestRuleListModel(TestingEditorModel testingEditorModel) : base(testingEditorModel) { }

		protected override RegisteredTestRuleVisualElement BindListViewElementUtil(VisualElement elem, int index)
		{
			var registeredTestRuleVisualElement = (RegisteredTestRuleVisualElement)elem;
			TestingEditorModel.SelectedRegisteredTestRule =
				TestingEditorModel.SelectedRegisteredTest.RegisteredTestRules[index];
			registeredTestRuleVisualElement.RegisteredTestRule = TestingEditorModel.SelectedRegisteredTestRule;
			return registeredTestRuleVisualElement;
		}

		protected override void ListView_OnItemChosen(object obj)
		{
			if (obj == null)
				return;
			var registeredTestRule = (RegisteredTestRule)obj;
			TestingEditorModel.SelectedRegisteredTestRule =
				TestingEditorModel.SelectedRegisteredTest.RegisteredTestRules.First(x => x == registeredTestRule);
		}

		protected override void ListView_OnSelectionChanged(IEnumerable<object> objs)
		{
			var registeredTestRule = (RegisteredTestRule)objs.First();
			TestingEditorModel.SelectedRegisteredTestRule =
				TestingEditorModel.SelectedRegisteredTest.RegisteredTestRules.First(x => x == registeredTestRule);
			OnSelectionChanged?.Invoke(registeredTestRule);
		}
	}
}
