// this file was copied from nuget package Beamable.Server.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Server.Common/3.0.0-PREVIEW.RC6

using System;

namespace Beamable.Server
{
	[System.AttributeUsage(System.AttributeTargets.Parameter)]
	public class ParameterAttribute : Attribute
	{
		public string ParameterNameOverride { get; set; }
		public ParameterAttribute(string parameterName = null)
		{
			ParameterNameOverride = parameterName;
		}
	}
}
