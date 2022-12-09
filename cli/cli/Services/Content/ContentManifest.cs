using Beamable.Serialization;

namespace cli.Services.Content;


/// <summary>
/// This type defines a %Beamable %Manifest %Difference
/// 
/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
/// 
/// #### Related Links
/// - See the <a target="_blank" href="https://docs.beamable.com/docs/content-feature">Content</a> feature documentation
/// 
/// ![img beamable-logo]
/// 
/// </summary>
[System.Serializable]
public class ContentManifest : JsonSerializable.ISerializable
{
	public string id;
	public long created;
	public List<ContentManifestReference> references;
	public string checksum;

	public void Serialize(JsonSerializable.IStreamSerializer s)
	{
		s.Serialize(nameof(id), ref id);
		s.Serialize(nameof(created), ref created);
		s.SerializeList(nameof(references), ref references);
	}
}

/// <summary>
/// This type defines a %Beamable %Manifest %Reference
/// 
/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
/// 
/// #### Related Links
/// - See Beamable.Editor.Content.ContentManifest script reference
/// 
/// ![img beamable-logo]
/// 
/// </summary>
[System.Serializable]
public class ContentManifestReference : JsonSerializable.ISerializable
{
	public string id;
	public string version;
	public string type;
	public string[] tags;
	public string uri;
	public string checksum;
	public string visibility;
	public long created;
	public long lastChanged;

	public void Serialize(JsonSerializable.IStreamSerializer s)
	{
		s.Serialize(nameof(id), ref id);
		s.Serialize(nameof(version), ref version);
		s.Serialize(nameof(type), ref type);
		s.SerializeArray(nameof(tags), ref tags);
		s.Serialize(nameof(uri), ref uri);
		s.Serialize(nameof(checksum), ref checksum);
		s.Serialize(nameof(visibility), ref visibility);
		s.Serialize(nameof(created), ref created);
		s.Serialize(nameof(lastChanged), ref lastChanged);
	}
}
