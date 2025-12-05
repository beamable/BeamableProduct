using System;

namespace Beamable.Server
{
	[System.AttributeUsage(System.AttributeTargets.Parameter)]
	public class IgnoreSourceGeneratorAttribute : Attribute
	{
		
	}
	
	[System.AttributeUsage(System.AttributeTargets.Parameter)]
	public class ParameterAttribute : Attribute
	{
		public string ParameterNameOverride { get; set; }
		public ParameterSource Source { get; }

		public ParameterAttribute(string parameterName = null, ParameterSource source=ParameterSource.Body)
		{
			ParameterNameOverride = parameterName;
			Source = source;
		}
	}

	public class InjectAttribute : ParameterAttribute
	{
		public InjectAttribute() : base(source: ParameterSource.Injection)
		{
			
		}
	}

	public enum ParameterSource
	{
		Body,
		// TODO: add header support when the backend allows CORs, and when we can update the client-code gen
		// Header,
		Injection
	}
}
