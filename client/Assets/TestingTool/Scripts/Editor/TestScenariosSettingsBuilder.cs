using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using static Beamable.Common.Constants.Features.TestingTool;

namespace TestingTool.Scripts.Editor
{
    public class TestScenariosSettingsBuilder
    {
        private static TestScenarios _testScenarios;

        [MenuItem("Window/Beamable Dev/Setup Test Scenarios Into Build", false, 50)]
        public static void Execute()
        {
            var config = GetConfig();
            if (config == null || !config.IsTestingToolEnabled)
            {
                EditorUtility.DisplayDialog("Testing Tool Status", "Testing Tool is disabled. If you want to use Testing Tool, enable it in TestingToolConfig", "Ok");
                return;
            }

            _testScenarios = AssetDatabase.LoadAssetAtPath<TestScenarios>(Directories.TEST_SCENARIOS_CREATOR_ASSET_PATH);
            Validate();
            SetupScenesInBuildSettings();
            SetupRuntimeTestScenarios();
            EditorUtility.DisplayDialog("Success", "Test scenarios set up correctly!", "Ok");
        }
        private static void Validate()
        {
            if (_testScenarios == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Test scenarios are not set up correctly. Check console for more info.", "Ok");
                throw new NullReferenceException($"Not found scriptable object \"TestScenarios.asset\" at path {Directories.TEST_SCENARIOS_CREATOR_ASSET_PATH}. Make sure the file exists in the given path.");
            }
            _testScenarios.Refresh();
            if (!IsEverySceneAssetUnique())
            {
                EditorUtility.DisplayDialog("Error",
                    "Test scenarios are not set up correctly. Check console for more info.", "Ok");
                throw new Exception("Test scene assets are not unique. Delete duplication or switch scene asset for unique one.");
            }
            if (!IsTestScenariosFileSetupCorrectly())
            {
                EditorUtility.DisplayDialog("Error",
                    "Test scenarios are not set up correctly. Check console for more info.", "Ok");
                throw new NullReferenceException($"Scene asset is not set up correctly in \"TestScenarios.asset\"");
            }
        }
        private static void SetupScenesInBuildSettings()
        {
            var editorBuildSettingsScenes = new List<EditorBuildSettingsScene> { TryGetScene(Directories.MAIN_MENU_SCENE_PATH) };
            foreach (var scenario in _testScenarios.Scenarios)
            {
                editorBuildSettingsScenes.Add(TryGetScene(Directories.TEST_SCENE_DATA_PATH(scenario.Name)));
            }
            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        }
        private static void SetupRuntimeTestScenarios()
        {
            var testScenariosRuntime = Resources.Load<TestScenariosRuntime>(FileNames.TEST_SCENARIOS_RUNTIME);
            if (testScenariosRuntime == null)
            {
                testScenariosRuntime = ScriptableObject.CreateInstance<TestScenariosRuntime>();
                AssetDatabase.CreateAsset(testScenariosRuntime, FileNames.TEST_SCENARIOS_RUNTIME);
            }

            testScenariosRuntime.CurrentScenario = null;
            testScenariosRuntime.Scenarios.Clear();

            foreach (var testScenario in _testScenarios.Scenarios)
            {
                var testStepsRuntime = testScenario.TestSteps.Select(testStep => new TestStepRuntime(testStep.Description)).ToList();
                testScenariosRuntime.Scenarios.Add(new TestScenarioRuntime(testScenario.SceneAsset.name, testScenario.ShortDescription,
                    testScenario.FullDescription, testStepsRuntime));
            }

            EditorUtility.SetDirty(testScenariosRuntime);
            AssetDatabase.SaveAssets();
        }
        private static EditorBuildSettingsScene TryGetScene(string path)
        {
            if (!File.Exists(path))
            {
                throw new NullReferenceException($"Not found given scene at path: {path}. Make sure the file exists in the given path.");
            }
            return new EditorBuildSettingsScene(path, true);
        }
        private static bool IsEverySceneAssetUnique()
        {
            for (var i = 0; i < _testScenarios.Scenarios.Count; i++)
            {
                for (var j = 0; j < _testScenarios.Scenarios.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    if (_testScenarios.Scenarios[i].SceneAsset == _testScenarios.Scenarios[j].SceneAsset)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private static bool IsTestScenariosFileSetupCorrectly()
        {
            return _testScenarios.Scenarios.All(scenario => scenario.SceneAsset != null);
        }
        public static TestingToolConfig GetConfig()
        {
	        var config = AssetDatabase.LoadAssetAtPath<TestingToolConfig>(Directories.CONFIG_ASSET_PATH);
            if (config == null)
            {
                var asset = ScriptableObject.CreateInstance<TestingToolConfig>();
                AssetDatabase.CreateAsset(asset, Directories.CONFIG_ASSET_PATH);
                AssetDatabase.SaveAssets();
                config = asset;
            }
            return config;
        }
    }

    public class TestBuilderProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        private TestScenariosRuntime _testScenariosRuntime;
        private TestingToolConfig _config;

        public void OnPreprocessBuild(BuildReport report)
        {
            _config = TestScenariosSettingsBuilder.GetConfig();
            if (_config == null || !_config.IsTestingToolEnabled)
                return;
            _testScenariosRuntime = Resources.Load<TestScenariosRuntime>(FileNames.TEST_SCENARIOS_RUNTIME);
            Validate();
            _testScenariosRuntime.ResetForBuild();
        }
        private void Validate()
        {
            if (_testScenariosRuntime == null)
            {
                throw new BuildFailedException($"\"{FileNames.TEST_SCENARIOS_RUNTIME}.asset\" not found! Check if file was created correctly");
            }
            if (!HasAnyScenarios())
            {
                throw new BuildFailedException($"Test scenarios are empty! Set up at least one scenario.");
            }
            if (IsAnySceneCorrupted(out var corruptedSceneName))
            {
                throw new BuildFailedException($"Scene \"{corruptedSceneName}\" is corrupted in \"{FileNames.TEST_SCENARIOS_RUNTIME}.asset\". Set up new {FileNames.TEST_SCENARIOS_RUNTIME}.asset from the creator \"{FileNames.TEST_CREATOR}.asset\"");
            }
        }
        private bool HasAnyScenarios()
        {
            return _testScenariosRuntime.Scenarios.Count != 0;
        }
        private bool IsAnySceneCorrupted(out string corruptedSceneName)
        {
            corruptedSceneName = string.Empty;
            foreach (var testScenario in _testScenariosRuntime.Scenarios)
            {
                if (!Application.CanStreamedLevelBeLoaded(testScenario.SceneName))
                {
                    corruptedSceneName = testScenario.SceneName;
                    return true;
                }
            }
            return false;
        }
    }
}
