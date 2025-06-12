using Beamable.Common;
using Beamable.Common.Spew;
using System.Diagnostics;
using System.Linq;

namespace Beamable.Server.Editor.Usam
{
	[SpewLogger]
	public class UsamLogger
	{
		private static long _previousStopWatchSeconds;
		private static Stopwatch _stopwatch = new Stopwatch();
		
		[Conditional(Constants.Features.Spew.SPEW_ALL), Conditional(Constants.Features.Spew.SPEW_USAM)]
		public static void Log(params object[] msg)
		{
			if (_stopwatch.IsRunning)
			{
				var diff = _stopwatch.ElapsedMilliseconds - _previousStopWatchSeconds;
				Logger.DoSimpleSpew($"USAM: total=({_stopwatch.ElapsedMilliseconds}) elapsed=({diff})" + string.Join(",", msg.Select(x => x?.ToString())));
				_previousStopWatchSeconds = _stopwatch.ElapsedMilliseconds;

			}
			else
			{			
				Logger.DoSimpleSpew($"USAM: " + string.Join(",", msg.Select(x => x?.ToString())));
			}
		}
		
		[Conditional(Constants.Features.Spew.SPEW_ALL), Conditional(Constants.Features.Spew.SPEW_USAM)]
		public static void ResetLogTimer()
		{
			_stopwatch.Restart();
			_previousStopWatchSeconds = 0;
		}
		
		[Conditional(Constants.Features.Spew.SPEW_ALL), Conditional(Constants.Features.Spew.SPEW_USAM)]
		public static void StopLogTimer()
		{
			_stopwatch.Stop();
			_previousStopWatchSeconds = 0;
		}
	}

}

