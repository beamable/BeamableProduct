using Beamable.Common.Content;
using Beamable.Serialization;
using JetBrains.Annotations;

namespace cli.Services.Content;

public class ManifestReferenceSuperset : JsonSerializable.ISerializable
{
	public string Type;
	public string Id;
	public string Version;
	public string Uri;
	public long Created;
	public string TypeName => Id.Substring(0, Id.LastIndexOf('.'));
	[CanBeNull] public string[] Tags;
	[CanBeNull] public string Checksum;
	[CanBeNull] public string Visibility;
	public long LastChanged;

	public void Serialize(JsonSerializable.IStreamSerializer s)
	{
		s.Serialize("type", ref Type);
		s.Serialize("id", ref Id);
		s.Serialize("version", ref Version);
		s.Serialize("uri", ref Uri);
		s.Serialize("created", ref Created);
		s.Serialize("lastChanged", ref LastChanged);


		if (Tags != null)
		{
			s.SerializeArray("tags", ref Tags);
		}

		s.Serialize("checksum", ref Checksum);
		s.Serialize("visibility", ref Visibility);
	}

	public string Key => MakeKey(Id, Visibility);

	public static string MakeKey(string id, string visibility)
	{
		return $"{id}/{visibility}";
	}

	public static ManifestReferenceSuperset CreateFromDefinition(ContentDefinition definition, [CanBeNull] ClientContentInfo info, bool isPublic = true)
	{
		return new ManifestReferenceSuperset
		{
			Type = "content",
			Id = definition.id,
			Version = info?.version ?? string.Empty,
			Uri = info?.uri ?? string.Empty,
			Created = 0,
			Tags = definition.tags,
			Checksum = definition.checksum,
			Visibility = isPublic ? "public" : "private",
			LastChanged = definition.lastChanged
		};
	}
}
