// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC4

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
}
