using Beamable.Serialization;

namespace cli.Services.Content;

public class ManifestSaveRequest : JsonSerializable.ISerializable
{
	public string Id;
	public List<ManifestReferenceSuperset> References;
	public void Serialize(JsonSerializable.IStreamSerializer s)
	{
		s.Serialize("id", ref Id);
		s.SerializeList("references", ref References);
	}
}
