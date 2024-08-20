using System;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public static class TimeDisplayUtil
	{
		/// <summary>
		/// Gets the date time from the time a log happened
		/// </summary>
		/// <param name="logTime">The time a log happened in seconds since the startup of Unity</param>
		/// <returns></returns>
		public static string GetLogDisplayTime(float logTime)
		{
			var currentTime = Time.realtimeSinceStartup;
			var timePassed = currentTime - logTime;
			var logDateTime = DateTime.Now.Subtract(new TimeSpan(0, 0, Convert.ToInt32(timePassed)));
			return logDateTime.ToLongTimeString();
		}
	}
}
