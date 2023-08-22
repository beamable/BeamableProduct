using Beamable.Common.Content;
using JetBrains.Annotations;
using System.Text.Json;

namespace cli.Services.Content;

public class ContentLocalCache
{
	private readonly IAppContext _context;
	private Dictionary<string, ContentDocument> _localAssets;
	private TagsLocalFile _localTags;
	public string ManifestId { get; set; } = "global";
	public string ContentDirPath => Path.Combine(BaseDirPath, ManifestId);
	private string BaseDirPath => Path.Combine(_context.WorkingDirectory, Constants.CONFIG_FOLDER, "Content");

	public Dictionary<string, ContentDocument> Assets => _localAssets;

	public Dictionary<string, ClientManifest> Manifests => _manifests;

	private readonly Dictionary<string, ClientManifest> _manifests = new();

	public ContentLocalCache(IAppContext context)
	{
		_context = context;
	}

	public string[] GetTags(string contentId) => _localTags.TagsForContent(contentId);

	public void UpdateManifest(string manifestId, ClientManifest manifest)
	{
		_manifests[manifestId] = manifest;
	}

	public List<LocalContent> GetLocalContentStatus(ClientManifest manifest)
	{
		var resultList = new List<LocalContent>();

		foreach (var pair in Assets.Where(pair => manifest.entries.All(info => info.contentId != pair.Key)))
		{
			resultList.Add(new LocalContent
			{
				contentId = pair.Key, status = ContentStatus.Created, tags = _localTags.TagsForContent(pair.Key)
			});
		}

		foreach (ClientContentInfo contentManifestEntry in manifest.entries)
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


	public void Init(string manifestId = "global")
	{
		if (_localAssets != null)
			return;
		ManifestId = manifestId;
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

	public async Task UpdateContent(ContentDocument result, ClientContentInfo info)
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
}
