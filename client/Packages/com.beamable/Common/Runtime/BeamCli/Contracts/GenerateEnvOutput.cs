using System;
using System.Collections.Generic;

namespace Beamable.Common.BeamCli.Contracts
{
	[Serializable]
	public class GenerateEnvFileOutput
	{
		public List<EnvVarOutput> envVars = new List<EnvVarOutput>();
	}

	[Serializable]
	public class EnvVarOutput
	{
		public string name;
		public string value;
		public static EnvVarOutput Create(string name, string value) => new EnvVarOutput { name = name, value = value };
	}
}
