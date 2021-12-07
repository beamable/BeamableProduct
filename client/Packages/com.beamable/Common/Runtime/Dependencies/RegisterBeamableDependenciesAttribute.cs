// unset

using System;

namespace Beamable.Common.Dependencies
{
	[AttributeUsage(validOn: AttributeTargets.Method)]
	public class RegisterBeamableDependenciesAttribute : Attribute
	{
		public int Order { get; set; }

		public RegisterBeamableDependenciesAttribute(int order = 0)
		{
			Order = order;
		}
	}
}
