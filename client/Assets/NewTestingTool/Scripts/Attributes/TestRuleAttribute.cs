using System;

namespace NewTestingTool.Attributes
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class TestRuleAttribute : Attribute
	{
		public int ExecutionOrder { get; }
		public object[] Arguments { get; }

		public TestRuleAttribute(int executionOrder, params object[] arguments)
		{
			ExecutionOrder = executionOrder;
			Arguments = arguments;
		}
		public TestRuleAttribute(int executionOrder)
		{
			ExecutionOrder = executionOrder;
			Arguments = null;
		}
		public TestRuleAttribute(int executionOrder, object arg1)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1 };
		}
		public TestRuleAttribute(int executionOrder, object arg1, object arg2)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2 };
		}
		public TestRuleAttribute(int executionOrder, object arg1, object arg2, object arg3)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2, arg3 }; 
		}
	}
}
