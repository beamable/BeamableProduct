// unset

using System;

namespace Beamable.Common.Dependencies
{
	/// <summary>
	/// Use this attribute to register custom services when Beamable starts up.
	/// You should use this on a method that takes one parameter of type <see cref="IDependencyBuilder"/>.
	/// Add whatever services you want to on the builder instance. Any service you register will exist for each BeamContext.
	/// </summary>
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
