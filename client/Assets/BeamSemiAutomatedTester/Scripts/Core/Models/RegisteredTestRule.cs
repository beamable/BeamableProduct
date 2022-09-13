using Beamable.BSAT.Core.Models.Descriptors;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.BSAT.Core.Models
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

		public ArrayDict GenerateReport()
		{
			var data = new List<ArrayDict>();
			for (int index = 0; index < _registeredTestRuleMethods.Count; index++)
			{
				var registeredTestRuleMethod = _registeredTestRuleMethods[index];
				var nestedData = new ArrayDict
				{
					{$"{index}", registeredTestRuleMethod.GenerateReport()}
				};
				data.Add(nestedData);
			}

			return new ArrayDict
			{
				{ "TestResult", TestResult},
				{ "Title", _registeredTestRuleMethods[0].Title },
				{ "Description", _registeredTestRuleMethods[0].Description },
				{ "TestRuleMethods", data },
			};
		}
	}
}
