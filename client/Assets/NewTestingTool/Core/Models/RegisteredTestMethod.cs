using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace NewTestingTool.Core.Models
{
	internal class RegisteredTestMethod
	{
		public TestResult TestResult { get; set; } = TestResult.NotSet;
		public Testable Testable { get; }
		public MethodInfo MethodInfo { get; }
		public object[] Arguments { get; }

		public RegisteredTestMethod(Testable testable, MethodInfo methodInfo, object[] arguments)
		{
			Testable = testable;
			MethodInfo = methodInfo;
			Arguments = arguments;
		}
		public async Task InvokeTest(bool displayLogs, int orderIndex, int caseIndex, Action<TestResult> finishedTest)
		{
			if (displayLogs)
				TestableDebug.Log($"Invoking test: Testable=[{Testable.GetType().Name}] Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{MethodInfo.Name}]");

			var result = IsAsyncMethod(MethodInfo)
				? await (Task<TestResult>)MethodInfo.Invoke(Testable, Arguments)
				: (TestResult)MethodInfo.Invoke(Testable, Arguments);

			finishedTest?.Invoke(result);
		}
		public void Reset() => TestResult = TestResult.NotSet;
		
		private static bool IsAsyncMethod(MethodInfo methodInfo)
		{
			var attType = typeof(AsyncStateMachineAttribute);
			var attrib = (AsyncStateMachineAttribute)methodInfo.GetCustomAttribute(attType);
			return attrib != null;
		}
	}
}
