using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Scripts.Core;
using NewTestingTool.Core;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static NewTestingTool.Constants.TestConstants;

namespace Beamable.NewTestingTool.Core
{
	public class TestController : MonoBehaviour
	{
		[SerializeField] private bool automaticallyStart = true;
		[SerializeField] private bool displayLogs = true;
		[SerializeField] private bool stopOnFirstFailed;

		[SerializeField] private Button passedButton; 
		[SerializeField] private Button failedButton;

		private event Action OnTestReadyToInvoke;

		private TestConfiguration TestConfiguration { get; set; }
		private RegisteredTestScene CurrentTestScene { get; set; }
		private RegisteredTest CurrentTest =>  CurrentTestScene.RegisteredTests[_currentTestIndex];
		private RegisteredTestRule CurrentTestRule => CurrentTest.RegisteredTestRules[_currentOrderIndex];
		private RegisteredTestRuleMethod CurrentTestRuleMethod => CurrentTestRule.RegisteredTestRuleMethods[_currentCaseIndex - 1];

		private int _currentTestIndex = 0, _currentOrderIndex = 0, _currentCaseIndex = 0;

		private Coroutine _coroutine;
		
		private void Awake()
		{
			if (!Application.isPlaying)
				return;
			
			ChangeButtonsInteractableState(false);
			LoadTestData();

			OnTestReadyToInvoke -= HandleTestReadyToInvoke;
			OnTestReadyToInvoke += HandleTestReadyToInvoke;
		}
		private void Start()
		{
			if (!automaticallyStart || !Application.isPlaying)
				return;
			_coroutine = StartCoroutine(InvokeNextTest());
		}

		private void LoadTestData()
		{
			TestConfiguration = Resources.Load<TestConfiguration>(CONFIGURATION_FILE_NAME);
			if (TestConfiguration == null)
			{
				Debug.LogError("Cannot load test scriptable object!");
				Debug.Break();
				return;
			}
			CurrentTestScene = TestConfiguration.GetRegisteredTestScene(SceneManager.GetActiveScene().name);
		}
		private void MarkTestResult(TestResult result)
		{
			if (result == TestResult.NotSet)
			{
				MarkTestResultManually();
				return;
			}

			CurrentTestRuleMethod.TestResult = result;
			ChangeButtonsInteractableState(false);

			if (displayLogs)
				TestableDebug.Log($"Result=[{TestableDebug.WrapWithColor(result, result == TestResult.Passed ? Color.green : Color.red)}] Method=[{TestableDebug.WrapWithColor(CurrentTestRuleMethod.MethodInfo.Name, Color.yellow)}]");
			
			TestConfiguration.OnTestFinished?.Invoke();
			HandleTestFinished();
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

			foreach (var registeredTest in CurrentTestScene.RegisteredTests)
				registeredTest.Reset();
		}
		private IEnumerator InvokeNextTest()
		{
			while (true)
			{
				if (_currentTestIndex < CurrentTestScene.RegisteredTests.Count)
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
				
				TestConfiguration.OnAllTestsFinished?.Invoke();
				TestableDebug.Log("All tests finished");
				SceneManager.LoadScene(0);
				yield break;
			}
		}
		private void ChangeButtonsInteractableState(bool isEnabled)
		{
			passedButton.interactable = isEnabled;
			failedButton.interactable = isEnabled;
		}
		private async void HandleTestReadyToInvoke()
		{
			var result = await CurrentTestRuleMethod.InvokeTest(displayLogs, _currentOrderIndex, _currentCaseIndex - 1);
			if (stopOnFirstFailed && result == TestResult.Failed)
			{
				TestableDebug.Log($"Testing tool stopped due to failed test.");
				return;
			}
			MarkTestResult(result);
		}
		private void HandleTestFinished()
		{
			if (_coroutine != null)
				StopCoroutine(_coroutine);
			_coroutine = StartCoroutine(InvokeNextTest());
		}
	}
}

