using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using System.Collections.Generic;

namespace Beamable.Content
{
	public interface IManifestResolver
	{
		/// <summary>
		/// Produces a <see cref="ClientManifest"/>
		/// Based on the implementation, this could fetch the manifest from the remote Realm, or use local content.
		/// See <see cref="DefaultManifestResolver.ResolveManifest"/> and <see cref="LocalManifestResolver.ResolveManifest"/>
		/// </summary>
		Promise<ClientManifest> ResolveManifest(IBeamableRequester requester,
													  string url,
													  ManifestSubscription subscription);
	}

	public class DefaultManifestResolver : IManifestResolver
	{
		/// <summary>
		/// Downloads a manifest from the current remote realm.
		/// </summary>
		public Promise<ClientManifest> ResolveManifest(IBeamableRequester requester, string url, ManifestSubscription subscription)
		{
			return requester.Request(Method.GET, url, null, false, ClientManifest.ParseCSV, true).Recover(ex =>
			{
				// TODO: Put "global" as a constant value somewhere. Currently it lives in a different asm, and its too much trouble.
				if (ex is PlatformRequesterException err && err.Status == 404 && subscription.ManifestID.Equals("global"))
				{
					return new ClientManifest
					{
						entries = new List<ClientContentInfo>()
					};
				}

				throw ex;
			});
		}
	}
}
