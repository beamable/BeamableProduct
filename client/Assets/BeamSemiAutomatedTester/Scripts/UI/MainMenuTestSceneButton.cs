using Beamable.BSAT.Core.Models;
using Beamable.BSAT.Core.Models.Descriptors;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.BSAT.UI
{
    public class MainMenuTestSceneButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI testNameText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image statusImage;

        private RegisteredTestScene _registeredTestScene;
        private TestDescriptor _testDescriptor;

        public void Init(RegisteredTestScene registeredTestScene, Action<RegisteredTestScene> onLoadTestButtonPressed)
        {
	        _registeredTestScene = registeredTestScene;
	        _testDescriptor = registeredTestScene.TestSceneDescriptor.GetTestDescriptor();
	        GetComponent<Button>().onClick.AddListener(() => onLoadTestButtonPressed?.Invoke(registeredTestScene));
            UpdateData();
        }
        private void UpdateData()
        {
            testNameText.SetText(_registeredTestScene.SceneName);
            titleText.SetText(_testDescriptor.Title);
            descriptionText.SetText(_testDescriptor.Description);
            statusImage.color = TestHelper.ConvertTestResultToColor(_registeredTestScene.TestResult);
        }
    }
}
