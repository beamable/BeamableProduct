using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models.Descriptors
{
	[Serializable]
	public class TestDescriptor
	{
		public string Title
		{
			get => _title;
			set => _title = value;
		}
		public string Description
		{
			get => _description;
			set => _description = value;
		}

		[SerializeField] private string _title;
		[SerializeField] private string _description;
		[SerializeField] private TestResult _testResult;
		[SerializeField] private List<TestRuleDescriptor> _testRuleDescriptors = new List<TestRuleDescriptor>();

		public TestRuleDescriptor GetTestRuleDescriptor(Testable testable)
			=> GetTestRuleDescriptor(testable.GetType().Name);
		public TestRuleDescriptor GetTestRuleDescriptor(string testableName)
			=> _testRuleDescriptors.FirstOrDefault(x => x.TestableName == testableName) ??
			                         CreateTestRuleDescriptor(testableName);

		private TestRuleDescriptor CreateTestRuleDescriptor(string testableName)
		{
			var testDescriptor = new TestRuleDescriptor(testableName);
			_testRuleDescriptors.Add(testDescriptor);
			return testDescriptor;
		}
	}
}
