using Beamable.Common;
using Beamable.Common.Spew;
using System.Diagnostics;
using System.Linq;

namespace Beamable.Editor.BeamCli
{
	[SpewLogger]
	public class CliLogger
	{
		[Conditional(Constants.Features.Spew.SPEW_ALL), Conditional(Constants.Features.Spew.SPEW_CLI)]
		public static void Log(params object[] msg)
		{
			Logger.DoSpew("CLI: " + string.Join(",", msg.Select(x => x?.ToString())));
		}
	}
}
