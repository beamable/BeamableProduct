﻿using Beamable.Common;
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
	private const string FILENAME_FORMAT = "contentTags_{0}.json";
	public string ManifestId { get; }
	public Dictionary<string, string[]> localTags = new();
	public Dictionary<string, string[]> remoteTags = new();
	public string Filename => string.Format(FILENAME_FORMAT, ManifestId);
	public string FullPath => Path.Combine(_configDir, Filename);

	ContentTags(string manifestId, string configDir)
	{
		_configDir = configDir;
		ManifestId = manifestId;
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

	public void AddTagToContent(string contentId, string tag)
	{
		var list = localTags.TryGetValue(tag, out string[] tags) ? tags.ToList() : new List<string>();

		if(list.Any(contentId.Equals))
			return;

		list.Add(contentId);
		localTags[tag] = list.ToArray();
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
		var path = Path.Combine(_configDir, Filename);
		var json = JsonConvert.SerializeObject(localTags, Formatting.Indented);
		File.WriteAllText(path, json, Encoding.UTF8);
	}

	public static ContentTags ReadFromDirectory(string configDir, string manifestId)
	{
		var tagsLocalFile = new ContentTags(manifestId, configDir);

		if (string.IsNullOrWhiteSpace(configDir) || !File.Exists(tagsLocalFile.FullPath))
		{
			BeamableLogger.LogWarning("Tags file not found, using empty one");
			return tagsLocalFile;
		}

		try
		{
			var jsonContent = File.ReadAllText(tagsLocalFile.FullPath);
			tagsLocalFile.localTags = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonContent);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to read \"{tagsLocalFile.FullPath}\" tag file. Exception: {e.Message}");
		}

		return tagsLocalFile;
	}
}
