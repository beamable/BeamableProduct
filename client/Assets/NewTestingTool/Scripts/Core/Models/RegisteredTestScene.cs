using System.Collections.Generic;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	public class RegisteredTestScene : ScriptableObject
	{
		public string SceneName => _sceneName;
		public List<RegisteredTest> RegisteredTests => _registeredTests;

		[SerializeField] private string _sceneName;
		[SerializeField] private List<RegisteredTest> _registeredTests;

		public void Init(string sceneName, List<RegisteredTest> registeredTests)
		{
			_sceneName = sceneName;
			_registeredTests = registeredTests;
		}
	}
}
