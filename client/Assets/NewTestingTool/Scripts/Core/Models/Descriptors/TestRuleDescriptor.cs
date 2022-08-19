using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models.Descriptors
{
	[Serializable]
	public class TestRuleDescriptor
	{
		public string TestableName => _testableName;
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

		[SerializeField] private string _testableName;
		[SerializeField] private string _title;
		[SerializeField] private string _description;
		[SerializeField] private List<TestRuleMethodDescriptor> _testRuleMethodDescriptors = new List<TestRuleMethodDescriptor>();

		public TestRuleDescriptor(string testableName)
		{
			_testableName = testableName;
		}

		public TestRuleMethodDescriptor GetTestRuleMethodDescriptor(MethodInfo methodInfo)
			=> GetTestRuleMethodDescriptor(methodInfo.Name);

		public TestRuleMethodDescriptor GetTestRuleMethodDescriptor(string methodName) =>
			_testRuleMethodDescriptors.FirstOrDefault(x => x.MethodName == methodName) ??
			CreateTestRuleMethodDescriptor(methodName);

		private TestRuleMethodDescriptor CreateTestRuleMethodDescriptor(string methodName)
		{
			var testDescriptor = new TestRuleMethodDescriptor(methodName);
			_testRuleMethodDescriptors.Add(testDescriptor);
			return testDescriptor;
		}
	}
}
