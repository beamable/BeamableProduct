using Beamable.BSAT.Editor.Models;
using Beamable.BSAT.Core.Models;
using Beamable.BSAT.Editor.UI.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Beamable.BSAT.Editor.Models.Lists
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
		}
		protected override void ListView_OnSelectionChanged(IEnumerable<object> objs)
		{
			var registeredTest = (RegisteredTest)objs.First();
			TestingEditorModel.SelectedRegisteredTest = TestingEditorModel.SelectedRegisteredTestScene.RegisteredTests.First(x => x == registeredTest);
			OnSelectionChanged?.Invoke(registeredTest);
		}
	}
}
