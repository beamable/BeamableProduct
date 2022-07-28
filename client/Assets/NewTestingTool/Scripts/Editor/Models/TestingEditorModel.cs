using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Scripts.Core;
using NewTestingTool.Attributes;
using NewTestingTool.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using static NewTestingTool.Constants.TestConstants;

namespace Beamable.Editor.NewTestingTool.Models
{
	public class TestingEditorModel
	{
		public TestConfiguration TestConfiguration
		{
			get
			{
				if (_testConfiguration == null)
					_testConfiguration = LoadScriptableObject<TestConfiguration>(CONFIGURATION_FILE_NAME, PATH_TO_TEST_RESOURCES);
				return _testConfiguration;
			}
		}
		private TestConfiguration _testConfiguration;
		
		public RegisteredTestScene SelectedRegisteredTestScene { get; set; }
		public RegisteredTest SelectedRegisteredTest { get; set; }
		public RegisteredTestRule SelectedRegisteredTestRule { get; set; }
		public RegisteredTestRuleMethod SelectedRegisteredTestRuleMethod { get; set; }
		
		public string GetPathToTestScene(string sceneName) => $"{PATH_TO_TESTS}/{sceneName}/{sceneName}.unity";

		public void Scan()
		{
			if (Application.isPlaying)
				return;

			EditorUtility.DisplayProgressBar(
				"Testing tool",
				"Processing",
				0);

			EditorUtility.SetDirty(TestConfiguration);
			TestConfiguration.Reset();

			try
			{
				var testDirectories = Directory.GetDirectories(PATH_TO_TESTS);
				int index = 0;
				foreach (var testDirectory in testDirectories)
				{
					EditorUtility.DisplayProgressBar(
						"Testing tool",
						$"Processing",
						index / (float)testDirectories.Length);

					var testSceneName = testDirectory.Split('\\')[1];
					var pathToTestScene = GetPathToTestScene(testSceneName);
					var scene = EditorSceneManager.OpenScene(pathToTestScene);

					SetupTestScene(testSceneName);
					EditorSceneManager.SaveScene(scene, pathToTestScene);
				}
			}
			finally
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.ClearProgressBar();
			}

			SelectedRegisteredTestScene = TestConfiguration.RegisteredTestScenes[0];
			SelectedRegisteredTest = SelectedRegisteredTestScene.RegisteredTests[0];
			SelectedRegisteredTestRule = SelectedRegisteredTest.RegisteredTestRules[0];
			SelectedRegisteredTestRuleMethod = SelectedRegisteredTestRule.RegisteredTestRuleMethods[0];

			//EditorSceneManager.OpenScene("Assets/NewTestingTool/Main Menu.unity");
		}
		private void SetupTestScene(string sceneName)
		{
			if (!TryGetTestables(out var testables, out var errorLog))
			{
				TestableDebug.LogError(errorLog);
				return;
			}

			var registeredTests = new List<RegisteredTest>();
			for (var index = 0; index < testables.Count; index++)
			{
				var testable = testables[index];
				if (!TryGetTestableMethods(testable, out var methodInfos, out errorLog))
				{
					TestableDebug.LogError(errorLog);
					return;
				}

				var registeredTestRules = RegisterTestRules(testable, methodInfos);
				registeredTests.Add(new RegisteredTest(testable.GetType().Name, registeredTestRules));
			}

			var registeredTestScene =
				LoadScriptableObject<RegisteredTestScene>(sceneName, $"{PATH_TO_TESTS}/{sceneName}");
			EditorUtility.SetDirty(registeredTestScene);
			registeredTestScene.Init(sceneName, registeredTests);
			TestConfiguration.RegisterTests(registeredTestScene);
		}
		private List<RegisteredTestRule> RegisterTestRules(Testable testable, IEnumerable<MethodInfo> methodInfos)
		{
			var registeredTests = new List<RegisteredTestRule>();
			foreach (var methodInfo in methodInfos)
			{
				var customAttributesData = GetCustomAttributesData(methodInfo);
				foreach (var customAttributeData in customAttributesData)
				{
					RegisterTestRuleMethod(testable, methodInfo, customAttributeData, ref registeredTests);
				}
			}

			return registeredTests.OrderBy(x => x.Order).ToList();
		}
		private static bool TryGetTestables(out List<Testable> results, out string errorLog)
		{
			results = null;
			errorLog = string.Empty;

			var testables = Object.FindObjectsOfType<Testable>().ToList();

			if (!testables.Any())
				errorLog =
					$"Cannot find any \"Testable\" class. Inherit from the \"Testable\" class to access the functionality of the test tool";
			else
				results = testables;

			return results != null;
		}
		private static bool TryGetTestableMethods(Testable testable, out IEnumerable<MethodInfo> methodInfos, out string errorLog)
		{
			errorLog = string.Empty;
			methodInfos = testable.GetType()
			                      .GetRuntimeMethods()
			                      .Where(x => x.GetCustomAttributes(typeof(TestRuleAttribute), false).Length > 0)
			                      .ToArray();

			if (!methodInfos.Any())
			{
				methodInfos = null;
				errorLog =
					$"Cannot find any \"Testable\" methods. Use \"TestStep()\" attribute over the method to register it as a test.";
			}

			return methodInfos != null;
		}
		private IEnumerable<CustomAttributeData> GetCustomAttributesData(MethodInfo methodInfo)
		{
			return methodInfo.CustomAttributes.Where(x => x.AttributeType == typeof(TestRuleAttribute))
			                 .ToArray();
		}
		private static void RegisterTestRuleMethod(Testable testable, MethodInfo methodInfo,
		                                           CustomAttributeData customAttributeData,
		                                           ref List<RegisteredTestRule> registeredTests)
		{
			var arguments = customAttributeData.ConstructorArguments;
			var order = (int)arguments[0].Value;
			var registeredMethodTest =
				new RegisteredTestRuleMethod(testable, methodInfo, arguments.Skip(1).Select(x => x.Value).ToArray());

			if (registeredTests.All(x => x.Order != order))
				registeredTests.Add(new RegisteredTestRule(methodInfo.Name, order));
			registeredTests.First(x => x.Order == order).RegisteredTestRuleMethods.Add(registeredMethodTest);
		}
		private static T LoadScriptableObject<T>(string scriptableObjectName, string path) where T : ScriptableObject
		{
			var scriptable = Resources.Load<T>(scriptableObjectName);
			if (scriptable == null)
			{
				scriptable = ScriptableObject.CreateInstance<T>();
				AssetDatabase.CreateAsset(scriptable, $"{path}/{scriptableObjectName}.asset");
			}

			return scriptable;
		}
	}
}
