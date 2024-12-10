using Beamable.BSAT.Attributes;
using Beamable.BSAT.Core;
using Beamable.BSAT.Core.Models;
using Beamable.BSAT.Core.Models.Descriptors;
using Beamable.BSAT.Extensions;
using Beamable.BSAT;
using Beamable.Server.Editor.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

using static Beamable.BSAT.Constants.TestConstants.General;
using static Beamable.BSAT.Constants.TestConstants.Paths;
using static Beamable.BSAT.Constants.TestConstants.SessionState;

namespace Beamable.BSAT.Editor.Models
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
		public void GenerateReport()
		{
			var json = TestConfiguration.GenerateReport();

			if (!Directory.Exists(PATH_TO_REPORTS))
				Directory.CreateDirectory(PATH_TO_REPORTS);

			var fileName = GetReportFileName($"{DateTime.UtcNow:yyyy-MM-dd_hh-mm-ss-tt}");
			var pathToFile = $"{PATH_TO_REPORTS}/{fileName}.json";
			using (var writer = File.CreateText(pathToFile))
			{
				writer.Write(json);
				writer.Close();
			}
			
			AssetDatabase.Refresh();
			if (EditorUtility.DisplayDialog("Report Generator", $"Report has been generated at path=[{pathToFile}].",
			                                "Ok"))
			{
				EditorUtility.RevealInFinder(pathToFile);
			}
		}
	}
	internal static class TestManagement
	{
		private static bool ShouldCreateNewTest
		{
			get => SessionState.GetBool(SHOULD_CREATE_NEW_TEST, false);
			set => SessionState.SetBool(SHOULD_CREATE_NEW_TEST, value);
		}
		private static string TestName
		{
			get => SessionState.GetString(TEST_NAME, string.Empty);
			set => SessionState.SetString(TEST_NAME, value);
		}
		private static string ScriptName
		{
			get => SessionState.GetString(SCRIPT_NAME, string.Empty);
			set => SessionState.SetString(SCRIPT_NAME, value);
		}

		public static void CreateTestScene(string testName)
		{
			WindowStateUtility.DisableAllWindows();
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
			
			ShouldCreateNewTest = true;
			TestName = testName;
			
			Directory.CreateDirectory(GetPathToTest(testName));
			Directory.CreateDirectory(GetPathToTestScripts(testName));
			File.Copy($"{PATH_TO_SCENE_TEMPLATE}.unity",
			          GetPathToTestScene(testName));

			GenerateTemplateCode();
		}
		private static void GenerateTemplateCode()
		{
			ScriptName = $"{TestName}Script";

			var text = File.ReadAllText($"{PATH_TO_SCRIPT_TEMPLATE}.cs");
			text = text.Replace(SCRIPT_TEMPLATE_NAME, ScriptName);
			text = text.Replace(SCRIPT_TEMPLATE_NAMESPACE_NAME, TestName);
			File.WriteAllText($"{GetPathToTestScripts(TestName)}/{ScriptName}.cs", text);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		[DidReloadScripts]
		private static void HandleAfterAssemblyReload()
		{
			if (!ShouldCreateNewTest)
				return;
			ShouldCreateNewTest = false;

			EditorApplication.delayCall += () =>
			{
				var scene = EditorSceneManager.OpenScene(GetPathToTestScene(TestName));
				var type = Type.GetType(
					$"Beamable.BSAT.Test.{TestName}.{ScriptName}, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
				var go = new GameObject(ScriptName);
				go.AddComponent(type);
				SceneManager.MoveGameObjectToScene(go, scene);

				EditorSceneManager.SaveScene(scene);
				AssetDatabase.Refresh();

				WindowStateUtility.EnableAllWindows();
			};
		}

		public static void DeleteTestScene(RegisteredTestScene registeredTestScene)
		{
			Directory.Delete(GetPathToTest(registeredTestScene.SceneName), true);
			File.Delete($"{GetPathToTest(registeredTestScene.SceneName)}.meta");
			Directory.Delete(GetPathToTestResources(registeredTestScene.SceneName), true);
			File.Delete($"{GetPathToTestResources(registeredTestScene.SceneName)}.meta");
			AssetDatabase.Refresh();
		}
	}
	internal static class TestScanUtility
	{
		private static TestConfiguration _testConfiguration;
		
		public static void Scan(TestingEditorModel testingEditorModel)
		{
			if (Application.isPlaying) return;

			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

			_testConfiguration = testingEditorModel.TestConfiguration;
			EditorUtility.DisplayProgressBar("Testing tool", "Processing", 0);
			EditorUtility.SetDirty(_testConfiguration);
			_testConfiguration.Reset();

			var activeScenePath = string.Empty;
			try
			{
				if (!Directory.Exists(PATH_TO_TEST_SCENES))
					Directory.CreateDirectory(PATH_TO_TEST_SCENES);
				
				var testDirectories = Directory.GetDirectories(PATH_TO_TEST_SCENES);
				if (testDirectories.Length != 0)
					activeScenePath = SceneManager.GetActiveScene().path;
				
				for (var index = 0; index < testDirectories.Length; index++)
				{
					var testDirectory = testDirectories[index];
					var testSceneName = testDirectory.Split(Path.DirectorySeparatorChar).LastOrDefault();
					
					EditorUtility.DisplayProgressBar("Testing tool", $"Processing test=[{testSceneName}]", index / (float)testDirectories.Length);

					if (string.IsNullOrWhiteSpace(testSceneName))
						continue;

					var pathToTestScene = GetPathToTestScene(testSceneName);
					var scene = EditorSceneManager.OpenScene(pathToTestScene);
					TrySetupTestScene(testSceneName);
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
			
			if (!string.IsNullOrWhiteSpace(activeScenePath))
				EditorSceneManager.OpenScene(activeScenePath);
		}
		
		private static void TrySetupTestScene(string sceneName)
		{
			if (!TryGetTestables(sceneName, out var testables, out var errorLog))
			{
				TestableDebug.LogWarning(errorLog);
				return;
			}

			var testSceneDescriptor = TestExtensions.LoadScriptableObject<TestSceneDescriptor>($"{sceneName}_Descriptor", $"Tests/{sceneName}", GetPathToTestResources(sceneName));
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
				registeredTests.Add(new RegisteredTest(testable.GetType().Name, registeredTestRules, testSceneDescriptor.GetTestDescriptor()));
			}

			var registeredTestScene = TestExtensions.LoadScriptableObject<RegisteredTestScene>(sceneName, $"Tests/{sceneName}", GetPathToTestResources(sceneName));
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

#if UNITY_6000_0_OR_NEWER
			var testables = Object.FindObjectsByType<Testable>(FindObjectsSortMode.None).ToList();
#else
			var testables = Object.FindObjectsOfType<Testable>().ToList();
#endif
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
