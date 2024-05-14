// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType]
	[Serializable]
	public class EnvironmentVersionData
	{
		public string nugetPackageVersion;
	}

}
