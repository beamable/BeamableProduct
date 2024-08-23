using System;

namespace Beamable.Editor.BeamCli.UI
{
	public static class TimeDisplayUtil
	{
		/// <summary>
		/// Gets the date time from the time a log happened
		/// </summary>
		/// <param name="logTime">The time a log happened in seconds since the startup of Unity</param>
		/// <param name="customFormat">A optional custom format for the time log</param>
		/// <returns>A formated date time based on the log time passed</returns>
		public static string GetLogDisplayTime(long logTime, string customFormat = "HH:mm:ss")
		{
			if (logTime < 0) return "";
			DateTime timeThen = DateTime.FromFileTime(logTime);

			// Return the formatted log time
			return timeThen.ToString(customFormat);
		}
	}
}
