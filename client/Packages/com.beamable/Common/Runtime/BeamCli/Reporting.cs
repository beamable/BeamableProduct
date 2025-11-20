// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;

namespace Beamable.Common.BeamCli
{
	public static class Reporting
	{
		public const string MESSAGE_DELIMITER = "\n\n";
		public const string PATTERN = "__@#!REPORT!#@__";
		public const string PATTERN_START = "<" + PATTERN + ">";
		public const string PATTERN_END = "</" + PATTERN + ">";

		public static string EncodeMessage(string message)
		{
			return $"{PATTERN_START}{message}{PATTERN_END}";
		}

	}

	[Serializable]
	public class ReportDataPoint<T>
	{
		public long ts;
		public string type;
		public T data;
	}

	[Serializable]
	public class ReportDataPoint
	{
		public long ts;
		public string type;
		public object data;
	}

	[Serializable]
	public class ReportDataPointDescription
	{
		public string json;
		public string ts;
		public string type;
	}

	[Serializable]
	public class SampleNumber
	{
		public int x;
	}
}
