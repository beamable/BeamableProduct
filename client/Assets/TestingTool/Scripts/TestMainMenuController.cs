using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Beamable.Common.Constants.Features.TestingTool;

#pragma warning disable CS0649

namespace TestingTool.Scripts
{
    public class TestMainMenuController : MonoBehaviour
    {
        [SerializeField] private TestScenariosRuntime testScenarios;
        [Space]
        [SerializeField] private GameObject sceneButton;
        [SerializeField] private Transform scrollViewContent;

        private List<TestScenarioButton> _scenarioButtons;

        private void Awake()
        {
            testScenarios = Resources.Load<TestScenariosRuntime>(FileNames.TEST_SCENARIOS_RUNTIME);
            Init();
        }
        private void OnEnable()
        {
            testScenarios.OnDataChanged += UpdateData;
        }
        private void OnDisable()
        {
            testScenarios.OnDataChanged -= UpdateData;
        }
        private void Init()
        {
            _scenarioButtons = new List<TestScenarioButton>();
            foreach (var scenario in testScenarios.Scenarios)
            {
                _scenarioButtons.Add(SetupButton(scenario));
            }
        }

        private TestScenarioButton SetupButton(TestScenarioRuntime scenario)
        {
            var button = Instantiate(sceneButton, scrollViewContent).GetComponent<TestScenarioButton>();
            button.Init(scenario, delegate { LoadTestScene(scenario); });
            return button;
        }
        private void LoadTestScene(TestScenarioRuntime scenario)
        {
            testScenarios.CurrentScenario = scenario;
            if (scenario.Progress == ProgressStatus.NotSet)
            {
                scenario.Progress = ProgressStatus.Pending;
            }
            SceneManager.LoadScene(scenario.SceneName);
        }
        private void UpdateData()
        {
            foreach (var scenarioButton in _scenarioButtons)
            {
                scenarioButton.UpdateData();
            }
        }
    }
}
