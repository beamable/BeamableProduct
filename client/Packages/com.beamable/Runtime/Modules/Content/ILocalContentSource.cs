using System;
using Beamable.Common;
using Beamable.Common.Content;

namespace Beamable.Content
{
	public interface ILocalContentSource
	{
		bool HandlesManifest(string manifestId);

		Promise<ClientManifest> GetManifest(string manifestId);

		Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestId);
	}
}
