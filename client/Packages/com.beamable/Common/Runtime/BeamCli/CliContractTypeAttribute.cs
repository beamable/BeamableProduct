using System;

namespace Beamable.Common.BeamCli
{
	[AttributeUsage(validOn: AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
	public class CliContractTypeAttribute : Attribute
	{

	}
}
