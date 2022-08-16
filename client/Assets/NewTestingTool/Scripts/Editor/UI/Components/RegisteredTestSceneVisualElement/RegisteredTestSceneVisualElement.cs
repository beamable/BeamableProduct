using Beamable.NewTestingTool.Core.Models;
using NewTestingTool.Helpers;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

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
			TestHelper.SetTestResult(_testResult, RegisteredTestScene.TestResult);
		}
	}
}
