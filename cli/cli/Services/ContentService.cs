using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace cli.Services;

public class ContentService
{
	// string url = $"/basic/content/manifest/public?id={ManifestId}";
	const string SERVICE = "/basic/content";
	private readonly CliRequester _requester;
	private readonly ContentLocalCache _contentLocal;

	public ContentService(CliRequester requester, ContentLocalCache contentLocal)
	{
		_requester = requester;
		_contentLocal = contentLocal;
	}

	public Promise<ClientManifest> GetManifest(string manifestId = "global")
	{
		string url = $"{SERVICE}/manifest/public?id={manifestId}";
		return _requester.Request(Method.GET, url, null, true, ClientManifest.ParseCSV, true).Recover(ex =>
		{
			if (ex is RequesterException { Status: 404 })
			{
				return new ClientManifest { entries = new List<ClientContentInfo>() };
			}

			throw ex;
		});
	}

	public async Promise<List<ContentDocument>> PullContent(ClientManifest manifest, bool saveToDisk = true)
	{
		_contentLocal.Init();
		var contents = new List<ContentDocument>(manifest.entries.Count);
		
		foreach (var contentInfo in manifest.entries)
		{
			if(_contentLocal.HasSameVersion(contentInfo))
			{
				contents.Add(await _contentLocal.GetContent(contentInfo.contentId));
				continue;
			}
			try
			{
				var result = await _requester.CustomRequest(Method.GET, contentInfo.uri, parser: s => JsonSerializer.Deserialize<ContentDocument>(s));
				contents.Add(result);
				_contentLocal.UpdateContent(result);
			}
			catch (Exception e)
			{
				BeamableLogger.LogException(e);
			}
		}
		return contents;
	}
}
