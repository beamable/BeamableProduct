using System;

namespace Beamable.NewTestingTool.Attributes
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class TestRuleAttribute : Attribute
	{
		public int ExecutionOrder { get; }
		public object[] Arguments { get; }

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
		public TestRuleAttribute(int executionOrder, object arg1, object arg2, object arg3, object arg4)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2, arg3, arg4 }; 
		}
		public TestRuleAttribute(int executionOrder, object arg1, object arg2, object arg3, object arg4, object arg5)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2, arg3, arg4, arg5 }; 
		}
		public TestRuleAttribute(int executionOrder, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2, arg3, arg4, arg5, arg6 }; 
		}
		public TestRuleAttribute(int executionOrder, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2, arg3, arg4, arg5, arg6, arg7 }; 
		}
		public TestRuleAttribute(int executionOrder, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }; 
		}
		public TestRuleAttribute(int executionOrder, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
		{
			ExecutionOrder = executionOrder;
			Arguments = new[]{ arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 }; 
		}
	}
}
