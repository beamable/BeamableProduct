// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC3
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC3

using System;
using System.Collections.Generic;

namespace Beamable.Common.BeamCli.Contracts
{
	[Serializable]
	[CliContractType]
	public class ServiceInfo
	{
		public string name;
		public string projectPath;
	}
}
