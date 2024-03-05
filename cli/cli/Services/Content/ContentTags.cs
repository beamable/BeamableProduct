using Beamable.Common;
using Beamable.Common.Content;
using Newtonsoft.Json;
using System.Text;

namespace cli.Services.Content;

public enum TagStatus
{
	LocalOnly,
	RemoteOnly,
	LocalAndRemote
}

public class ContentTags
{
	private readonly string _configDir;
	public string ManifestId { get; }
	public Dictionary<string, string[]> localTags = new();
	public Dictionary<string, string[]> remoteTags = new();

	ContentTags(string manifestId, string configDir)
	{
		_configDir = configDir;
		ManifestId = manifestId;
	}

	/// <summary>
	/// Returns the path of the content tags file.
	/// </summary>
	/// <returns>The path of the content tags file.</returns>
	public string GetPath()
	{
		var filename = string.Format(Constants.CONTENT_TAGS_FORMAT, ManifestId);
		return Path.Combine(_configDir, filename);
	}

	public void UpdateRemoteTagsInfo(ClientManifest manifest, bool overrideLocalTags)
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

		remoteTags = new();
		if (overrideLocalTags)
		{
			localTags = new();
		}

		foreach (string key in tags.Keys)
		{
			remoteTags[key] = tags[key].ToArray();
			if (overrideLocalTags)
			{
				localTags[key] = remoteTags[key];
			}
		}
	}

	public bool AddTagToContent(string contentId, string tag)
	{
		var list = localTags.TryGetValue(tag, out string[] tags) ? tags.ToList() : new List<string>();

		if (list.Any(contentId.Equals))
			return false;

		list.Add(contentId);
		localTags[tag] = list.ToArray();
		return true;
	}

	public bool RemoveTagFromContent(string contentId, string tag)
	{
		if (!localTags.ContainsKey(tag)) return false;
		var tagsLength = localTags[tag].Length;
		localTags[tag] = localTags[tag].ToList().Where(s => !s.Equals(contentId)).ToArray();
		return tagsLength != localTags[tag].Length;
	}

	public string[] TagsForContent(string contentId, bool isRemote)
	{
		var resultList = new List<string>();
		var tags = isRemote ? remoteTags.Keys : localTags.Keys;
		foreach (var tag in tags)
		{
			if (isRemote ? remoteTags[tag].Contains(contentId) : localTags[tag].Contains(contentId))
			{
				resultList.Add(tag);
			}
		}

		return resultList.ToArray();
	}

	public Dictionary<string, TagStatus> GetContentAllTagsStatus(string contentId)
	{
		var dict = new Dictionary<string, TagStatus>();
		var localContentTags = TagsForContent(contentId, false);
		var remoteContentTags = TagsForContent(contentId, true);
		foreach (string localTag in localContentTags)
		{
			var localAndRemote = remoteContentTags.Contains(localTag);
			dict.Add(localTag, localAndRemote ? TagStatus.LocalAndRemote : TagStatus.LocalOnly);
		}

		var remoteOnly = remoteContentTags.Where(tag => localContentTags.All(localTag => tag != localTag));
		foreach (var tag in remoteOnly)
			dict.Add(tag, TagStatus.RemoteOnly);

		return dict;
	}

	public void WriteToFile()
	{
		var path = GetPath();
		var json = JsonConvert.SerializeObject(localTags, Formatting.Indented);
		File.WriteAllText(path, json, Encoding.UTF8);
	}

	public static ContentTags ReadFromDirectory(string configDir, string manifestId)
	{
		var tagsLocalFile = new ContentTags(manifestId, configDir);
		var path = tagsLocalFile.GetPath();

		if (string.IsNullOrWhiteSpace(configDir) || !File.Exists(path))
		{
			var oldVersionPath = Path.Combine(configDir!, string.Format(Constants.OLD_CONTENT_TAGS_FORMAT, manifestId));
			if (!File.Exists(oldVersionPath))
			{
				BeamableLogger.LogWarning("Tags file not found, using empty one");
				return tagsLocalFile;
			}

			File.Move(oldVersionPath, path!);
		}

		try
		{
			var jsonContent = File.ReadAllText(path);
			tagsLocalFile.localTags = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonContent);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to read \"{path}\" tag file. Exception: {e.Message}");
		}

		return tagsLocalFile;
	}
}
