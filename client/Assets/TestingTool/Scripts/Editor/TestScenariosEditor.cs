using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.BeamableConstants.Features.TestingTool;

namespace TestingTool.Scripts.Editor
{
    [CustomEditor(typeof(TestScenarios))]
    public class TestScenariosEditor : UnityEditor.Editor
    {
        private List<EditorTestScenario> _editorTestScenarios = new List<EditorTestScenario>();
        private TestScenarios _testScenarios;
        
        public override void OnInspectorGUI()
        {
            _testScenarios = target as TestScenarios;
            EditorUtility.SetDirty(_testScenarios);
            
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
                    _editorTestScenarios.Add(new EditorTestScenario(_testScenarios.Scenarios[i], false, false));
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
                editorData.FoldoutState = EditorGUILayout.Foldout(editorData.FoldoutState, scenario.FindPropertyRelative("name").stringValue);
                GUILayout.Space(16);
                editorData.ToggleState = EditorGUILayout.Toggle(editorData.ToggleState, GUILayout.Width(30));
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
            EditorGUILayout.PropertyField(scenario.FindPropertyRelative("sceneAsset"));
            EditorGUILayout.PropertyField(scenario.FindPropertyRelative("shortDescription"));
            EditorGUILayout.PropertyField(scenario.FindPropertyRelative("fullDescription"));
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
                    _editorTestScenarios[scenarioIndex].EditorTestSteps.Add(new EditorTestStep(false, false));
                }
            }

            for (var testStepIndex = 0; testStepIndex < testSteps.arraySize; testStepIndex++)
            {
                var testStep = testSteps.GetArrayElementAtIndex(testStepIndex);
                var editorData = _editorTestScenarios[scenarioIndex].EditorTestSteps[testStepIndex];
      
                EditorGUILayout.BeginHorizontal();
                editorData.FoldoutState = EditorGUILayout.Foldout(editorData.FoldoutState, $"Test Step - {testStepIndex + 1}");
                editorData.ToggleState = EditorGUILayout.Toggle(editorData.ToggleState, GUILayout.Width(30));
                GUILayout.Space(20);
                EditorGUILayout.EndHorizontal();
                
                if (!editorData.FoldoutState)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(testStep.FindPropertyRelative("description"));
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create new step")) CreateTestStep(scenarioIndex);
            if (GUILayout.Button("Delete selected steps")) DeleteSelectedTestSteps(scenarioIndex);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        private void ShowControlButtons()
        {
            if (GUILayout.Button("Refresh window")) _testScenarios.Refresh();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select all")) ChangeAllToggleStates(true);
            if (GUILayout.Button("Deselect all")) ChangeAllToggleStates(false);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand all")) ChangeAllFoldoutStates(true);
            if (GUILayout.Button("Collapse all")) ChangeAllFoldoutStates(false);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create new test")) CreateNewTest();
            if (GUILayout.Button("Create empty test")) CreateEmptyTest();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete selected")) DeleteSelectedTests();
            if (GUILayout.Button("Delete empty scenarios")) DeleteEmptyTests();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sort scenarios")) SortAllTests();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Setup test build settings")) SetupTestBuildSettings();
        }
        
        private void CreateNewTest()
        {
            var parsedSceneNumbers = new List<int>();
            var scenes = Directory.GetFiles(Directories.TEST_TOOL_SCENES_PATH).Where(x => x.EndsWith(".unity")).ToList();

            foreach (var scene in scenes)
            {
                var sceneNumber = Path.GetFileName(scene).Substring(0, 3);
                if (!Int32.TryParse(sceneNumber, out var parsedSceneNumber))
                {
                    continue;
                }
                parsedSceneNumbers.Add(parsedSceneNumber);
            }
            parsedSceneNumbers.Sort();

            var fileNumber = 1;
            for (var i = 0; i < parsedSceneNumbers.Count; i++)
            {
                if (parsedSceneNumbers[i] != i+1)
                {
                    fileNumber = i+1;
                    break;
                }
                fileNumber += 1;
            }

            var newFileName = $"{fileNumber:000} - {FileNames.NEW_SCENE}";
            if (!AssetDatabase.CopyAsset(
	                Directories.TEMPLATE_SCENE_PATH,
	                Directories.TEST_SCENE_DATA_PATH(newFileName)))
            {
                Debug.LogError("Failed while copying a file.");
                return;
            }
            
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Directories.TEST_SCENE_DATA_PATH(newFileName));
            CreateTest(sceneAsset);
        }
        private void CreateEmptyTest()
        {
            CreateTest(null);
        }
        private void CreateTest(SceneAsset sceneAsset)
        {
            ChangeAllFoldoutStates(false);
            var newTestScenario = new TestScenario(sceneAsset);
            _testScenarios.Scenarios.Add(newTestScenario);
            _editorTestScenarios.Add(new EditorTestScenario(newTestScenario, true, false));
            _testScenarios.OnValidate();
        }
        private void CreateTestStep(int scenarioIndex)
        {
            _testScenarios.Scenarios[scenarioIndex].TestSteps.Add(new TestStep());
            _editorTestScenarios[scenarioIndex].EditorTestSteps.Add(new EditorTestStep(true, false));
            _testScenarios.OnValidate();
        }
        private void DeleteSelectedTestSteps(int scenarioIndex)
        {
            for (var i = _editorTestScenarios[scenarioIndex].EditorTestSteps.Count - 1; i >= 0; i--)
            {
                if ( _editorTestScenarios[scenarioIndex].EditorTestSteps[i].ToggleState)
                {
                    _testScenarios.Scenarios[scenarioIndex].TestSteps.RemoveAt(i);
                    _editorTestScenarios[scenarioIndex].EditorTestSteps.RemoveAt(i);
                }
            }
            _testScenarios.OnValidate();
        }
        private void ChangeAllToggleStates(bool isEnabled)
        {
            foreach (var editorScenario in _editorTestScenarios)
            {
                editorScenario.ToggleState = isEnabled;
            }
        }
        private void ChangeAllFoldoutStates(bool isEnabled)
        {
            foreach (var editorScenario in _editorTestScenarios)
            {
                editorScenario.FoldoutState = isEnabled;
            }
        }
        private void DeleteSelectedTests()
        {
            for (var i = _editorTestScenarios.Count - 1; i >= 0; i--)
            {
                if (_editorTestScenarios[i].ToggleState)
                {
                    _testScenarios.Scenarios.RemoveAt(i);
                    _editorTestScenarios.RemoveAt(i);
                }
            }
            if (_testScenarios.Scenarios.Count == 0)
            {
                _testScenarios.CurrentScenario = null;
            }
            _testScenarios.OnValidate();
        }
        private void DeleteEmptyTests()
        {
            for (var i =  _testScenarios.Scenarios.Count - 1; i >= 0; i--)
            {
                if (_testScenarios.Scenarios[i].SceneAsset == null)
                {
                    _testScenarios.Scenarios.RemoveAt(i);
                    _editorTestScenarios.RemoveAt(i);
                }
            }
            if (_testScenarios.Scenarios.Count == 0)
            {
                _testScenarios.CurrentScenario = null;
            }
            _testScenarios.OnValidate();
        }
        private void SortAllTests()
        {
            DeleteEmptyTests();
            _testScenarios.Scenarios = _testScenarios.Scenarios.OrderBy(x => x.Name).ToList();
            _editorTestScenarios = _editorTestScenarios.OrderBy(x => x.Scenario.Name).ToList();
            _testScenarios.OnValidate();
        }
        private void SetupTestBuildSettings()
        {
            SortAllTests();
            TestScenariosSettingsBuilder.Execute();
        }
        
        private abstract class EditorListStatesBase
        {
            public bool FoldoutState { get; set; }
            public bool ToggleState { get; set; }
        }
        private class EditorTestScenario : EditorListStatesBase
        {
            public TestScenario Scenario { get; }
            public List<EditorTestStep> EditorTestSteps { get; }

            public EditorTestScenario(TestScenario scenario, bool foldoutState, bool toggleState)
            {
                Scenario = scenario;
                FoldoutState = foldoutState;
                ToggleState = toggleState;
                EditorTestSteps = new List<EditorTestStep>();
            }
        }
        private class EditorTestStep : EditorListStatesBase
        {
            public EditorTestStep(bool foldoutState, bool toggleState)
            {
                FoldoutState = foldoutState;
                ToggleState = toggleState;
            }
        }
    }
}
