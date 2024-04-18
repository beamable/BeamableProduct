using Beamable.Common;
using Beamable.Common.Spew;
using System.Diagnostics;
using System.Linq;

namespace Beamable.Server.Editor.Usam
{
	[SpewLogger]
	public class UsamLogger
	{
		[Conditional(Constants.Features.Spew.SPEW_ALL), Conditional(Constants.Features.Spew.SPEW_USAM)]
		public static void Log(params object[] msg)
		{
			Logger.DoSimpleSpew("USAM: " + string.Join(",", msg.Select(x => x?.ToString())));
		}
	}
}

