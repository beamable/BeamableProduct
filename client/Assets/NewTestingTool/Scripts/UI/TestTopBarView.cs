using Beamable.NewTestingTool.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.NewTestingTool.UI
{
	public class TestTopBarView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI sceneNameText;
		[SerializeField] private Button backToMainMenuButton;
		[SerializeField] private Button startTestButton;

		[SerializeField] private TestControllerUI _testControllerUI;
		[SerializeField] private TestController _testController;

		private void Start()
		{
			sceneNameText.SetText(_testController.CurrentTestScene.SceneName);
			backToMainMenuButton.onClick.AddListener(() => _testControllerUI.BackToMainMenu());

			if (!_testController.AutomaticallyStart)
			{
				startTestButton.onClick.AddListener(() =>
				{
					_testController.StartTest();
					startTestButton.interactable = false;
				});
			}
			else
			{
				startTestButton.interactable = false;
			}
		}
	}
}
