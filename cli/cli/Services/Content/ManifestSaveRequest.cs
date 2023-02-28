using Beamable.Serialization;

namespace cli.Services.Content;

[Serializable]
public class ManifestSaveRequest : JsonSerializable.ISerializable
{
	public string id;
	public List<ManifestReferenceSuperset> references;
	public void Serialize(JsonSerializable.IStreamSerializer s)
	{
		s.Serialize("id", ref id);
		s.SerializeList("references", ref references);
	}
}
