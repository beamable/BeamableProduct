using TestingTool.Scripts.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#pragma warning disable CS0649

namespace TestingTool.Scripts
{
    public class TestScenarioButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI testName;
        [SerializeField] private TextMeshProUGUI shortDescription;
        [SerializeField] private Image statusImage;

        private TestScenarioRuntime _testScenario;

        public void Init(TestScenarioRuntime testScenario, UnityAction sceneButtonAction)
        {
            _testScenario = testScenario;
            GetComponent<Button>().onClick.AddListener(sceneButtonAction);
            UpdateData();
            OnEnable();
        }
        private void OnEnable()
        {
            if (_testScenario != null) _testScenario.OnValueChanged += UpdateData;
        }
        private void OnDisable()
        {
            if (_testScenario != null) _testScenario.OnValueChanged -= UpdateData;
        }
        public void UpdateData()
        {
            testName.SetText(_testScenario.SceneName);
            shortDescription.SetText(_testScenario.ShortDescription);
            statusImage.color = ColorHelper.GetColor(_testScenario.Progress);
        }
    }
}
