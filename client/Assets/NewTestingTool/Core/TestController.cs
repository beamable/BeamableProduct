using NewTestingTool.Attributes;
using NewTestingTool.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.UI;

namespace NewTestingTool.Core
{
	public class TestController : ComponentSingleton<TestController>
	{
		[SerializeField] private bool automaticallyStart = true;
		[SerializeField] private bool displayLogs = true;

		[SerializeField] private Button passedButton; 
		[SerializeField] private Button failedButton; 

		private List<RegisteredTest> CurrentTest => _allTests[_currentTestIndex];
		private RegisteredTest CurrentRegisteredTest => CurrentTest[_currentOrderIndex];
		private RegisteredTestMethod CurrentTestedTestMethod => CurrentRegisteredTest.RegisteredMethodTests[_currentCaseIndex - 1];

		private readonly List<List<RegisteredTest>> _allTests = new List<List<RegisteredTest>>();
		private int _currentTestIndex, _currentOrderIndex, _currentCaseIndex;
		private bool _isSetup;

		private void Awake()
		{
			ChangeButtonsInteractableState(false);
			Init();
		}
		private async void Start()
		{
			if (automaticallyStart)
				await InvokeNextTest();
		}
		
		private void Init()
		{
			if (!TryGetTestables(out var testables, out var errorLog))
			{
				TestableDebug.LogError(errorLog);
				return;
			}

			for (var index = 0; index < testables.Count; index++)
			{
				var testable = testables[index];
				if (!TryGetTestableMethods(testable, out var methodInfos, out errorLog))
				{
					TestableDebug.LogError(errorLog);
					return;
				}

				var registeredTests = RegisterTests(testable, methodInfos);
				_allTests.Add(registeredTests);
			}

			_isSetup = true;
			
			if (displayLogs)
				TestableDebug.Log($"\"TestController\" setup correctly.");
		}
		private List<RegisteredTest> RegisterTests(Testable testable, IEnumerable<MethodInfo> methodInfos)
		{
			var registeredTests = new List<RegisteredTest>();
			foreach (var methodInfo in methodInfos)
			{
				var customAttributesData = GetCustomAttributesData(methodInfo);
				foreach (var customAttributeData in customAttributesData)
				{
					RegisterTest(testable, methodInfo, customAttributeData, ref registeredTests);
				}
			}
			return registeredTests.OrderBy(x => x.Order).ToList();
		}
		private bool TryGetTestables(out List<Testable> results, out string errorLog)
		{
			results = null;
			errorLog = string.Empty;
			
			var testables = FindObjectsOfType<Testable>().ToList();

			if (!testables.Any())
				errorLog = $"Cannot find any \"Testable\" class. Inherit from the \"Testable\" class to access the functionality of the test tool";
			else
				results = testables;

			return results != null;
		}
		private bool TryGetTestableMethods(Testable testable, out IEnumerable<MethodInfo> methodInfos, out string errorLog)
		{
			errorLog = string.Empty;
			methodInfos = testable.GetType()
			                      .GetRuntimeMethods()
			                      .Where(x => x.GetCustomAttributes(typeof(TestStepAttribute), false).Length > 0)
			                      .ToArray();

			if (!methodInfos.Any())
			{
				methodInfos = null;
				errorLog = $"Cannot find any \"Testable\" methods. Use \"TestStep()\" attribute over the method to register it as a test.";
			}
			return methodInfos != null;
		}
		private IEnumerable<CustomAttributeData> GetCustomAttributesData(MethodInfo methodInfo)
		{
			return methodInfo.CustomAttributes.Where(x => x.AttributeType == typeof(TestStepAttribute))
			                 .ToArray();
		}
		private void RegisterTest(Testable testable, MethodInfo methodInfo, CustomAttributeData customAttributeData, ref List<RegisteredTest> registeredTests)
		{
			var arguments = customAttributeData.ConstructorArguments;
			var order = (int)arguments[0].Value;
			var registeredMethodTest = new RegisteredTestMethod(testable, methodInfo, arguments.Skip(1).Select(x => x.Value).ToArray());
					
			if (registeredTests.All(x => x.Order != order))
				registeredTests.Add(new RegisteredTest(order));
			registeredTests.First(x => x.Order == order).RegisteredMethodTests.Add(registeredMethodTest);
		}
		private async void MarkTestResult(TestResult result)
		{
			if (result == TestResult.NotSet)
			{
				MarkTestResultManually();
				return;
			}
			
			ChangeButtonsInteractableState(false);
			CurrentTestedTestMethod.TestResult = result;
			if (displayLogs)
				TestableDebug.Log(
					$"Result=[{TestableDebug.WrapWithColor(result, result == TestResult.Passed ? Color.green : Color.red)}] Method=[{TestableDebug.WrapWithColor(CurrentTestedTestMethod.MethodInfo.Name, Color.yellow)}]");

			await InvokeNextTest();
		}
		private void MarkTestResultManually()
		{
			void MarkTestAsPassed() => MarkTestResult(TestResult.Passed);
			void MarkTestAsFailed() => MarkTestResult(TestResult.Failed);

			passedButton.onClick.RemoveListener(MarkTestAsPassed);
			passedButton.onClick.AddListener(MarkTestAsPassed);
			
			failedButton.onClick.RemoveListener(MarkTestAsFailed);
			failedButton.onClick.AddListener(MarkTestAsFailed);

			ChangeButtonsInteractableState(true);
		}
		/// <summary>
		/// Restarts all tests loaded in a runtime to the starting position.
		/// </summary>
		public void ResetTests()
		{
			_currentTestIndex = 0;
			_currentOrderIndex = 0;
			_currentCaseIndex = 0;

			foreach (var registeredTests in _allTests)
				foreach (var registeredTest in registeredTests)
					registeredTest.Reset();
		}
		
		private async Task InvokeNextTest()
		{
			while (true)
			{
				if (!_isSetup)
				{
					TestableDebug.LogError("\"TestController\" is not properly setup!");
					return;
				}

				if (_currentTestIndex < _allTests.Count)
				{
					var registeredTests = _allTests[_currentTestIndex];
					if (_currentOrderIndex < registeredTests.Count)
					{
						var registeredMethodTests = _allTests[_currentTestIndex][_currentOrderIndex].RegisteredMethodTests;
						if (_currentCaseIndex < registeredMethodTests.Count)
						{
							_currentCaseIndex++;
							await registeredMethodTests[_currentCaseIndex - 1].InvokeTest(displayLogs, _currentOrderIndex, _currentCaseIndex - 1, MarkTestResult);
						}
						else
						{
							_currentOrderIndex++;
							_currentCaseIndex = 0;
							continue;
						}
					}
					else
					{
						_currentTestIndex++;
						_currentOrderIndex = 0;
						_currentCaseIndex = 0;
						continue;
					}
				}
				else
					TestableDebug.Log("ALL TESTS COMPLETED");

				break;
			}
		}
		private void ChangeButtonsInteractableState(bool isEnabled)
		{
			passedButton.interactable  = isEnabled;
			failedButton.interactable  = isEnabled;
		}
	}

	public enum TestResult
	{
		NotSet,
		Passed,
		Failed,
	}
}

