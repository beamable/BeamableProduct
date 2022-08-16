using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Scripts.Core;
using NewTestingTool.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static NewTestingTool.Constants.TestConstants;

namespace Beamable.NewTestingTool.Core
{
	public class TestController : ComponentSingleton<TestController>
	{
		[SerializeField] private bool automaticallyStart = true;
		[SerializeField] private bool displayLogs = true;
		[SerializeField] private bool stopOnFirstFailed;

		[SerializeField] private Button passedButton; 
		[SerializeField] private Button failedButton;

		[SerializeField] private Transform testObjectsParent;

		private event Action OnTestReadyToInvoke;
		private event Action OnNextTestRequested;

		private TestConfiguration TestConfiguration { get; set; }
		private RegisteredTest CurrentTest => _registeredTests[_currentTestIndex];
		private RegisteredTestRule CurrentTestRule => CurrentTest.RegisteredTestRules[_currentOrderIndex];
		private RegisteredTestRuleMethod CurrentTestRuleMethod => CurrentTestRule.RegisteredTestRuleMethods[_currentCaseIndex - 1];

		private List<RegisteredTest> _registeredTests = new List<RegisteredTest>();
		private int _currentTestIndex, _currentOrderIndex, _currentCaseIndex;

		private List<GameObject> _testGroups = new List<GameObject>();

		private void Awake()
		{
			if (!Application.isPlaying)
				return;
			
			ChangeButtonsInteractableState(false);
			_registeredTests = LoadTestData();

			foreach (Transform child in testObjectsParent)
			{
				child.gameObject.SetActive(false);
				_testGroups.Add(child.gameObject);
			}

			OnTestReadyToInvoke -= HandleTestReadyToInvoke;
			OnTestReadyToInvoke += HandleTestReadyToInvoke;
			OnNextTestRequested -= HandleNextTestRequested;
			OnNextTestRequested += HandleNextTestRequested;
		}
		private void Start()
		{
			if (!automaticallyStart || !Application.isPlaying)
				return;
			StartCoroutine(InvokeNextTest());
		}

		private List<RegisteredTest> LoadTestData()
		{
			TestConfiguration = Resources.Load<TestConfiguration>(CONFIGURATION_FILE_NAME);
			if (TestConfiguration == null)
			{
				Debug.LogError("Cannot load test scriptable object!");
				Debug.Break();
				return null;
			}
			return TestConfiguration.GetTestData(SceneManager.GetActiveScene().name);
		}
		private void MarkTestResult(TestResult result)
		{
			if (result == TestResult.NotSet)
			{
				MarkTestResultManually();
				return;
			}
			
			ChangeButtonsInteractableState(false);
			ResetTestObjects();

			if (displayLogs)
				TestableDebug.Log($"Result=[{TestableDebug.WrapWithColor(result, result == TestResult.Passed ? Color.green : Color.red)}] Method=[{TestableDebug.WrapWithColor(CurrentTestRuleMethod.MethodInfo.Name, Color.yellow)}]");
			
			OnNextTestRequested?.Invoke();
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

			foreach (var registeredTest in _registeredTests)
				registeredTest.Reset();
		}
		private IEnumerator InvokeNextTest()
		{
			while (true)
			{
				if (_currentTestIndex < _registeredTests.Count)
				{
					if (_currentOrderIndex < CurrentTest.RegisteredTestRules.Count)
					{
						if (_currentCaseIndex < CurrentTestRule.RegisteredTestRuleMethods.Count)
						{
							_currentCaseIndex++;
							OnTestReadyToInvoke?.Invoke();
							yield break;
						}

						_currentOrderIndex++;
						_currentCaseIndex = 0;
						continue;
					}

					_currentTestIndex++;
					_currentOrderIndex = 0;
					_currentCaseIndex = 0;
					continue;
				}
				
				TestableDebug.Log("All tests finished");
				yield break;
			}
		}
		private void ChangeButtonsInteractableState(bool isEnabled)
		{
			passedButton.interactable  = isEnabled;
			failedButton.interactable  = isEnabled;
		}
		private async void HandleTestReadyToInvoke()
		{
			var result = await CurrentTestRuleMethod.InvokeTest(displayLogs, _currentOrderIndex, _currentCaseIndex - 1);
			CurrentTestRuleMethod.TestResult = result;
			
			if (stopOnFirstFailed && result == TestResult.Failed)
			{
				TestableDebug.Log($"Testing tool stopped due to failed test.");
				return;
			}
			MarkTestResult(result);
		}
		private void HandleNextTestRequested() => StartCoroutine(InvokeNextTest());

		private void ResetTestObjects()
		{
			foreach (var testGroup in _testGroups)
				testGroup.SetActive(false);
		}
	}
}

