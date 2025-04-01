// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC3
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC3

using System;

namespace Beamable.Common.BeamCli
{
	[AttributeUsage(validOn: AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
	public class CliContractTypeAttribute : Attribute
	{

	}
}
