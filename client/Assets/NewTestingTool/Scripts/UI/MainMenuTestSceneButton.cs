using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Core.Models.Descriptors;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.NewTestingTool.UI
{
    public class MainMenuTestSceneButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI testNameText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image statusImage;

        private RegisteredTestScene _registeredTestScene;
        private TestDescriptor _testDescriptor;
        
        // TODO - Temp check. Remove later
        private Dictionary<TestResult, Color> _kek = new Dictionary<TestResult, Color>()
        {
	        {TestResult.NotSet, Color.yellow},
	        {TestResult.Passed, Color.green},
	        {TestResult.Failed, Color.red},
        };

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
            statusImage.color = _kek[_registeredTestScene.TestResult];
        }
    }
}
