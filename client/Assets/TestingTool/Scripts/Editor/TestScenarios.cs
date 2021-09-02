using System;
using System.Collections.Generic;
using TestingTool.Scripts.Attributes;
using UnityEditor;
using UnityEngine;

namespace TestingTool.Scripts.Editor
{
    [Serializable]
    public class TestStep : CustomSetterBase
    {
        public ProgressStatus Progress
        { 
            get => progress;
            set => Set(ref progress, value);
        }
        public string Description => description;
    
        [SerializeField, StatusVerifier] 
        private ProgressStatus progress = ProgressStatus.NotSet;
        [SerializeField, TextArea(1, 3)] 
        private string description = string.Empty;
    }

    [Serializable]
    public class TestScenario : CustomSetterBase
    {
        public string Name
        {
            get => name;
            set => Set(ref name, value);
        }
        public SceneAsset SceneAsset
        {
            get => sceneAsset;
            set => Set(ref sceneAsset, value);
        }
        public ProgressStatus Progress 
        { 
            get => progress;
            set => Set(ref progress, value);
        }
        public string ShortDescription => shortDescription;
        public string FullDescription => fullDescription;
        public List<TestStep> TestSteps => testSteps;

        [SerializeField, ReadOnly] 
        private string name;
        [SerializeField, StatusVerifier] 
        private ProgressStatus progress = ProgressStatus.NotSet;
        [SerializeField, RequiredField]
        private SceneAsset sceneAsset = null;
        [SerializeField, TextArea(1, 2)] 
        private string shortDescription = string.Empty;
        [SerializeField, TextArea(5, 5)] 
        private string fullDescription = string.Empty;
        [SerializeField]
        private List<TestStep> testSteps = new List<TestStep>();

        public TestScenario(SceneAsset sceneAsset)
        {
            this.sceneAsset = sceneAsset;
            name = sceneAsset == null ? "Scene asset not added" : sceneAsset.name;
        }
    }

    [CreateAssetMenu(fileName = "TestScenariosCreator", menuName = "Testing Tool/Test Scenarios Creator")]
    public class TestScenarios : ScriptableObject
    {
        public TestScenario CurrentScenario { get; set; }
        public List<TestScenario> Scenarios
        {
            get => scenarios;
            set => Set(ref scenarios, value);
        }
    
        public event Action OnDataChanged;
    
        [SerializeField] private List<TestScenario> scenarios;
    
        public void OnValidate()
        {
            Refresh();
            OnDataChanged?.Invoke();
        }
        public void Refresh()
        {
            foreach (var scenario in scenarios)
            {
                scenario.Name = scenario.SceneAsset == null ? "Scene asset not added" : scenario.SceneAsset.name;
            }
        }
        protected void Set<T>(ref T field, T value)
        {
            field = value;
            OnDataChanged?.Invoke();
        }
    }
}