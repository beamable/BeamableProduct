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
#if UNITY_ANDROID || UNITY_IOS
		private readonly int _requiredTouchCount = 3;
		private bool _waitForRelease;
#endif
		private void Update()
		{
#if UNITY_ANDROID || UNITY_IOS
			if (Input.touchCount == _requiredTouchCount)
			{
				if (_waitForRelease)
					return;
				_waitForRelease = true;
				ToggleTestUI();
			}
			else
				_waitForRelease = false;
#else
			if (Input.GetKeyDown(TestConfiguration.ToggleTestUIKey))
				ToggleTestUI();
#endif
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
