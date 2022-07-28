using Beamable.NewTestingTool.Core.Models;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class RegisteredTestSceneVisualElement : TestingToolComponent
	{
		public RegisteredTestScene RegisteredTestScene { get; set; }
		
		private Label _sceneName;
		
		public new class UxmlFactory : UxmlFactory<RegisteredTestSceneVisualElement, UxmlTraits> { }

		public RegisteredTestSceneVisualElement() : base(nameof(RegisteredTestSceneVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();
			_sceneName = Root.Q<Label>("sceneName");
			_sceneName.text = RegisteredTestScene.SceneName;
		}
	}
}
