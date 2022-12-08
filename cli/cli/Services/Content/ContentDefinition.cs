namespace cli.Services.Content;

public class ContentDefinition
{
	public string id;
	public string checksum;
	public string properties;
	public string[] tags;
	public long lastChanged;
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
