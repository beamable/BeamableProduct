using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	public class RegisteredTestScene : ScriptableObject
	{
		public string SceneName => _sceneName;
		public TestResult TestResult =>
			_testResult = RegisteredTests.Any(x => x.TestResult == TestResult.Failed) ? TestResult.Failed :
				RegisteredTests.All(x => x.TestResult == TestResult.Passed) ? TestResult.Passed :
				TestResult.NotSet;
		public List<RegisteredTest> RegisteredTests => _registeredTests;

		[SerializeField] private string _sceneName;
		[SerializeField] private TestResult _testResult;
		[SerializeField] private RegisteredTestSceneDescriptor _registeredTestSceneDescriptor;
		[SerializeField] private List<RegisteredTest> _registeredTests;

		public void Init(string sceneName, List<RegisteredTest> registeredTests, RegisteredTestSceneDescriptor registeredTestSceneDescriptor)
		{
			_sceneName = sceneName;
			_registeredTests = registeredTests;
			_registeredTestSceneDescriptor = registeredTestSceneDescriptor;
		}
	}
}
