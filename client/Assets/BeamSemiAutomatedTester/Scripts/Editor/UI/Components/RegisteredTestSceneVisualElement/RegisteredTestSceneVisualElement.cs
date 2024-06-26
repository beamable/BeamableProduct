using Beamable.BSAT.Core.Models;
using Beamable.BSAT;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

using static Beamable.BSAT.Constants.TestConstants.Paths;

namespace Beamable.BSAT.Editor.UI.Components
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
