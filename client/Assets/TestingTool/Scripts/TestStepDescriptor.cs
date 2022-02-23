using System.Collections.Generic;
using System.Linq;
using TestingTool.Scripts.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Beamable.Common.Constants.Features.TestingTool;

#pragma warning disable CS0649

namespace TestingTool.Scripts
{
    public class TestStepDescriptor : MonoBehaviour
    {
        public TestStepRuntime CurrentTestStep => _testSteps[_currentStepIndex];

        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image progressImage;
        [SerializeField] private TextMeshProUGUI indexText;
        [Space]
        [SerializeField] private GameObject descriptionBackground;
        [SerializeField] private GameObject previousStep, nextStep;

        private TestScenariosRuntime _testScenarios;
        private List<TestStepRuntime> _testSteps;
        private int _currentStepIndex = 0;

        private void Awake()
        {
            _testScenarios = Resources.Load<TestScenariosRuntime>(FileNames.TEST_SCENARIOS_RUNTIME);
            Init();
        }
        private void Init()
        {
            _testSteps = _testScenarios.CurrentScenario.TestSteps;
            if (_testSteps.Count == 0)
            {
                HideTestStepUIComponents();
                return;
            }
            UpdateData();
        }
        private void OnEnable()
        {
            if (_testSteps == null || _testSteps.Count == 0)
            {
                return;
            }
            foreach (var testStep in _testSteps)
            {
                testStep.OnValueChanged += UpdateData;
            }
            _testScenarios.OnDataChanged += UpdateData;
        }
        private void OnDisable()
        {
            if (_testSteps == null || _testSteps.Count == 0)
            {
                return;
            }
            foreach (var testStep in _testSteps)
            {
                testStep.OnValueChanged -= UpdateData;
            }
            _testScenarios.OnDataChanged -= UpdateData;
        }
        private void HideTestStepUIComponents()
        {
            descriptionBackground.SetActive(false);
            previousStep.SetActive(false);
            nextStep.SetActive(false);
        }

        public void PreviousStep()
        {
            if (_currentStepIndex - 1 < 0)
            {
                return;
            }
            _currentStepIndex--;
            UpdateData();
        }
        public void NextStep()
        {
            if (_currentStepIndex + 1 >= _testSteps.Count)
            {
                return;
            }
            _currentStepIndex++;
            UpdateData();
        }
        public void SetPassedProgress()
        {
            if (_testSteps.Count == 0)
            {
                _testScenarios.CurrentScenario.Progress = ProgressStatus.Passed;
                return;
            }
            SetState(ProgressStatus.Passed);
        }
        public void SetFailedProgress()
        {
            if (_testSteps.Count == 0)
            {
                _testScenarios.CurrentScenario.Progress = ProgressStatus.NotPassed;
                return;
            }
            SetState(ProgressStatus.NotPassed);
        }

        private void SetState(ProgressStatus progress)
        {
            CurrentTestStep.Progress = progress;

            if (_testSteps.All(x => x.Progress == ProgressStatus.Passed))
            {
                _testScenarios.CurrentScenario.Progress = ProgressStatus.Passed;
            }
            else if (_testSteps.Any(x => x.Progress == ProgressStatus.NotPassed))
            {
                _testScenarios.CurrentScenario.Progress = ProgressStatus.NotPassed;
            }
            else
            {
                _testScenarios.CurrentScenario.Progress = ProgressStatus.Pending;
            }
        }
        private void UpdateData()
        {
            if (CurrentTestStep.Progress == ProgressStatus.NotSet)
            {
                CurrentTestStep.Progress = ProgressStatus.Pending;
            }
            descriptionText.SetText(CurrentTestStep.Description);
            progressImage.color = ColorHelper.GetColor(CurrentTestStep.Progress);
            indexText.SetText($"{_currentStepIndex + 1}/{_testSteps.Count}");
        }
    }
}
