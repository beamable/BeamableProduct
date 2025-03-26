// this file was copied from nuget package Beamable.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Common/4.1.5

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
