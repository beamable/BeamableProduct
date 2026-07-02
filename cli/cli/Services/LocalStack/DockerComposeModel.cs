using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cli.Services.LocalStack;

/// <summary>
/// A minimal view of <c>docker/local/docker-compose.yml</c> — just enough to read the per-service
/// <c>profiles</c> and the custom <c>x-beam-services</c> metadata (basic/object service names) added by
/// BeamableBackend PR#632. Ported from #4258 <c>DockerComposeModel.cs</c>.
/// </summary>
public class DockerComposeModel
{
	public Dictionary<string, DockerComposeService> services = new Dictionary<string, DockerComposeService>();

	/// <summary>Parses the local docker-compose file, or returns null if it is missing/unreadable.</summary>
	public static DockerComposeModel TryLoad(string scalaDir)
	{
		try
		{
			var path = Path.Combine(scalaDir, "docker", "local", "docker-compose.yml");
			if (!File.Exists(path))
				return null;

			var deserializer = new DeserializerBuilder()
				.IgnoreUnmatchedProperties()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			return deserializer.Deserialize<DockerComposeModel>(File.ReadAllText(path));
		}
		catch
		{
			return null;
		}
	}
}

public class DockerComposeService
{
	public string[] profiles = Array.Empty<string>();

	[YamlMember(Alias = "depends_on", ApplyNamingConventions = false)]
	public string[] dependsOn = Array.Empty<string>();

	/// <summary>
	/// The <c>x-beam-services</c> block: keys are <c>basic</c>/<c>object</c>, values are the service-name
	/// lists that container provides. A declared-but-empty entry (e.g. <c>basic:</c> with no list) parses as a
	/// null value, which still marks the container as a provider of that kind.
	/// </summary>
	[YamlMember(Alias = "x-beam-services", ApplyNamingConventions = false)]
	public Dictionary<string, string[]> beamServices;

	public bool HasProfile(string profile) =>
		profiles != null && profiles.Any(p => string.Equals(p, profile, StringComparison.OrdinalIgnoreCase));
}
