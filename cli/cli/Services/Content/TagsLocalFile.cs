using Beamable.Common;
using Newtonsoft.Json;
using System.Text;

namespace cli.Services.Content;

[Serializable]
public class TagsLocalFile
{
	private const string FILENAME = "localTags.json";
	public Dictionary<string, string[]> tags = new();

	public TagsLocalFile()
	{ }

	public TagsLocalFile(Dictionary<string, List<string>> dict)
	{
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
		var path = Path.Combine(dir, FILENAME);
		var json = JsonConvert.SerializeObject(this, Formatting.Indented);
		File.WriteAllText(path, json, Encoding.UTF8);
	}

	public static TagsLocalFile ReadFromFile(string dir)
	{
		var path = Path.Combine(dir, FILENAME);
		if (string.IsNullOrWhiteSpace(dir) || !File.Exists(path))
		{
			BeamableLogger.LogWarning("Tags file not found, using empty one");
			return new TagsLocalFile();
		}
		var jsonContent = File.ReadAllText(path);

		return JsonConvert.DeserializeObject<TagsLocalFile>(jsonContent);
	}
}
