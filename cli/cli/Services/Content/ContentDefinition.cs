using Beamable.Serialization.SmallerJSON;
using System.Text;

namespace cli.Services.Content;

[System.Serializable]
public class ContentDefinition : IRawJsonProvider
{
	public string id;
	public string checksum;
	public string properties;
	public string[] tags;
	public long lastChanged;

	public string ToJson()
	{
		var dict = new ArrayDict
		{
			{"id", id},
			{"checksum", checksum},
			{"tags", tags},
			{"lastChanged", lastChanged},
			{"properties", new RawValue(properties)},
		};

		var json = Json.Serialize(dict, new StringBuilder());
		return json;
	}
}

public class RawValue : IRawJsonProvider
{
	private readonly string _json;

	public RawValue(string json)
	{
		_json = json;
	}
	public string ToJson()
	{
		return _json;
	}
}

[System.Serializable]
public class ContentSaveResponse
{
	public List<ContentReference> content;
}

[System.Serializable]
public class ContentReference
{
	public string id, version, uri, checksum, visibility;
	public long lastChanged;
	public string[] tags;
}
