using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Spectre.Console;
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

	public async Promise<List<ContentDocument>> PullContent(ClientManifest manifest, bool saveToDisk = true)
	{
		var contents = new List<ContentDocument>(manifest.entries.Count);
		
		foreach (var contentInfo in manifest.entries)
		{
			if(ContentLocal.HasSameVersion(contentInfo))
			{
				contents.Add(await ContentLocal.GetContent(contentInfo.contentId));
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

	public async Task DisplayStatusTable(string manifestId = "global", bool skipUpToDate = false)
	{
		var contentManifest = await GetManifest(manifestId);
		var table = new Table();
		table.AddColumn("Current status");
		table.AddColumn("ID");
		table.AddColumn(new TableColumn("tags").RightAligned());
		foreach (ClientContentInfo contentManifestEntry in contentManifest.entries)
		{
			if (ContentLocal.HasSameVersion(contentManifestEntry))
			{
				if(!skipUpToDate)
				{
					table.AddRow("Up to date", contentManifestEntry.contentId,
						string.Join(",", contentManifestEntry.tags));
				}
			}else if (ContentLocal.Assets.ContainsKey(contentManifestEntry.contentId))
			{
				table.AddRow("[yellow]Different content[/]", contentManifestEntry.contentId, string.Join(",",contentManifestEntry.tags));
			}
			else
			{
				table.AddRow("[red]Remote only[/]", contentManifestEntry.contentId, string.Join(",",contentManifestEntry.tags));
			}
		}

		foreach (var pair in ContentLocal.Assets.Where(pair => contentManifest.entries.All(info => info.contentId != pair.Key)))
		{
			table.AddRow("[green]Local only[/]", pair.Key, string.Empty);
		}
		AnsiConsole.Write(table);
	}
}
