using Beamable.BSAT.Core.Models;
using Beamable.BSAT.Editor.UI.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Beamable.BSAT.Editor.Models.Lists
{
	internal class RegisteredTestSceneListModel : TestListModelBase<RegisteredTestSceneVisualElement, RegisteredTestScene>
	{
		public RegisteredTestSceneListModel(TestingEditorModel testingEditorModel) : base(testingEditorModel) { }

		protected override RegisteredTestSceneVisualElement BindListViewElementUtil(VisualElement elem, int index)
		{
			var registeredTestSceneVisualElement = (RegisteredTestSceneVisualElement)elem;
			TestingEditorModel.SelectedRegisteredTestScene =
				TestingEditorModel.TestConfiguration.RegisteredTestScenes[index];
			registeredTestSceneVisualElement.RegisteredTestScene = TestingEditorModel.SelectedRegisteredTestScene;
			return registeredTestSceneVisualElement;
		}

		protected override void ListView_OnItemChosen(object obj)
		{
			if (obj == null)
				return;
			var registeredTestScene = (RegisteredTestScene)obj;
			TestingEditorModel.SelectedRegisteredTestScene =
				TestingEditorModel.TestConfiguration.RegisteredTestScenes.First(x => x == registeredTestScene);
		}

		protected override void ListView_OnSelectionChanged(IEnumerable<object> objs)
		{
			var registeredTestScene = (RegisteredTestScene)objs.First();
			TestingEditorModel.SelectedRegisteredTestScene =
				TestingEditorModel.TestConfiguration.RegisteredTestScenes.First(x => x == registeredTestScene);
			OnSelectionChanged?.Invoke(registeredTestScene);
		}
	}
}
