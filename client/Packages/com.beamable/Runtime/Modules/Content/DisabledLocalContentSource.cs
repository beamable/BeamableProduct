using System;
using Beamable.Common;
using Beamable.Common.Content;

namespace Beamable.Content
{
	public class DisabledLocalContentSource : ILocalContentSource
	{
		public bool HandlesManifest(string manifestId) => false;

		public Promise<ClientManifest> GetManifest(string manifestId)
		{
			return Promise<ClientManifest>.Failed(new InvalidOperationException("Local content resolution is unavailable."));
		}

		public Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestId)
		{
			return Promise<IContentObject>.Failed(new InvalidOperationException("Local content resolution is unavailable."));
		}
	}
}
