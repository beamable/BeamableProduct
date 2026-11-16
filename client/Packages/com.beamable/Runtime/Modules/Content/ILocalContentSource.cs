using System;
using Beamable.Common;
using Beamable.Common.Content;

namespace Beamable.Content
{
	public interface ILocalContentSource
	{
		bool HandlesManifest(string manifestId);

		/// <summary>Return isolated metadata, filtering before copying when a query is supplied.</summary>
		Promise<ClientManifest> GetManifest(string manifestId, ContentQuery query = null);

		Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestId);
	}
}
