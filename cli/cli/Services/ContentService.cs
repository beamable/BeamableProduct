using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Spectre.Console;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace cli.Services;

public class ContentService
{
	const string SERVICE = "/basic/content";
	private readonly CliRequester _requester;
	private readonly ContentLocalCache _contentLocal;
	public ContentLocalCache ContentLocal
	{
		get
		{
			_contentLocal.Init();
			return _contentLocal;
		}
	}

	public ContentService(CliRequester requester, ContentLocalCache contentLocal)
	{
		_requester = requester;
		_contentLocal = contentLocal;
	}

	public Promise<ClientManifest> GetManifest(string manifestId)
	{
		if (string.IsNullOrWhiteSpace(manifestId))
		{
			manifestId = "global";
		}
		if (ContentLocal.Manifests.ContainsKey(manifestId))
		{
			var promise = new Promise<ClientManifest>();
			promise.CompleteSuccess(ContentLocal.Manifests[manifestId]);
			return promise;
		}
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

	public void UpdateTags(ClientManifest manifest)
	{
		Dictionary<string, List<string>> tags = new();
		foreach (ClientContentInfo clientContentInfo in manifest.entries)
		{
			foreach (string tag in clientContentInfo.tags)
			{
				if (!tags.ContainsKey(tag))
				{
					tags[tag] = new List<string>();
				}
				tags[tag].Add(clientContentInfo.contentId);
			}
		}

		var localTags = new TagsLocalFile(tags);
		_contentLocal.UpdateTags(localTags);
	}

	public async Promise<List<ContentDocument>> PullContent(ClientManifest manifest, bool saveToDisk = true)
	{
		var contents = new List<ContentDocument>(manifest.entries.Count);
		
		foreach (var contentInfo in manifest.entries)
		{
			if(ContentLocal.HasSameVersion(contentInfo))
			{
				contents.Add(ContentLocal.GetContent(contentInfo.contentId));
				continue;
			}
			try
			{
				var result = await _requester.CustomRequest(Method.GET, contentInfo.uri, parser: s => JsonSerializer.Deserialize<ContentDocument>(s));
				contents.Add(result);
				if(saveToDisk)
				{
					await ContentLocal.UpdateContent(result);
				}
			}
			catch (Exception e)
			{
				BeamableLogger.LogException(e);
			}
		}
		return contents;
	}

	public async Task DisplayStatusTable(string manifestId, bool skipUpToDate, int limit)
	{
		var contentManifest = await GetManifest(manifestId);
		var localContentStatus = ContentLocal.GetLocalContentStatus(contentManifest);
		var table = new Table();
		table.AddColumn("Current status");
		table.AddColumn("ID");
		table.AddColumn(new TableColumn("tags").RightAligned());

		if (skipUpToDate)
		{
			localContentStatus = localContentStatus.Where(content => content.status != ContentStatus.UpToDate).ToList();
		}
		var range = limit > 0 && limit < localContentStatus.Count ? limit : localContentStatus.Count;

		foreach (var content in localContentStatus.GetRange(0,range))
		{
			table.AddRow(content.StatusString(), content.contentId, string.Join(",",content.tags));
		}
		AnsiConsole.Write(table);
	}
}
