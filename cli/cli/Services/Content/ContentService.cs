using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Newtonsoft.Json;
using Spectre.Console;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace cli.Services.Content;

public class ContentService
{
	const int DEFAULT_TABLE_LIMIT = 100;
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
			if (ContentLocal.HasSameVersion(contentInfo))
			{
				contents.Add(ContentLocal.GetContent(contentInfo.contentId));
				continue;
			}

			try
			{
				var result = await _requester.CustomRequest(Method.GET, contentInfo.uri,
					parser: s => JsonSerializer.Deserialize<ContentDocument>(s));
				contents.Add(result);
				if (saveToDisk)
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

	public async Task DisplayStatusTable(string manifestId, bool showUpToDate, int limit, int skipAmount)
	{
		var contentManifest = await GetManifest(manifestId);
		var localContentStatus = ContentLocal.GetLocalContentStatus(contentManifest);
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
		range.ForEach(
			content => table.AddRow(content.StatusString(), content.contentId, string.Join(",", content.tags)));
		AnsiConsole.Write(table);
		AnsiConsole.WriteLine($"Content: {range.Count} out of {totalCount}");
	}


	public async Promise<ContentManifest> PublishContentAndManifest(string manifestId)
	{
		if (string.IsNullOrWhiteSpace(manifestId))
		{
			manifestId = "global";
		}

		var clientManifest = await GetManifest(manifestId);
		var contentSaveResponse = await PublishChangedContent(clientManifest);
		var contentManifest = await PublishNewManifest(contentSaveResponse, clientManifest, manifestId);

		return contentManifest;
	}

	private async Promise<ContentManifest> PublishNewManifest(ContentSaveResponse contentSaveResponse,
		ClientManifest manifest, string manifestId)
	{
		var localContent = ContentLocal
			.GetLocalContentStatus(manifest)
			.Where(content => content.status is not ContentStatus.Deleted).ToList();
		var referenceSet = BuildLocalManifestReferenceSupersets(localContent, manifest);
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

			if (referenceSet.ContainsKey(key))
			{
				referenceSet[key] = reference;
			}
			else
			{
				referenceSet.Add(key, reference);
			}
		});
		var manifestRequest = new ManifestSaveRequest { id = manifestId, references = referenceSet.Values.ToList() };
		return await _requester.RequestJson<ContentManifest>(Method.POST, $"/basic/content/manifest?id={manifestId}",
			manifestRequest);
	}

	private async Promise<ContentSaveResponse> PublishChangedContent(ClientManifest contentManifest)
	{
		var localContent = ContentLocal.GetLocalContentStatus(contentManifest)
			.Where(content => content.status is not (ContentStatus.Deleted or ContentStatus.UpToDate))
			.Select(content => PrepareContentForPublish(_contentLocal.GetContent(content.contentId))).ToList();


		var dict = new ArrayDict { { "content", localContent.ToList() } };
		var reqJson = Json.Serialize(dict, new StringBuilder());

		return await _requester.Request<ContentSaveResponse>(Method.POST, "/basic/content", reqJson);
	}

	Dictionary<string, ManifestReferenceSuperset> BuildLocalManifestReferenceSupersets(List<LocalContent> localContents,
		ClientManifest currentManifest)
	{
		var dict = new Dictionary<string, ManifestReferenceSuperset>();
		foreach (var doc in
		         localContents.Where(content => content.status != ContentStatus.Deleted)
			         .Select(localContent => _contentLocal.GetContent(localContent.contentId)))
		{
			var definition = PrepareContentForPublish(doc);
			var matchingContent = currentManifest.entries.FirstOrDefault(info => info.contentId.Equals(definition.id));
			var publicVersion = ManifestReferenceSuperset.CreateFromDefinition(definition, matchingContent, true);
			var privateVersion = ManifestReferenceSuperset.CreateFromDefinition(definition, matchingContent, false);
			dict.Add(publicVersion.Key, publicVersion);
			dict.Add(privateVersion.Key, privateVersion);
		}

		return dict;
	}

	ContentDefinition PrepareContentForPublish(ContentDocument document)
	{
		return new ContentDefinition
		{
			id = document.id,
			checksum = document.CalculateChecksum(),
			properties =
				JsonSerializer.Serialize(document.properties, new JsonSerializerOptions { WriteIndented = false }),
			tags = _contentLocal.GetTags(document.id),
			lastChanged = 0
		};
	}
}
