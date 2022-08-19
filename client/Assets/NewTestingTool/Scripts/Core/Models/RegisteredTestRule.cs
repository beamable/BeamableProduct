using Beamable.NewTestingTool.Core.Models.Descriptors;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	[Serializable]
	public class RegisteredTestRule
	{
		public event Action OnTestResultChanged;
		public TestRuleDescriptor TestRuleDescriptor => _testRuleDescriptor;
		public string TestMethodName => _testMethodName;
		public int Order => _order;

		public TestResult TestResult => _testResult = RegisteredTestRuleMethods.Any(x => x.TestResult == TestResult.Failed) ? TestResult.Failed :
				RegisteredTestRuleMethods.All(x => x.TestResult == TestResult.Passed) ? TestResult.Passed :
				TestResult.NotSet;
		
		public List<RegisteredTestRuleMethod> RegisteredTestRuleMethods => _registeredTestRuleMethods;

		[SerializeField] private string _testMethodName;
		[SerializeField] private int _order;
		[SerializeField] private TestResult _testResult;
		[SerializeField, HideInInspector] TestRuleDescriptor _testRuleDescriptor;
		[SerializeField] private List<RegisteredTestRuleMethod> _registeredTestRuleMethods = new List<RegisteredTestRuleMethod>();

		public RegisteredTestRule(string testMethodName, int order, TestRuleDescriptor testRuleDescriptor)
		{
			_testMethodName = testMethodName;
			_order = order;
			_testRuleDescriptor = testRuleDescriptor;
		}
		public void Reset()
		{
			foreach (var registeredMethodTest in RegisteredTestRuleMethods)
				registeredMethodTest.Reset();
		}

		public void SetupEvents()
		{
			foreach (var registeredTestRuleMethod in _registeredTestRuleMethods)
			{
				registeredTestRuleMethod.OnTestResultChanged -= HandleTestResultChange;
				registeredTestRuleMethod.OnTestResultChanged += HandleTestResultChange;
			}
		}
		private void HandleTestResultChange()
			=> OnTestResultChanged?.Invoke();
	}
}
