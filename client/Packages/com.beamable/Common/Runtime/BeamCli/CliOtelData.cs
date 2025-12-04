// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;
using System.Collections.Generic;

namespace Beamable.Common.BeamCli.Contracts
{

	[Serializable]
	public class CliOtelMessage
	{
		public List<CliOtelLogRecord> allLogs;
	}

	[Serializable]
	public class CliOtelLogRecord
	{
		public string Timestamp;
		public string LogLevel; // needs to be either of ["Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"]
		public string Body;
		public string ExceptionMessage;
		public string ExceptionStackTrace;
		public Dictionary<string, string> Attributes;
	}
}
