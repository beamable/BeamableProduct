using System;

namespace Beamable.Common.BeamCli
{
	[AttributeUsage(validOn: AttributeTargets.Struct | AttributeTargets.Class )]
	public class CliContractTypeAttribute : Attribute
	{
		
	}
}
