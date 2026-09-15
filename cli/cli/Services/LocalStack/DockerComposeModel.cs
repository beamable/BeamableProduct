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
	public static DockerComposeModel TryLoad(string scalaDir) =>
		TryLoadFile(Path.Combine(scalaDir, "docker", "local", "docker-compose.yml"));

	/// <summary>Parses a specific compose file, or returns null if it is missing/unreadable.</summary>
	public static DockerComposeModel TryLoadFile(string composeFilePath)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(composeFilePath) || !File.Exists(composeFilePath))
				return null;

			var deserializer = new DeserializerBuilder()
				.IgnoreUnmatchedProperties()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			return deserializer.Deserialize<DockerComposeModel>(File.ReadAllText(composeFilePath));
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// The fixed host ports every service in this file publishes — what a pre-launch check has to test, since
	/// docker fails the whole <c>compose up</c> when it cannot bind even one of them. Entries without a fixed host
	/// port (a bare container port, which docker maps to a random one) and port ranges are skipped.
	/// </summary>
	public IEnumerable<int> PublishedHostPorts()
	{
		if (services == null)
			yield break;

		foreach (var service in services.Values)
		{
			if (service?.ports == null)
				continue;

			foreach (var entry in service.ports)
			{
				var port = TryParseHostPort(entry);
				if (port > 0)
					yield return port;
			}
		}
	}

	/// <summary>
	/// The host port from a compose <c>ports</c> entry, or 0 when there isn't a fixed one. Handles
	/// <c>"61616:61616"</c>, <c>"127.0.0.1:61616:61616"</c> and a trailing <c>/tcp</c>|<c>/udp</c>. Returns 0 for a
	/// bare <c>"61616"</c> (that publishes the CONTAINER port on a host port docker picks, so there is nothing
	/// fixed to check) and for ranges like <c>"9000-9010:9000-9010"</c>.
	/// </summary>
	public static int TryParseHostPort(string entry)
	{
		if (string.IsNullOrWhiteSpace(entry))
			return 0;

		var spec = entry.Trim();
		var slash = spec.IndexOf('/');
		if (slash >= 0)
			spec = spec.Substring(0, slash);

		var parts = spec.Split(':', StringSplitOptions.TrimEntries);
		if (parts.Length < 2)
			return 0;

		// "HOST:CONTAINER" or "IP:HOST:CONTAINER" — the host port is always the second-to-last field.
		return int.TryParse(parts[^2], out var port) && port > 0 && port <= 65535 ? port : 0;
	}
}

public class DockerComposeService
{
	public string[] profiles = Array.Empty<string>();

	/// <summary>The raw <c>ports</c> entries (<c>"61616:61616"</c>, …). Read via <see cref="DockerComposeModel.PublishedHostPorts"/>.</summary>
	public string[] ports = Array.Empty<string>();

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
