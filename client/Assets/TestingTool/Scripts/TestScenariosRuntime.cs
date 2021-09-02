using System;
using System.Collections.Generic;
using TestingTool.Scripts.Attributes;
using UnityEngine;

namespace TestingTool.Scripts
{
    public enum ProgressStatus
    {
        NotSet = 0,
        NotPassed = 1,
        Pending = 2,
        Passed = 3
    }
    public abstract class CustomSetterBase
    {
        public event Action OnValueChanged;

        protected void Set<T>(ref T field, T value)
        {
            field = value;
            OnValueChanged?.Invoke();
        }
    }
    [Serializable]
    public class TestStepRuntime : CustomSetterBase
    {
        public ProgressStatus Progress
        {
            get => progress;
            set => Set(ref progress, value);
        }
        public string Description => description;

        [SerializeField, StatusVerifier]
        private ProgressStatus progress;
        [SerializeField]
        private string description;

        public TestStepRuntime(string description)
        {
            progress = ProgressStatus.NotSet;
            this.description = description;
        }
    }
    [Serializable]
    public class TestScenarioRuntime : CustomSetterBase
    {
        public string SceneName => sceneName;
        public ProgressStatus Progress
        {
            get => progress;
            set => Set(ref progress, value);
        }
        public string ShortDescription => shortDescription;
        public string FullDescription => fullDescription;
        public List<TestStepRuntime> TestSteps => testSteps;

        [SerializeField]
        private string sceneName;
        [SerializeField, StatusVerifier]
        private ProgressStatus progress;
        [SerializeField]
        private string shortDescription;
        [SerializeField]
        private string fullDescription;
        [SerializeField]
        private List<TestStepRuntime> testSteps;

        public TestScenarioRuntime(string sceneName, string shortDescription, string fullDescription, List<TestStepRuntime> testSteps)
        {
            this.sceneName = sceneName;
            progress = ProgressStatus.NotSet;
            this.shortDescription = shortDescription;
            this.fullDescription = fullDescription;
            this.testSteps = testSteps;
        }
    }
    [Serializable]
    public class TestScenariosRuntime : ScriptableObject
    {
        public TestScenarioRuntime CurrentScenario { get; set; }
        public List<TestScenarioRuntime> Scenarios
        {
            get => scenarios;
            set => Set(ref scenarios, value);
        }
        
        public event Action OnDataChanged;

        [SerializeField]
        private List<TestScenarioRuntime> scenarios = new List<TestScenarioRuntime>();

        public void OnValidate()
        {
            OnDataChanged?.Invoke();
        }
        protected void Set<T>(ref T field, T value)
        {
            field = value;
            OnDataChanged?.Invoke();
        }
        public void ResetForBuild()
        {
            CurrentScenario = null;
            foreach (var scenario in scenarios)
            {
                scenario.Progress = ProgressStatus.NotSet;
                foreach (var testStep in scenario.TestSteps)
                {
                    testStep.Progress = ProgressStatus.NotSet;
                }
            }
        }
    }
}