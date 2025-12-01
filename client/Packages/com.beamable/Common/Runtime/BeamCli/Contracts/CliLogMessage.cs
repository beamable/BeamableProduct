// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

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

		public static CliLogMessage FromStringNow(string message, string level="Info")
		{
			return new CliLogMessage
			{
				message = message, logLevel = level, timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			};
		}
	}

	[CliContractType]
	[Serializable]
	public class TelemetryReportStatus
	{
		public string FilePath;
		public bool Success;
		public string ErrorMessage;
	}
}
