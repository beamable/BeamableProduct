using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using cli.Utils;
using JetBrains.Annotations;
using System.Text.Json;

namespace cli.Services.Content;

public class ContentLocalCache
{
	public string ManifestId { get; }
	public Dictionary<string, ContentDocument> Assets => _localAssets;
	public string ContentDirPath => Path.Combine(BaseDirPath, "Content");
	private string BaseDirPath => _configService.ConfigFilePath;

	private Dictionary<string, ContentDocument> _localAssets;
	private TagsLocalFile _localTags;
	private CliRequester _requester;

	private ClientManifest Manifest { get; set; }

	private readonly ConfigService _configService;

	public ContentLocalCache(ConfigService configService, string manifestId, CliRequester requester)
	{
		ManifestId = manifestId;
		_requester = requester;
		_configService = configService;
	}

	public string[] GetTags(string contentId) => _localTags.TagsForContent(contentId);

	public List<LocalContent> GetLocalContentStatus()
	{
		if (Manifest == null)
		{
			throw new CliException("Cannot show current local status due to missing Manifest information.");
		}
		var resultList = new List<LocalContent>();

		foreach (var pair in Assets.Where(pair => Manifest.entries.All(info => info.contentId != pair.Key)))
		{
			resultList.Add(new LocalContent
			{
				contentId = pair.Key, status = ContentStatus.Created, tags = _localTags.TagsForContent(pair.Key)
			});
		}

		foreach (ClientContentInfo contentManifestEntry in Manifest.entries)
		{
			ContentStatus localStatus;
			var contentExistsLocally = Assets.ContainsKey(contentManifestEntry.contentId);
			if (contentExistsLocally)
			{
				var sameTags = _localTags.TagsForContent(contentManifestEntry.contentId)
					.All(contentManifestEntry.tags.Contains);
				localStatus = HasSameVersion(contentManifestEntry) && sameTags
					? ContentStatus.UpToDate
					: ContentStatus.Modified;
			}
			else
			{
				localStatus = ContentStatus.Deleted;
			}

			var tags = contentExistsLocally
				? _localTags.TagsForContent(contentManifestEntry.contentId)
				: contentManifestEntry.tags;
			resultList.Add(new LocalContent
			{
				contentId = contentManifestEntry.contentId, status = localStatus, tags = tags
			});
		}

		resultList.Sort((a, b) => a.status.CompareTo(b.status));

		return resultList;
	}

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

		_localAssets = new Dictionary<string, ContentDocument>();

		foreach (var path in Directory.EnumerateFiles(ContentDirPath, "*json"))
		{
			var content = ContentDocument.AtPath(path);
			_localAssets.Add(content.id, content);
		}

		_localTags = TagsLocalFile.ReadFromDirectory(BaseDirPath, ManifestId);
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

	public void UpdateTags(TagsLocalFile tags)
	{
		_localTags = tags;
		_localTags.WriteToFile(BaseDirPath);
	}

	public void Remove(LocalContent content)
	{
		var path = GetContentPath(content.contentId);
		File.Delete(path);
	}

	public string GetContentPath(string id) => Path.Combine(ContentDirPath, $"{id}.json");

	public async Promise<ClientManifest> GetManifest()
	{
		if (Manifest != null)
		{
			return Manifest;
		}

		string url = $"{ContentService.SERVICE}/manifest/public?id={ManifestId}";

		Manifest = await _requester.Request(Method.GET, url, null, true, ClientManifest.ParseCSV, true).Recover(ex =>
		{
			if (ex is RequesterException { Status: 404 })
			{
				return new ClientManifest { entries = new List<ClientContentInfo>() };
			}

			throw ex;
		}).ShowLoading();
		return Manifest;
	}
	public async Promise<Dictionary<string, ManifestReferenceSuperset>> BuildLocalManifestReferenceSupersets()
	{
		var manifest = await GetManifest();
		var localContents = GetLocalContentStatus();
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
		var tags = GetTags(contentId);
		
		return new ContentDefinition
		{
			id = document!.id,
			checksum = document.CalculateChecksum(),
			properties =
				JsonSerializer.Serialize(document.properties, new JsonSerializerOptions { WriteIndented = false }),
			tags = tags,
			lastChanged = 0
		};
	}
}
