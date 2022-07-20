using NewTestingTool.Helpers;
using System.Reflection;
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
		public async Task<TestResult> InvokeTest(bool displayLogs, int orderIndex, int caseIndex)
		{
			if (displayLogs)
				TestableDebug.Log($"Invoking test: Testable=[{Testable.GetType().Name}] Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{TestableDebug.WrapWithColor(MethodInfo.Name, Color.yellow)}]");

			TestResult = TestHelper.IsAsyncMethod(MethodInfo)
				? await (Task<TestResult>)MethodInfo.Invoke(Testable, Arguments)
				: (TestResult)MethodInfo.Invoke(Testable, Arguments);
			
			return TestResult;
		}
		public void Reset() => TestResult = TestResult.NotSet;
	}
}
