using Beamable.BSAT.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.BSAT.UI
{
	public class TestBottomBarView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI titleText;
		[SerializeField] private TextMeshProUGUI descriptionText;
		[SerializeField] private Button passedButton;
		[SerializeField] private Button failedButton;

		[SerializeField] private TestController _testController;

		private void Awake() => ChangeButtonsInteractableState(false);

		private void OnEnable()
		{
			_testController.OnTestReadyToInvoke += HandleTestReadyToInvoke;
			_testController.OnManualTestResultRequested += EnableTestButtons;
			_testController.TestConfiguration.OnTestFinished += DisableTestButtons;
			passedButton.onClick.AddListener(_testController.MarkTestAsPassed);
			failedButton.onClick.AddListener(_testController.MarkTestAsFailed);
		}

		private void OnDisable()
		{
			_testController.OnTestReadyToInvoke -= HandleTestReadyToInvoke;
			_testController.OnManualTestResultRequested -= EnableTestButtons;
			_testController.TestConfiguration.OnTestFinished -= DisableTestButtons;
			passedButton.onClick.RemoveListener(_testController.MarkTestAsPassed);
			failedButton.onClick.RemoveListener(_testController.MarkTestAsFailed);
		}

		private void HandleTestReadyToInvoke()
		{
			titleText.SetText(_testController.CurrentTestRuleMethod.Title);
			descriptionText.SetText(_testController.CurrentTestRuleMethod.Description);
		}

		private void EnableTestButtons() => ChangeButtonsInteractableState(true);
		private void DisableTestButtons() => ChangeButtonsInteractableState(false);

		private void ChangeButtonsInteractableState(bool isEnabled)
		{
			passedButton.interactable = isEnabled;
			failedButton.interactable = isEnabled;
		}
	}
}
