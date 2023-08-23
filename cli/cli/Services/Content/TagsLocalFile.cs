using Beamable.Common;
using Newtonsoft.Json;
using System.Text;

namespace cli.Services.Content;

[Serializable]
public class TagsLocalFile
{
	private const string FILENAME_FORMAT = "contentTags_{0}.json";
	public string ManifestId { get; private init; }
	public Dictionary<string, string[]> tags = new();
	private string Filename => string.Format(FILENAME_FORMAT, ManifestId);

	public TagsLocalFile(Dictionary<string, List<string>> dict, string manifestId)
	{
		ManifestId = manifestId;
		foreach (string key in dict.Keys)
		{
			tags[key] = dict[key].ToArray();
		}
	}

	public string[] TagsForContent(string contentId)
	{
		var resultList = new List<string>();
		foreach (var tag in tags.Keys)
		{
			if (tags[tag].Contains(contentId))
			{
				resultList.Add(tag);
			}
		}

		return resultList.ToArray();
	}

	public void WriteToFile(string configDir)
	{
		var path = Path.Combine(configDir, Filename);
		var json = JsonConvert.SerializeObject(tags, Formatting.Indented);
		File.WriteAllText(path, json, Encoding.UTF8);
	}

	public static TagsLocalFile ReadFromDirectory(string configDir, string manifestId)
	{
		var tagsLocalFile = new TagsLocalFile(new(), manifestId);

		var path = Path.Combine(configDir, tagsLocalFile.Filename);
		if (string.IsNullOrWhiteSpace(configDir) || !File.Exists(path))
		{
			BeamableLogger.LogWarning("Tags file not found, using empty one");
			return tagsLocalFile;
		}

		try
		{
			var jsonContent = File.ReadAllText(path);
			tagsLocalFile.tags = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonContent);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to read \"{path}\" tag file. Exception: {e.Message}");
		}

		return tagsLocalFile;
	}
}
