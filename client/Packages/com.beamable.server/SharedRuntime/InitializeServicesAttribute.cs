using System;

namespace Beamable.Server
{
	[AttributeUsage(AttributeTargets.Method)]
	public class InitializeServicesAttribute : Attribute
	{
		public int ExecutionOrder;

		public InitializeServicesAttribute(int executionOrder = 0)
		{
			ExecutionOrder = executionOrder;
		}
	}
}
