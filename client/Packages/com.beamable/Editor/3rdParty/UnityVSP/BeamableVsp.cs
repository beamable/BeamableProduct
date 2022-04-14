using Beamable;

namespace UnityEditor.VspAttribution.Beamable
{
	public static class BeamableVsp
	{
		public static void TryToEmitAttribution(string action, string cid)
		{
			if (!BeamableEnvironment.IsUnityVsp) return;
			if (string.IsNullOrEmpty(action)) return;
			if (string.IsNullOrEmpty(cid)) return;

			VspAttribution.SendAttributionEvent(
				action,
				"beamable",
				cid);
		}
	}
}
