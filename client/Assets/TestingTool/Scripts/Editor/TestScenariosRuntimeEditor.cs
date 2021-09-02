using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TestingTool.Scripts.Editor
{
    [CustomEditor(typeof(TestScenariosRuntime))]
    public class TestScenariosRuntimeEditor : UnityEditor.Editor
    {
        private List<EditorTestScenario> _editorTestScenarios = new List<EditorTestScenario>();
        private TestScenariosRuntime _testScenarios;
        
        public override void OnInspectorGUI()
        {
            _testScenarios = target as TestScenariosRuntime;

            serializedObject.Update();
            Show(serializedObject.FindProperty("scenarios"));
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(20);
            ShowControlButtons();
        }
        
        private void Show(SerializedProperty scenarios)
        {
            if (_editorTestScenarios.Count != scenarios.arraySize)
            {
                for (int i = 0; i < scenarios.arraySize; i++)
                {
                    _editorTestScenarios.Add(new EditorTestScenario(_testScenarios.Scenarios[i], false));
                }
            }
            
            if (_editorTestScenarios.Count(x => x.FoldoutState) > 3)
            {
                ShowControlButtons();
                EditorGUILayout.Space();
            }

            for (var scenarioIndex = 0; scenarioIndex < scenarios.arraySize; scenarioIndex++)
            {
                var scenario = scenarios.GetArrayElementAtIndex(scenarioIndex);
                var editorData = _editorTestScenarios[scenarioIndex];
                
                EditorGUILayout.BeginHorizontal();
                editorData.FoldoutState = EditorGUILayout.Foldout(editorData.FoldoutState, scenario.FindPropertyRelative("sceneName").stringValue);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(scenario.FindPropertyRelative("progress"), GUIContent.none,
                    GUILayout.Width(80));
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Reset", GUILayout.Width(60f))) ResetSpecificTest(scenarioIndex);
                EditorGUILayout.EndHorizontal();
                
                if (!editorData.FoldoutState)
                {
                    continue;
                }

                ShowTestScenarioData(scenario, scenarioIndex);
                EditorGUILayout.Space();
            }
        }
        private void ShowTestScenarioData(SerializedProperty scenario, int scenarioIndex)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(scenario.FindPropertyRelative("sceneName"));
            EditorGUILayout.PropertyField(scenario.FindPropertyRelative("shortDescription"));
            EditorGUILayout.PropertyField(scenario.FindPropertyRelative("fullDescription"));
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel += 1;
            ShowTestStepsData(scenario, scenarioIndex);
            EditorGUI.indentLevel -= 1;
        }
        private void ShowTestStepsData(SerializedProperty scenario, int scenarioIndex)
        {
            var testSteps = scenario.FindPropertyRelative("testSteps");

            if (_editorTestScenarios[scenarioIndex].EditorTestSteps.Count != testSteps.arraySize)
            {
                for (var i = 0; i < testSteps.arraySize; i++)
                {
                    _editorTestScenarios[scenarioIndex].EditorTestSteps.Add(new EditorTestStep(false));
                }
            }

            for (var testStepIndex = 0; testStepIndex < testSteps.arraySize; testStepIndex++)
            {
                var testStep = testSteps.GetArrayElementAtIndex(testStepIndex);
                var editorData = _editorTestScenarios[scenarioIndex].EditorTestSteps[testStepIndex];
      
                EditorGUILayout.BeginHorizontal();
                editorData.FoldoutState = EditorGUILayout.Foldout(editorData.FoldoutState, $"Test Step - {testStepIndex + 1}");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(testStep.FindPropertyRelative("progress"), GUIContent.none, GUILayout.Width(100));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                
                if (!editorData.FoldoutState)
                {
                    continue;
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(testStep.FindPropertyRelative("description"));
                EditorGUI.EndDisabledGroup();
            }
        }
        private void ShowControlButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand all")) ChangeAllFoldoutStates(true);
            if (GUILayout.Button("Collapse all")) ChangeAllFoldoutStates(false);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Reset all tests")) ResetAllTests();
        }
        
        private void ChangeAllFoldoutStates(bool isEnabled)
        {
            foreach (var editorScenario in _editorTestScenarios)
            {
                editorScenario.FoldoutState = isEnabled;
            }
        }
        private void ResetSpecificTest(int scenarioIndex)
        {
            var scenario = _testScenarios.Scenarios[scenarioIndex];

            scenario.Progress = ProgressStatus.NotSet;
            foreach (var testStep in scenario.TestSteps)
            {
                testStep.Progress = ProgressStatus.NotSet;
            }
            _testScenarios.OnValidate();
        }
        private void ResetAllTests()
        {
            if (!EditorUtility.DisplayDialog("Reset all tests?", "Are you sure to reset all tests?", "Yes", "Cancel"))
            {
                return;
            }
            foreach (var scenario in _testScenarios.Scenarios)
            {
                scenario.Progress = ProgressStatus.NotSet;
                foreach (var testStep in scenario.TestSteps)
                {
                    testStep.Progress = ProgressStatus.NotSet;
                }
            }
            _testScenarios.OnValidate();
        }
       
        private abstract class EditorListStatesBase
        {
            public bool FoldoutState { get; set; }
        }
        private class EditorTestScenario : EditorListStatesBase
        {
            public TestScenarioRuntime Scenario { get; }
            public List<EditorTestStep> EditorTestSteps { get; }

            public EditorTestScenario(TestScenarioRuntime scenario, bool foldoutState)
            {
                Scenario = scenario;
                FoldoutState = foldoutState;
                EditorTestSteps = new List<EditorTestStep>();
            }
        }
        private class EditorTestStep : EditorListStatesBase
        {
            public EditorTestStep(bool foldoutState)
            {
                FoldoutState = foldoutState;
            }
        }
    }
}