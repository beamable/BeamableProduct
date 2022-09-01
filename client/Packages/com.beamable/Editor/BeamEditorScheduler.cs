using Beamable.Common;
using System.Collections;
using Unity.EditorCoroutines.Editor;

namespace Beamable.Editor
{
	public class BeamEditorScheduler : IScheduler
	{
		public Promise Delay()
		{
			var p = new Promise();
			EditorDebouncer.SetTimeout(() => { p.CompleteSuccess(); }, 0);

			return p;
		}
	}
}
