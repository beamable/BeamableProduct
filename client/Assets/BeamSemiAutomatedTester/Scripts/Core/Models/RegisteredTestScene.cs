using Beamable.BSAT.Core.Models.Descriptors;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.BSAT.Core.Models
{
	public class RegisteredTestScene : ScriptableObject
	{
		public event Action OnTestResultChanged;
		public TestSceneDescriptor TestSceneDescriptor => _testSceneDescriptor;
		public string SceneName => _sceneName;
		public TestResult TestResult => _testResult = RegisteredTests.Any(x => x.TestResult == TestResult.Failed) ? TestResult.Failed :
				RegisteredTests.All(x => x.TestResult == TestResult.Passed) ? TestResult.Passed :
				TestResult.NotSet;
		public List<RegisteredTest> RegisteredTests => _registeredTests;

		[SerializeField] private string _sceneName;
		[SerializeField] private TestResult _testResult;
		[SerializeField, HideInInspector] private TestSceneDescriptor _testSceneDescriptor;
		[SerializeField] private List<RegisteredTest> _registeredTests;

		public void Init(string sceneName, List<RegisteredTest> registeredTests, TestSceneDescriptor testSceneDescriptor)
		{
			_sceneName = sceneName;
			_registeredTests = registeredTests;
			_testSceneDescriptor = testSceneDescriptor;
			SetupEvents();
		}
		private void SetupEvents()
		{
			foreach (var registeredTest in _registeredTests)
			{
				registeredTest.OnTestResultChanged -= HandleTestResultChange;
				registeredTest.OnTestResultChanged += HandleTestResultChange;
				registeredTest.SetupEvents();
			}
		}
		private void HandleTestResultChange()
			=> OnTestResultChanged?.Invoke();
	}
}
