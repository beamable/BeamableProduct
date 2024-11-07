// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

using System;

namespace Beamable.Common.BeamCli
{
	[AttributeUsage(validOn: AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
	public class CliContractTypeAttribute : Attribute
	{

	}
}
