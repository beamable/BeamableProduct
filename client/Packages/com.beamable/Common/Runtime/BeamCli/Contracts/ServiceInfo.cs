// this file was copied from nuget package Beamable.Common@5.3.0
// https://www.nuget.org/packages/Beamable.Common/5.3.0

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
