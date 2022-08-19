using System;
using System.Reflection;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models.Descriptors
{
	[Serializable]
	public class TestRuleMethodDescriptor
	{
		public string MethodName => _methodName;
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

		[SerializeField] private string _methodName;
		[SerializeField] private string _title;
		[SerializeField] private string _description;

		public TestRuleMethodDescriptor(string methodName)
		{
			_methodName = methodName;
		}

		public TestRuleMethodDescriptor(MethodInfo methodInfo)
		{
			_methodName = methodInfo.Name;
		}
	}
}
