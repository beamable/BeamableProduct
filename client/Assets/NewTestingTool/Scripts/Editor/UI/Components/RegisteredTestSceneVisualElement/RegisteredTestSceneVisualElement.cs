using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Helpers;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

using static Beamable.NewTestingTool.Constants.TestConstants.Paths;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class RegisteredTestSceneVisualElement : TestingToolComponent
	{
		public RegisteredTestScene RegisteredTestScene { get; set; }
		
		private Label _sceneName;
		private VisualElement _testResult;
		
		public RegisteredTestSceneVisualElement() : base(nameof(RegisteredTestSceneVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();
			_sceneName = Root.Q<Label>("sceneName");
			_sceneName.text = RegisteredTestScene.SceneName;
			
			_testResult = Root.Q("testResult");
			
			RegisterCallback<MouseDownEvent>(HandleMouseDownEvent);
			
			RegisteredTestScene.OnTestResultChanged -= HandleTestResultChange;
			RegisteredTestScene.OnTestResultChanged += HandleTestResultChange;
			TestHelper.SetTestResult(_testResult, RegisteredTestScene.TestResult);
		}
		private void HandleMouseDownEvent(MouseDownEvent mde)
		{
			if (mde.clickCount > 1)
				EditorSceneManager.OpenScene(GetPathToTestScene(RegisteredTestScene.SceneName));
		}
		private void HandleTestResultChange() 
			=> TestHelper.SetTestResult(_testResult, RegisteredTestScene.TestResult);
	}
}
