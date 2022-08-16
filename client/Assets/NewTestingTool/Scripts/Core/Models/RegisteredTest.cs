using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	[Serializable]
	public class RegisteredTest
	{
		public string TestClassName => _testClassName;
		public TestResult TestResult =>
			_testResult = RegisteredTestRules.Any(x => x.TestResult == TestResult.Failed) ? TestResult.Failed :
				RegisteredTestRules.All(x => x.TestResult == TestResult.Passed) ? TestResult.Passed :
				TestResult.NotSet;
		public List<RegisteredTestRule> RegisteredTestRules => _registeredTestRules;
		
		[SerializeField] private string _testClassName;
		[SerializeField] private TestResult _testResult;
		[SerializeField] private List<RegisteredTestRule> _registeredTestRules;
		
		public RegisteredTest(string testClassName, List<RegisteredTestRule> registeredTestRules)
		{
			_testClassName = testClassName;
			_registeredTestRules = registeredTestRules;
		}
		public void Reset()
		{
			foreach (var registeredTestRule in RegisteredTestRules)
				registeredTestRule.Reset();
		}
	}
}
