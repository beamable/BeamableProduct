using System.Collections.Generic;

namespace NewTestingTool.Core.Models
{
	internal class RegisteredTest
	{
		public int Order { get; }
		public List<RegisteredTestMethod> RegisteredMethodTests { get; } = new List<RegisteredTestMethod>();

		public RegisteredTest(int order)
		{
			Order = order;
		}
		public void Reset()
		{
			foreach (var registeredMethodTest in RegisteredMethodTests)
				registeredMethodTest.Reset();
		}
	}
}
