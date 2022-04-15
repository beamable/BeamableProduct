using Beamable;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Editor;
using System;

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

		public static async Promise<VspMetadata> GetLatestVersion()
		{
			var api = await EditorAPI.Instance;
			var res = await api.Requester.ManualRequest<VspVersionResponse>(Method.GET, "http://beamable-vsp.beamable.com/vsp-meta.json");
			var metadata = new VspMetadata {storeUrl = res.storeUrl};
			try
			{
				PackageVersion version = res.version;
				metadata.version = version;
			}
			catch
			{
				metadata.version = new PackageVersion(0,0,0);
			}

			return metadata;
		}

		[Serializable]
		public class VspVersionResponse
		{
			public string version;
			public string storeUrl;
		}

		public class VspMetadata
		{
			public PackageVersion version;
			public string storeUrl;
		}
	}
}
