using TestingTool.Scripts.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Beamable.Common.Constants.Features.TestingTool;

#pragma warning disable CS0649

namespace TestingTool.Scripts
{
    public class TestSceneNavigator : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI sceneNameText;
        [SerializeField] private Image testProgressImage;

        private TestScenariosRuntime _testScenarios;
        private int _currentBuildIndex;

        private void Awake()
        {
            _testScenarios = Resources.Load<TestScenariosRuntime>(FileNames.TEST_SCENARIOS_RUNTIME);
            Init();
        }
        private void Init()
        {
            _currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            sceneNameText.SetText(_testScenarios.CurrentScenario.SceneName);
            UpdateData();
        }
        private void OnEnable()
        {
            _testScenarios.CurrentScenario.OnValueChanged += UpdateData;
        }
        private void OnDisable()
        {
            _testScenarios.CurrentScenario.OnValueChanged -= UpdateData;
        }

        public void PreviousTest()
        {
            if (_currentBuildIndex - 1 < 1)
            {
                return;
            }
            SetupScene(false);
        }
        public void NextTest()
        {
            if (_currentBuildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
            {
                return;
            }
            SetupScene(true);
        }
        public void BackToMainMenu()
        {
            LoadScene(FileNames.MAIN_TEST_SCENE);
        }

        private void SetupScene(bool isNextScene)
        {
            int index = GetScenarioIndex();
            OnDisable();
            _testScenarios.CurrentScenario = isNextScene ? _testScenarios.Scenarios[index + 1] : _testScenarios.Scenarios[index - 1];
            if (_testScenarios.CurrentScenario.Progress == ProgressStatus.NotSet)
            {
                _testScenarios.CurrentScenario.Progress = ProgressStatus.Pending;
            }
            LoadScene(_testScenarios.CurrentScenario.SceneName);
        }
        private int GetScenarioIndex()
        {
            return _testScenarios.Scenarios.FindIndex(scenario => scenario == _testScenarios.CurrentScenario);
        }
        private void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
        private void UpdateData()
        {
            testProgressImage.color = ColorHelper.GetColor(_testScenarios.CurrentScenario.Progress);
        }
    }
}
