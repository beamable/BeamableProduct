// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC6

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
