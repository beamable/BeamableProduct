using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Helpers.TestableDebug;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using static Beamable.NewTestingTool.Constants.TestConstants.General;

namespace Beamable.NewTestingTool.Core
{
	public class TestController : MonoBehaviour
	{
		public event Action OnTestReadyToInvoke;
		public event Action OnManualTestResultRequested;
		public bool AutomaticallyStart => automaticallyStart;
		
		[SerializeField] private bool automaticallyStart = true;
		[SerializeField] private bool displayLogs = true;
		[SerializeField] private bool stopOnFirstFailed;

		public TestConfiguration TestConfiguration { get; private set; }
		public RegisteredTestScene CurrentTestScene { get; private set; }
		public RegisteredTest CurrentTest =>  CurrentTestScene.RegisteredTests[_currentTestIndex];
		public RegisteredTestRule CurrentTestRule => CurrentTest.RegisteredTestRules[_currentOrderIndex];
		public RegisteredTestRuleMethod CurrentTestRuleMethod => CurrentTestRule.RegisteredTestRuleMethods[_currentCaseIndex - 1];

		private int _currentTestIndex = 0, _currentOrderIndex = 0, _currentCaseIndex = 0;

		private Coroutine _coroutine;
		
		private void Awake()
		{
			if (!Application.isPlaying)
				return;
			LoadTestData();
		}
		private void Start()
		{
			if (!automaticallyStart || !Application.isPlaying)
				return;
			StartTest();
		}
		private void OnEnable()
		{
			OnTestReadyToInvoke += HandleTestReadyToInvoke;

		}
		private void OnDisable()
		{
			OnTestReadyToInvoke -= HandleTestReadyToInvoke;
		}

		public void StartTest()
		{
			if (CurrentTestScene == null)
			{
				TestableDebug.LogWarning($"No tests registered in scene=[{SceneManager.GetActiveScene().name}]");
				return;
			}

			if (_coroutine != null)
				return;
			_coroutine = StartCoroutine(InvokeNextTest());
		}

		private void LoadTestData()
		{
			TestConfiguration = Resources.Load<TestConfiguration>(CONFIGURATION_FILE_NAME);
			if (TestConfiguration == null)
			{
				TestableDebug.LogError("Cannot load test scriptable object!");
				Debug.Break();
				return;
			}
			CurrentTestScene = TestConfiguration.GetRegisteredTestScene(SceneManager.GetActiveScene().name);
		}
		public void MarkTestResult(TestResult result)
		{
			if (result == TestResult.NotSet)
			{
				OnManualTestResultRequested?.Invoke();
				return;
			}
			CurrentTestRuleMethod.TestResult = result;

			if (displayLogs)
				TestableDebug.Log($"Result=[{TestableDebug.WrapWithColor(result, result == TestResult.Passed ? Color.green : Color.red)}] Method=[{TestableDebug.WrapWithColor(CurrentTestRuleMethod.MethodInfo.Name, Color.yellow)}]");
			
			TestConfiguration.OnTestFinished?.Invoke();
			HandleTestFinished();
		}
		public void MarkTestAsPassed() => MarkTestResult(TestResult.Passed);
		public void MarkTestAsFailed() => MarkTestResult(TestResult.Failed);
		private IEnumerator InvokeNextTest()
		{
			yield return new WaitForEndOfFrame();
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
				yield break;
			}
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

