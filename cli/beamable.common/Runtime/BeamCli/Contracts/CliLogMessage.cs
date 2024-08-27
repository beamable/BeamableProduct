using System;

namespace Beamable.Common.BeamCli.Contracts
{
	[CliContractType]
	[Serializable]
	public class CliLogMessage
	{
		public string logLevel;
		public string message;
		public long timestamp;
	}
}
