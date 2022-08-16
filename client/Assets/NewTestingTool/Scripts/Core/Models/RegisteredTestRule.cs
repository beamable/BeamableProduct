using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	[Serializable]
	public class RegisteredTestRule
	{
		public string TestMethodName => _testMethodName;
		public int Order => _order;

		public TestResult TestResult =>
			_testResult = RegisteredTestRuleMethods.Any(x => x.TestResult == TestResult.Failed) ? TestResult.Failed :
				RegisteredTestRuleMethods.All(x => x.TestResult == TestResult.Passed) ? TestResult.Passed :
				TestResult.NotSet;
		
		public List<RegisteredTestRuleMethod> RegisteredTestRuleMethods => _registeredTestRuleMethods;

		[SerializeField] private string _testMethodName;
		[SerializeField] private int _order;
		[SerializeField] private TestResult _testResult;
		[SerializeField] private List<RegisteredTestRuleMethod> _registeredTestRuleMethods = new List<RegisteredTestRuleMethod>();
		
		public RegisteredTestRule(string testMethodName, int order)
		{
			_testMethodName = testMethodName;
			_order = order;
		}
		public void Reset()
		{
			foreach (var registeredMethodTest in RegisteredTestRuleMethods)
				registeredMethodTest.Reset();
		}
	}
}
