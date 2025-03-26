// this file was copied from nuget package Beamable.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Common/4.1.5

using System;
using System.Collections.Generic;

namespace Beamable.Common.BeamCli.Contracts
{
	[Serializable]
	[CliContractType]
	public class GenerateEnvFileOutput
	{
		public List<EnvVarOutput> envVars = new List<EnvVarOutput>();
	}

	[Serializable]
	[CliContractType]
	public class EnvVarOutput
	{
		public string name;
		public string value;
		public static EnvVarOutput Create(string name, string value) => new EnvVarOutput { name = name, value = value };
	}
}
