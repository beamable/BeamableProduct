using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Core.Models.Descriptors;
using Beamable.NewTestingTool.Scripts.Core;
using NewTestingTool.Attributes;
using NewTestingTool.Core;
using NewTestingTool.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static NewTestingTool.Constants.TestConstants;
using Object = UnityEngine.Object;

namespace Beamable.Editor.NewTestingTool.Models
{
	public class TestingEditorModel
	{
		public TestConfiguration TestConfiguration
		{
			get
			{
				if (_testConfiguration == null)
					_testConfiguration = TestExtensions.LoadScriptableObject<TestConfiguration>(CONFIGURATION_FILE_NAME, string.Empty, PATH_TO_RESOURCES);
				return _testConfiguration;
			}
		}
		private TestConfiguration _testConfiguration;
		
		public RegisteredTestScene SelectedRegisteredTestScene { get; set; }
		public RegisteredTest SelectedRegisteredTest { get; set; }
		public RegisteredTestRule SelectedRegisteredTestRule { get; set; }
		public RegisteredTestRuleMethod SelectedRegisteredTestRuleMethod { get; set; }
		
		public void Scan() => TestScanUtility.Scan(this);
		public void CreateTestScene(string testName) => TestManagement.CreateTestScene(testName);
		public void DeleteTestScene(RegisteredTestScene registeredTestScene) => TestManagement.DeleteTestScene(registeredTestScene);
	}
	internal static class TestManagement
	{
		public static void CreateTestScene(string testName)
		{
			Directory.CreateDirectory($"Assets/NewTestingTool/Tests/{testName}");
			Directory.CreateDirectory($"Assets/NewTestingTool/Tests/{testName}/Scripts");
			File.Copy("Assets/NewTestingTool/Templates/TestTemplate.unity",
			          $"Assets/NewTestingTool/Tests/{testName}/{testName}.unity");
			AssetDatabase.Refresh();
		}
		public static void DeleteTestScene(RegisteredTestScene registeredTestScene)
		{
			Directory.Delete($"Assets/NewTestingTool/Tests/{registeredTestScene.SceneName}", true);
			File.Delete($"Assets/NewTestingTool/Tests/{registeredTestScene.SceneName}.meta");
			Directory.Delete($"Assets/NewTestingTool/Resources/Tests/{registeredTestScene.SceneName}", true);
			File.Delete($"Assets/NewTestingTool/Resources/Tests/{registeredTestScene.SceneName}.meta");
			AssetDatabase.Refresh();
		}
	}
	internal static class TestScanUtility
	{
		private static TestConfiguration _testConfiguration;
		
		public static void Scan(TestingEditorModel testingEditorModel)
		{
			if (Application.isPlaying) return;

			_testConfiguration = testingEditorModel.TestConfiguration;
			EditorUtility.DisplayProgressBar("Testing tool", "Processing", 0);
			EditorUtility.SetDirty(_testConfiguration);
			_testConfiguration.Reset();

			try
			{
				var testDirectories = Directory.GetDirectories(PATH_TO_TEST_SCENES);
				for (var index = 0; index < testDirectories.Length; index++)
				{
					var testDirectory = testDirectories[index];
					EditorUtility.DisplayProgressBar("Testing tool", $"Processing", index / (float)testDirectories.Length);

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

			if (_testConfiguration.RegisteredTestScenes.Count != 0)
			{
				testingEditorModel.SelectedRegisteredTestScene = _testConfiguration.RegisteredTestScenes[0];
				testingEditorModel.SelectedRegisteredTest = testingEditorModel.SelectedRegisteredTestScene.RegisteredTests[0];
				testingEditorModel.SelectedRegisteredTestRule = testingEditorModel.SelectedRegisteredTest.RegisteredTestRules[0];
				testingEditorModel.SelectedRegisteredTestRuleMethod = testingEditorModel.SelectedRegisteredTestRule.RegisteredTestRuleMethods[0];
			}

			EditorSceneManager.OpenScene($"{PATH_TO_TESTING_TOOL}/TestMainMenu.unity");
		}
		
		private static void SetupTestScene(string sceneName)
		{
			if (!TryGetTestables(sceneName, out var testables, out var errorLog))
			{
				TestableDebug.LogWarning(errorLog);
				return;
			}

			var testSceneDescriptor = TestExtensions.LoadScriptableObject<TestSceneDescriptor>($"{sceneName}_Descriptor", $"Tests/{sceneName}", PATH_TO_RESOURCES_TESTS(sceneName));
			EditorUtility.SetDirty(testSceneDescriptor);

			var registeredTests = new List<RegisteredTest>();
			foreach (var testable in testables)
			{
				if (!TryGetTestableMethods(testable, out var methodInfos, out errorLog))
				{
					TestableDebug.LogError(errorLog);
					return;
				}

				var registeredTestRules = RegisterTestRules(testable, methodInfos, testSceneDescriptor);
				registeredTests.Add(new RegisteredTest(testable.GetType().Name, registeredTestRules, testSceneDescriptor.GetTestDescriptor() ));
			}

			var registeredTestScene = TestExtensions.LoadScriptableObject<RegisteredTestScene>(sceneName, $"Tests/{sceneName}", PATH_TO_RESOURCES_TESTS(sceneName));
			EditorUtility.SetDirty(registeredTestScene);
			registeredTestScene.Init(sceneName, registeredTests, testSceneDescriptor);
			_testConfiguration.RegisterTests(registeredTestScene);
		}
		private static List<RegisteredTestRule> RegisterTestRules(Testable testable, IEnumerable<MethodInfo> methodInfos, TestSceneDescriptor testSceneDescriptor)
		{
			var registeredTests = new List<RegisteredTestRule>();
			var testRuleDescriptor = testSceneDescriptor.GetTestDescriptor().GetTestRuleDescriptor(testable);
			foreach (var methodInfo in methodInfos)
			{
				var testRuleMethodDescriptor = testRuleDescriptor.GetTestRuleMethodDescriptor(methodInfo);
				var customAttributesData = GetCustomAttributesData(methodInfo);
				foreach (var customAttributeData in customAttributesData)
					RegisterTestRuleMethod(testable, methodInfo, customAttributeData, ref registeredTests, testRuleDescriptor);
			}

			#if UNITY_2020_3_OR_NEWER
				AssetDatabase.SaveAssetIfDirty(testSceneDescriptor);
			#else
				AssetDatabase.SaveAssets();
			#endif
			
			return registeredTests.OrderBy(x => x.Order).ToList();
			
		}
		private static bool TryGetTestables(string sceneName, out List<Testable> results, out string errorLog)
		{
			results = null;
			errorLog = string.Empty;

			var testables = Object.FindObjectsOfType<Testable>().ToList();

			if (!testables.Any())
				errorLog =
					$"An error occured in scene=[{sceneName}]. Cannot find any \"Testable\" class. Inherit from the \"Testable\" class to access the functionality of the test tool. ";
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
		private static IEnumerable<CustomAttributeData> GetCustomAttributesData(MethodInfo methodInfo)
		{
			return methodInfo.CustomAttributes.Where(x => x.AttributeType == typeof(TestRuleAttribute))
			                 .ToArray();
		}
		private static void RegisterTestRuleMethod(Testable testable, MethodInfo methodInfo,
		                                           CustomAttributeData customAttributeData,
		                                           ref List<RegisteredTestRule> registeredTests,
		                                           TestRuleDescriptor testRuleDescriptor)
		{
			var allArguments = customAttributeData.ConstructorArguments;
			var order = (int)allArguments[0].Value;
			var filteredArguments = allArguments.Skip(1).Select(x => x.Value).ToArray();
			
			var registeredMethodTest = new RegisteredTestRuleMethod(ref testable, methodInfo, filteredArguments, testRuleDescriptor.GetTestRuleMethodDescriptor(methodInfo));

			if (registeredTests.All(x => x.Order != order))
				registeredTests.Add(new RegisteredTestRule(methodInfo.Name, order, testRuleDescriptor));
			registeredTests.First(x => x.Order == order).RegisteredTestRuleMethods.Add(registeredMethodTest);
		}
	}
}
