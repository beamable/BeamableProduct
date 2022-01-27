using Beamable.Common.Content;
using System;

namespace Beamable.Common.Api.Content
{
	public interface IContentApi : ISupportsGet<ClientManifest>
	{
		Promise<IContentObject> GetContent(string contentId, string manifestID = "");
		Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestID = "");
		Promise<IContentObject> GetContent(IContentRef reference, string manifestID = "");
		Promise<TContent> GetContent<TContent>(IContentRef reference, string manifestID = "") where TContent : ContentObject, new();
		Promise<TContent> GetContent<TContent>(IContentRef<TContent> reference, string manifestID = "") where TContent : ContentObject, new();

		Promise<ClientManifest> GetManifestWithID(string manifestID = "");
		Promise<ClientManifest> GetManifest(string filter = "", string manifestID = "");
		Promise<ClientManifest> GetManifest(ContentQuery query, string manifestID = "");
	}

	public static class ContentApi
	{
		// TODO: This is very hacky, but it lets use inject a different service in. Replace with ServiceManager (lot of unity deps to think about)
		public static Promise<IContentApi> Instance = new Promise<IContentApi>();
	}
}
