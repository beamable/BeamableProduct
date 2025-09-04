using System;
using System.Collections.Generic;

namespace Beamable.Common.BeamCli.Contracts
{
	[Serializable]
	public class TelemetryReportStatus
	{
		public string FilePath;
		public bool Success;
		public string ErrorMessage;
	}

	[Serializable]
	public class CliOtelMessage
	{
		public List<CliOtelLogRecord> allLogs;
	}

	[Serializable]
	public class CliOtelLogRecord
	{
		public string Timestamp { get; set; }
		public string LogLevel { get; set; } // needs to be either of ["Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"]
		public string Body { get; set; }
		public string ExceptionMessage { get; set; }
		public string ExceptionStackTrace { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
	}
}
