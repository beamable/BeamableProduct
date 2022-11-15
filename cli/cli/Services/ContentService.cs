using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Spectre.Console;
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
		_contentLocal.Init();
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

	public async Task DisplayStatusTable()
	{
		var contentManifest = await GetManifest();
		var table = new Table();
		table.AddColumn("ID");
		table.AddColumn("Current status");
		table.AddColumn(new TableColumn("tags").RightAligned());
		foreach (ClientContentInfo contentManifestEntry in contentManifest.entries)
		{
			if (_contentLocal.HasSameVersion(contentManifestEntry))
			{
				table.AddRow(contentManifestEntry.contentId, "Up to date", string.Join(",",contentManifestEntry.tags));
			}else if (_contentLocal.Assets.ContainsKey(contentManifestEntry.contentId))
			{
				table.AddRow(contentManifestEntry.contentId, "[yellow]Different content[/]", string.Join(",",contentManifestEntry.tags));
			}
			else
			{
				table.AddRow(contentManifestEntry.contentId, "[red]Remote only[/]", string.Join(",",contentManifestEntry.tags));
			}
		}

		foreach (var pair in _contentLocal.Assets.Where(pair => contentManifest.entries.All(info => info.contentId != pair.Key)))
		{
			table.AddRow(pair.Key, "[green]Local only[/]", string.Empty);
		}
		AnsiConsole.Write(table);
	}
}
