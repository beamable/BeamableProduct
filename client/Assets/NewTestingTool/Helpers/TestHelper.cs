using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NewTestingTool.Helpers
{
	public static class TestHelper
	{
		public static bool IsAsyncMethod(Type classType, string methodName)
		{
			MethodInfo methodInfo = classType.GetMethod(methodName);
			return IsAsyncMethod(methodInfo);
		}
		public static bool IsAsyncMethod(MethodInfo methodInfo)
		{
			var attType = typeof(AsyncStateMachineAttribute);
			var attrib = (AsyncStateMachineAttribute)methodInfo.GetCustomAttribute(attType);
			return attrib != null;
		}
	}
}
