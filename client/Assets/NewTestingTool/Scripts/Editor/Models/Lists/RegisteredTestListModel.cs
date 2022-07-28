using Beamable.Editor.NewTestingTool.Models;
using Beamable.Editor.NewTestingTool.UI.Components;
using Beamable.NewTestingTool.Core.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Beamable.Editor.NewTestingTool.Models.Lists
{
	internal class RegisteredTestListModel : TestListModelBase<RegisteredTestVisualElement, RegisteredTest>
	{
		public RegisteredTestListModel(TestingEditorModel testingEditorModel) : base(testingEditorModel) { }
		protected override RegisteredTestVisualElement BindListViewElementUtil(VisualElement elem, int index)
		{
			var registeredTestVisualElement = (RegisteredTestVisualElement)elem;
			TestingEditorModel.SelectedRegisteredTest = TestingEditorModel.SelectedRegisteredTestScene.RegisteredTests[index];
			registeredTestVisualElement.RegisteredTest = TestingEditorModel.SelectedRegisteredTest;
			return registeredTestVisualElement;
		}

		protected override void ListView_OnItemChosen(object obj)
		{
			if (obj == null) 
				return;
			var registeredTest = (RegisteredTest)obj;
			TestingEditorModel.SelectedRegisteredTest = TestingEditorModel.SelectedRegisteredTestScene.RegisteredTests.First(x => x == registeredTest);
			OnItemChosen?.Invoke(registeredTest);
		}
		protected override void ListView_OnSelectionChanged(IEnumerable<object> objs)
		{
			var registeredTest = (RegisteredTest)objs.First();
			TestingEditorModel.SelectedRegisteredTest = TestingEditorModel.SelectedRegisteredTestScene.RegisteredTests.First(x => x == registeredTest);
			OnSelectionChanged?.Invoke(registeredTest);
		}
	}
}
