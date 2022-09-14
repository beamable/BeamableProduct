using Beamable.BSAT.Core.Models.Descriptors;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.BSAT.Core.Models
{
	[Serializable]
	public class RegisteredTest
	{
		public event Action OnTestResultChanged;
		public TestDescriptor TestDescriptor => _testDescriptor;
		
		public string TestClassName => _testClassName;
		public TestResult TestResult => _testResult = RegisteredTestRules.Any(x => x.TestResult == TestResult.Failed) ? TestResult.Failed :
				RegisteredTestRules.All(x => x.TestResult == TestResult.Passed) ? TestResult.Passed :
				TestResult.NotSet;
		public List<RegisteredTestRule> RegisteredTestRules => _registeredTestRules;
		
		[SerializeField] private string _testClassName;
		[SerializeField] private TestResult _testResult;
		[SerializeField, HideInInspector] private TestDescriptor _testDescriptor;
		[SerializeField] private List<RegisteredTestRule> _registeredTestRules;
		
		public RegisteredTest(string testClassName, List<RegisteredTestRule> registeredTestRules, TestDescriptor testDescriptor)
		{
			_testClassName = testClassName;
			_registeredTestRules = registeredTestRules;
			_testDescriptor = testDescriptor;
		}
		public void Reset()
		{
			foreach (var registeredTestRule in _registeredTestRules)
				registeredTestRule.Reset();
		}

		public void SetupEvents()
		{
			foreach (var registeredTestRule in _registeredTestRules)
			{
				registeredTestRule.OnTestResultChanged -= HandleTestResultChange;
				registeredTestRule.OnTestResultChanged += HandleTestResultChange;
				registeredTestRule.SetupEvents();
			}
		}
		private void HandleTestResultChange()
			=> OnTestResultChanged?.Invoke();

		public ArrayDict GenerateReport()
		{
			var data = new List<ArrayDict>();
			foreach (var registeredTestRule in _registeredTestRules)
			{
				var nestedData = new ArrayDict
				{
					{ registeredTestRule.TestMethodName, registeredTestRule.GenerateReport() }
				};
				data.Add(nestedData);
			}
			return new ArrayDict
			{
				{ "TestResult", TestResult},
				{ "Title", TestDescriptor.Title },
				{ "Description", TestDescriptor.Description },
				{ "TestRules", data}
			};
		}
	}
}
