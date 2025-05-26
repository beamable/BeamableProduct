using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using cli.Utils;
using Errata;
using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace cli.Services.Content;

public class ContentLocalCache
{
	public string ManifestId { get; }
	public IDictionary<string, ContentDocument> Assets => _localAssets;
	public string ContentDirPath => Path.Combine(BaseDirPath, ManifestId);
	public ContentTags Tags => _contentTags;

	private string BaseDirPath => Path.Combine(_configService.ConfigDirectoryPath!, Constants.CONTENT_DIRECTORY);

	private ConcurrentDictionary<string, ContentDocument> _localAssets;
	private ContentTags _contentTags;
	private ClientManifest _manifest;
	private readonly CliRequester _requester;
	private readonly ConfigService _configService;

	public ContentLocalCache(ConfigService configService, string manifestId, CliRequester requester)
	{
		ManifestId = manifestId;
		_requester = requester;
		_configService = configService;
	}

	public IEnumerable<string> ContentMatchingRegex(string pattern)
	{
		try
		{
			var regex = new Regex(pattern);

			return _localAssets.Keys.Where(id => regex.IsMatch(id));
		}
		catch (ArgumentException)
		{
			BeamableLogger.LogError("{Pattern} is not a valid regex!", pattern);
		}

		return new List<string>();
	}

	public Dictionary<string, TagStatus> GetContentTagsStatus(string contentId) =>
		_contentTags.GetContentAllTagsStatus(contentId);

	public async Promise<List<LocalContent>> GetLocalContentStatus()
	{
		if (_manifest == null)
		{
			_ = await UpdateManifest();
		}

		var resultList = new List<LocalContent>();

		foreach (var pair in Assets.Where(pair => _manifest.entries.All(info => info.contentId != pair.Key)))
		{
			resultList.Add(new LocalContent
			{
				contentId = pair.Key,
				status = ContentStatus.Created,
				tags = _contentTags.TagsForContent(pair.Key, false),
				hash = pair.Value.CalculateChecksum()
			});
		}

		foreach (ClientContentInfo contentManifestEntry in _manifest.entries)
		{
			ContentStatus localStatus;
			var contentExistsLocally = Assets.ContainsKey(contentManifestEntry.contentId);
			if (contentExistsLocally)
			{
				var sameTags = _contentTags.GetContentAllTagsStatus(contentManifestEntry.contentId)
					.All(pair => pair.Value == TagStatus.LocalAndRemote);
				localStatus = HasSameVersion(contentManifestEntry) && sameTags
					? ContentStatus.UpToDate
					: ContentStatus.Modified;
			}
			else
			{
				localStatus = ContentStatus.Deleted;
			}

			var tags = _contentTags.TagsForContent(contentManifestEntry.contentId, !contentExistsLocally);
			resultList.Add(new LocalContent
			{
				contentId = contentManifestEntry.contentId,
				status = localStatus,
				tags = tags,
				hash = contentManifestEntry.version,
			});
		}

		resultList.Sort((a, b) => a.status.CompareTo(b.status));

		return resultList;
	}

	public bool HasContent(string contentId) => Assets.TryGetValue(contentId, out _);

	public bool HasSameVersion(ClientContentInfo contentInfo)
	{
		if (Assets.TryGetValue(contentInfo.contentId, out var localVersion))
		{
			var local = localVersion.CalculateChecksum();
			return local == contentInfo.version;
		}

		return false;
	}

	[CanBeNull]
	public ContentDocument GetContent(string id)
	{
		var content = ContentDocument.AtPath(GetContentPath(id));
		return content;
	}


	public void Init()
	{
		if (_localAssets != null)
			return;

		if (!Directory.Exists(ContentDirPath))
		{
			Directory.CreateDirectory(ContentDirPath);
		}

		_localAssets = new ConcurrentDictionary<string, ContentDocument>();

		foreach (var path in Directory.EnumerateFiles(ContentDirPath, "*json"))
		{
			try
			{
				var content = ContentDocument.AtPath(path);
				_localAssets.TryAdd(content.id, content);
			}
			catch (JsonException e)
			{
				var loc = new Location((int)e.LineNumber.GetValueOrDefault(0),
					(int)e.BytePositionInLine.GetValueOrDefault(0));
				var label = new Label(path, loc, e.Message).WithNote("Edit content file and try again.");
				var extraReport = new Diagnostic("Content file does not contain valid JSON")
					.WithLabel(label);
				throw new CliException("Invalid content JSON",
					Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_INVALID_CONTENT,
					true, null, new[] { extraReport });
			}
		}

		_contentTags = ContentTags.ReadFromDirectory(BaseDirPath, ManifestId);
	}

	public async Task UpdateContent(ContentDocument result)
	{
		var path = Path.Combine(ContentDirPath, $"{result.id}.json");
		if (result.properties != null)
		{
			var value = JsonSerializer.Serialize(result.properties.Value,
				new JsonSerializerOptions { WriteIndented = true });
			_localAssets[result.id] = result;
			await File.WriteAllTextAsync(path, value);
		}
	}

	public async Promise UpdateTags(bool saveToFile = true)
	{
		_ = await UpdateManifest();

		if (saveToFile)
		{
			_contentTags.UpdateRemoteTagsInfo(_manifest, true);
			_contentTags.WriteToFile();
		}
	}

	public void Remove(LocalContent content)
	{
		var path = GetContentPath(content.contentId);
		File.Delete(path);
	}

	public string GetContentPath(string id) => Path.Combine(ContentDirPath, $"{id}.json");

	public async Promise<ClientManifest> UpdateManifest(bool forceUpdate = false)
	{
		if (_manifest != null && !forceUpdate)
		{
			return _manifest;
		}

		string url = $"{ContentService.SERVICE}/manifest/public?id={ManifestId}";

		_manifest = await _requester.Request(Method.GET, url, null, true, ClientManifest.ParseCSV, true).Recover(ex =>
		{
			if (ex is RequesterException { Status: 404 })
			{
				return new ClientManifest { entries = new List<ClientContentInfo>() };
			}

			throw ex;
		}).ShowLoading();

		_contentTags.UpdateRemoteTagsInfo(_manifest, false);

		return _manifest;
	}

	public async Promise<Dictionary<string, ManifestReferenceSuperset>> BuildLocalManifestReferenceSupersets()
	{
		var manifest = await UpdateManifest();
		var localContents = await GetLocalContentStatus();
		var dict = new Dictionary<string, ManifestReferenceSuperset>();

		foreach (var localContent in
				 localContents.Where(content => content.status != ContentStatus.Deleted))
		{
			var definition = PrepareContentForPublish(localContent.contentId);
			var matchingContent = manifest.entries.FirstOrDefault(info => info.contentId.Equals(definition.id));
			var publicVersion = ManifestReferenceSuperset.CreateFromDefinition(definition, matchingContent, true);
			var privateVersion = ManifestReferenceSuperset.CreateFromDefinition(definition, matchingContent, false);
			dict.Add(publicVersion.Key, publicVersion);
			dict.Add(privateVersion.Key, privateVersion);
		}

		return dict;
	}

	public ContentDefinition PrepareContentForPublish(string contentId)
	{
		var document = GetContent(contentId);
		var tags = _contentTags.TagsForContent(contentId, false);

		return new ContentDefinition
		{
			id = document!.id,
			checksum = document.CalculateChecksum(),
			properties = JsonSerializer.Serialize(document.properties, new JsonSerializerOptions { WriteIndented = false } ),
			tags = tags,
			lastChanged = 0
		};
	}

	public async Promise<List<ContentDocument>> PullContent(bool saveToDisk = true)
	{
		var manifest = await UpdateManifest();
		var contents = new List<ContentDocument>(manifest.entries.Count);

		// Add the unchanged content files directly to the list of content documents
		var localContent = manifest.entries.Where(HasSameVersion).ToArray();
		contents.AddRange(localContent.Select(c => GetContent(c.contentId)));
		
		// Download and overwrite the local content for things that have changed based on the hash. 
		{
			var requiresDownloadContent = manifest.entries.Except(localContent).ToArray();
			var downloadPromises = requiresDownloadContent.Select(async c =>
			{
				var content = await _requester.CustomRequest(Method.GET, c.uri, parser: s => JsonSerializer.Deserialize<ContentDocument>(s));
				if (saveToDisk)
				{
					await UpdateContent(content);
				}

				return content;
			}).ToArray();

			var downloadedContent = await Task.WhenAll(downloadPromises);
			contents.AddRange(downloadedContent);
		}

		// Return the updated list of content
		return contents;
	}
	
	public async Promise<List<ContentDocument>> PullContent(IEnumerable<string> toDownload, bool saveToDisk = true)
	{
		var manifest = await UpdateManifest();
		var contents = new List<ContentDocument>(manifest.entries.Count);

		// Download and overwrite the local content for things that have changed based on the hash. 
		{
			var requiresDownloadContent = manifest.entries.Where(e => toDownload.Contains(e.contentId)).ToArray();
			var downloadPromises = requiresDownloadContent.Select(async c =>
			{
				var content = await _requester.CustomRequest(Method.GET, c.uri, parser: s => JsonSerializer.Deserialize<ContentDocument>(s));
				if (saveToDisk)
				{
					await UpdateContent(content);
				}

				return content;
			}).ToArray();

			var downloadedContent = await Task.WhenAll(downloadPromises);
			contents.AddRange(downloadedContent);
		}

		// Return the updated list of content
		return contents;
	}

	public async Promise RemoveLocalOnlyContent()
	{
		var localContents = await GetLocalContentStatus();

		var localOnlyContent = localContents.Where(content => content.status == ContentStatus.Created);
		foreach (LocalContent localContent in localOnlyContent)
		{
			Remove(localContent);
		}
	}
}
