using System.Reflection;

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
		public void InvokeTest(bool displayLogs = false, int orderIndex = 0, int caseIndex = 0)
		{
			if (displayLogs)
				TestableDebug.Log($"Invoking test: Testable=[{Testable.GetType().Name}] Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{MethodInfo.Name}]");
			MethodInfo.Invoke(Testable, Arguments);
		}
		public void Reset() => TestResult = TestResult.NotSet;
	}
}
