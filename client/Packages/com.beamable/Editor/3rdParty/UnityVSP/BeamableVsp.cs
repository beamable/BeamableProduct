using Beamable;

namespace UnityEditor.VspAttribution.Beamable
{
	public static class BeamableVsp
	{
		public static void TryToEmitAttribution()
		{
			if (!BeamableEnvironment.IsUnityVsp) return;
			VspAttribution.SendAttributionEvent(
				"login",
				"beamable",
				BeamableEnvironment.UnityVspId);
		}
	}
}
