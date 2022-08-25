using Beamable.NewTestingTool.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beamable.NewTestingTool.UI
{
	public class TestControllerUI : MonoBehaviour
	{
		private TestConfiguration TestConfiguration => _testController.TestConfiguration;

		[SerializeField] private TestTopBarView _topBarView;
		[SerializeField] private TestBottomBarView _bottomBarView;
		[SerializeField] private TestController _testController;

		private bool _isTestUIEnabled = true;
		
		private void Update()
		{
			if (Input.GetKeyDown(TestConfiguration.ToggleTestUIKey))
				ToggleTestUI();
		}

		private void ToggleTestUI()
		{
			_isTestUIEnabled = !_isTestUIEnabled;
			
			_topBarView.gameObject.SetActive(_isTestUIEnabled);
			_bottomBarView.gameObject.SetActive(_isTestUIEnabled);
		}

		private void OnEnable()
		{
			TestConfiguration.OnAllTestsFinished += BackToMainMenu;
		}
		private void OnDisable()
		{
			TestConfiguration.OnAllTestsFinished -= BackToMainMenu;
		}
		public void BackToMainMenu() => SceneManager.LoadScene(0);
	}
}
