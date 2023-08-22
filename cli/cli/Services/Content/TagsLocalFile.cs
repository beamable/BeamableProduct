using Beamable.Common;
using Newtonsoft.Json;
using System.Text;

namespace cli.Services.Content;

[Serializable]
public class TagsLocalFile
{
	private const string FILENAME_FORMAT = "localTags_{0}.json";
	public string ManifestId { get; init; }
	public Dictionary<string, string[]> tags = new();
	private string Filename => string.Format(FILENAME_FORMAT, ManifestId);

	public TagsLocalFile()
	{ }

	public TagsLocalFile(Dictionary<string, List<string>> dict , string manifestId)
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

	public void WriteToFile(string dir)
	{
		var path = Path.Combine(dir, Filename);
		var json = JsonConvert.SerializeObject(this, Formatting.Indented);
		File.WriteAllText(path, json, Encoding.UTF8);
	}

	public static TagsLocalFile ReadFromDirectory(string dir, string manifestId)
	{
		var tagsLocalFile = new TagsLocalFile { ManifestId = manifestId };
		
		var path = Path.Combine(dir, tagsLocalFile.Filename);
		if (string.IsNullOrWhiteSpace(dir) || !File.Exists(path))
		{
			BeamableLogger.LogWarning("Tags file not found, using empty one");
			return tagsLocalFile;
		}
		var jsonContent = File.ReadAllText(path);
		tagsLocalFile = JsonConvert.DeserializeObject<TagsLocalFile>(jsonContent);
		return tagsLocalFile;
	}
}
