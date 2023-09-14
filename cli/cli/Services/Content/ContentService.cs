using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Serialization.SmallerJSON;
using Spectre.Console;
using System.Text;

namespace cli.Services.Content;

public class ContentService
{
	public const string SERVICE = "/basic/content";
	const int DEFAULT_TABLE_LIMIT = 100;
	private readonly CliRequester _requester;
	private readonly ConfigService _config;
	private readonly Dictionary<string, ContentLocalCache> _localCaches = new();

	public ContentLocalCache GetLocalCache(string manifestId)
	{
		if (_localCaches.TryGetValue(manifestId, out ContentLocalCache localCache))
		{
			return localCache;
		}

		var newLocalCache = new ContentLocalCache(_config, manifestId, _requester);
		newLocalCache.Init();
		_localCaches.Add(newLocalCache.ManifestId, newLocalCache);

		return _localCaches[manifestId];
	}

	public ContentService(CliRequester requester, ConfigService config)
	{
		_requester = requester;
		_config = config;
	}

	public Promise<ClientManifest> GetManifest(string manifestId)
	{
		if (string.IsNullOrWhiteSpace(manifestId))
		{
			throw new CliException($"This is not a valid manifestID: \"{manifestId}\"");
		}

		return GetLocalCache(manifestId).UpdateManifest();
	}

	public Promise<List<ContentDocument>> PullContent(string manifestId, bool saveToDisk = true)
	{
		return GetLocalCache(manifestId).PullContent(saveToDisk);
	}

	public async Task DisplayStatusTable(string manifestId, bool showUpToDate, int limit, int skipAmount)
	{
		var contentCache = GetLocalCache(manifestId);
		var localContentStatus = await contentCache.GetLocalContentStatus();
		var totalCount = localContentStatus.Count;
		var table = new Table();
		table.AddColumn("Current status");
		table.AddColumn("ID");
		table.AddColumn(new TableColumn("tags").RightAligned());

		if (!showUpToDate)
		{
			localContentStatus = localContentStatus.Where(content => content.status != ContentStatus.UpToDate).ToList();
		}

		if (!showUpToDate && localContentStatus.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]Your local content is up to date with remote.[/]");
			return;
		}


		var range = localContentStatus.Skip(skipAmount).Take(limit > 0 ? limit : DEFAULT_TABLE_LIMIT).ToList();
		foreach (var content in range)
		{
			var tags = contentCache.GetContentTagsStatus(content.contentId).Select(pair =>
			{
				switch (pair.Value)
				{
					case TagStatus.LocalOnly:
						return $"[green][[+]]{pair.Key}[/]";
					case TagStatus.RemoteOnly:
						return $"[red][[-]]{pair.Key}[/]";
					case TagStatus.LocalAndRemote:
						return pair.Key;
					default:
						throw new ArgumentOutOfRangeException();
				}
			});
			table.AddRow(content.StatusString(), content.contentId, string.Join(",", tags));
		}
		AnsiConsole.Write(table);
		AnsiConsole.WriteLine($"Content: {range.Count} out of {totalCount}");
	}


	public async Promise<ContentManifest> PublishContentAndManifest(string manifestId)
	{
		if (string.IsNullOrWhiteSpace(manifestId))
		{
			throw new CliException($"This is not a valid manifestID: \"{manifestId}\"");
		}

		var contentSaveResponse = await PublishChangedContent(manifestId);
		var contentManifest = await PublishNewManifest(contentSaveResponse, manifestId);

		return contentManifest;
	}

	private async Promise<ContentManifest> PublishNewManifest(ContentSaveResponse contentSaveResponse, string manifestId)
	{
		var localCache = GetLocalCache(manifestId);

		var referenceSet = await localCache.BuildLocalManifestReferenceSupersets();
		contentSaveResponse.content.ForEach(entry =>
		{
			var reference = new ManifestReferenceSuperset
			{
				Checksum = entry.checksum,
				Id = entry.id,
				Tags = entry.tags,
				Uri = entry.uri,
				Version = entry.version,
				Visibility = entry.visibility,
				Type = "content",
				LastChanged = entry.lastChanged
			};
			var key = reference.Key;

			referenceSet[key] = reference;
		});
		var manifestRequest = new ManifestSaveRequest { id = manifestId, references = referenceSet.Values.ToList() };
		return await _requester.RequestJson<ContentManifest>(Method.POST, $"/basic/content/manifest?id={manifestId}",
			manifestRequest);
	}

	private async Promise<ContentSaveResponse> PublishChangedContent(string manifestId)
	{
		var contentLocal = GetLocalCache(manifestId);
		var localContent = await contentLocal.GetLocalContentStatus();
		var changedContent = localContent
			.Where(content => content.status is not (ContentStatus.Deleted or ContentStatus.UpToDate))
			.Select(content => contentLocal.PrepareContentForPublish(content.contentId)).ToList();


		var dict = new ArrayDict { { "content", changedContent } };
		var reqJson = Json.Serialize(dict, new StringBuilder());

		return await _requester.Request<ContentSaveResponse>(Method.POST, "/basic/content", reqJson);
	}
}
