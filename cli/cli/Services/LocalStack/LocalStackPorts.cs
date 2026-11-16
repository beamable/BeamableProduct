using System.Text.RegularExpressions;

namespace cli.Services.LocalStack;

/// <summary>
/// Moves a step whose port something else holds to the next free one, the way <c>beam local management</c> finds
/// its own, and repoints everything that reaches it: the step's own URLs and environment, the portal URL, and for
/// the gateways what the rest of the backend calls them through (Caddy, the C# hosts' <c>Legacy:Uri</c>, Scala's
/// <c>beamableApi</c> URI). Only steps that declare a <see cref="LocalStackStep.port"/> move; the docker ports and
/// Caddy's 8080 are baked into BeamableAPI and the Mongo seed data, so they keep the pre-launch conflict error.
/// </summary>
public static class LocalStackPorts
{
	public const string ScalaGatewayStep = "scala: gateway";
	public const string AnalyticsGatewayStep = "scala: analytics-gateway";

	/// <summary>What BeamableAPI's compose passes to Caddy for each upstream it proxies (routes.caddy defaults them).</summary>
	public const string CaddyGatewayPort = "BEAM_LOCAL_GATEWAY_PORT",
		CaddyScalaGatewayPort = "BEAM_LOCAL_SCALA_GATEWAY_PORT",
		CaddyAnalyticsGatewayPort = "BEAM_LOCAL_ANALYTICS_GATEWAY_PORT";

	/// <summary>How far above its own port a step looks for a free one before giving up (and failing as before).</summary>
	private const int SearchSpan = 100;

	private static readonly Regex LocalPort = new Regex(@"(?<=\b(?:localhost|127\.0\.0\.1):)\d+(?!\d)", RegexOptions.Compiled);

	/// <summary>
	/// A free port for each of <paramref name="taken"/>: the first above its own that <paramref name="isTaken"/> says
	/// is free and no step of <paramref name="steps"/> declares. A step with nothing free nearby is left out.
	/// </summary>
	public static Dictionary<string, int> Choose(IEnumerable<LocalStackStep> steps, IEnumerable<LocalStackStep> taken, Func<int, bool> isTaken)
	{
		var claimed = steps.Where(s => s.port > 0).Select(s => s.port).ToHashSet();
		var moves = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (var step in taken)
		{
			var port = step.port + 1;
			while (port < step.port + SearchSpan && port <= 65535 && (claimed.Contains(port) || isTaken(port)))
			{
				port++;
			}

			if (port >= step.port + SearchSpan || port > 65535)
			{
				continue;
			}

			claimed.Add(port);
			moves[step.name] = port;
		}

		return moves;
	}

	/// <summary>Moves each step named in <paramref name="moves"/> to its port, and repoints whatever reaches it.</summary>
	public static void Apply(List<LocalStackStep> steps, LocalStackConfig config, IReadOnlyDictionary<string, int> moves)
	{
		var byPort = new Dictionary<int, int>();
		var moved = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (var step in steps.Where(s => s.port > 0 && moves.TryGetValue(s.name, out var to) && to != s.port))
		{
			byPort[step.port] = moves[step.name];
			moved[step.name] = moves[step.name];
		}

		if (byPort.Count == 0)
		{
			return;
		}

		string Rewrite(string text) => text == null
			? null
			: LocalPort.Replace(text, m => byPort.TryGetValue(int.Parse(m.Value), out var to) ? to.ToString() : m.Value);

		foreach (var step in steps)
		{
			step.arguments = Rewrite(step.arguments);
			step.readyWhenHttp200 = Rewrite(step.readyWhenHttp200);
			step.readyWhenHttpOk = Rewrite(step.readyWhenHttpOk);
			if (step.environment != null)
			{
				foreach (var key in step.environment.Keys.ToList())
				{
					step.environment[key] = Rewrite(step.environment[key]);
				}
			}

			if (moved.TryGetValue(step.name, out var port))
			{
				step.port = port;
			}
		}

		config.portalUrl = Rewrite(config.portalUrl);

		IEnumerable<LocalStackStep> Named(Func<string, bool> match) => steps.Where(s => s.name != null && match(s.name));
		bool IsScalaService(string name) => name.StartsWith("scala:", StringComparison.OrdinalIgnoreCase) && name != "scala: redis";
		bool IsCSharpHost(string name) => name.StartsWith("c# ", StringComparison.OrdinalIgnoreCase);

		if (moved.TryGetValue(LocalStackTemplate.PortalFrontendStepName, out var portalPort))
		{
			// Vite's --port wins over its config; its strictPort stays on, so a race for this one still fails loudly.
			foreach (var portal in Named(n => n == LocalStackTemplate.PortalFrontendStepName))
			{
				portal.arguments += $" -- --port {portalPort}";
			}
		}

		if (moved.TryGetValue(LocalStackDependencies.GatewayStep, out var gatewayPort))
		{
			var url = $"http://localhost:{gatewayPort}";
			foreach (var gateway in Named(n => n == LocalStackDependencies.GatewayStep))
			{
				// The gateway takes Kestrel's default 5000 (no ASPNETCORE_URLS of its own), and names itself in Gateway:BaseUrl.
				Env(gateway)["ASPNETCORE_URLS"] = url;
				Env(gateway)["Gateway__BaseUrl"] = url;
			}

			foreach (var scala in Named(IsScalaService))
			{
				AddJvmProperty(scala, "beamableApi.env.local.uri", url);
			}

			CaddyUpstream(steps, CaddyGatewayPort, gatewayPort);
		}

		if (moved.TryGetValue(ScalaGatewayStep, out var scalaGatewayPort))
		{
			foreach (var gateway in Named(n => n == ScalaGatewayStep))
			{
				AddJvmProperty(gateway, "service.port", scalaGatewayPort.ToString());
			}

			foreach (var host in Named(IsCSharpHost))
			{
				Env(host)["Legacy__Uri"] = $"http://localhost:{scalaGatewayPort}";
			}

			CaddyUpstream(steps, CaddyScalaGatewayPort, scalaGatewayPort);
		}

		if (moved.TryGetValue(AnalyticsGatewayStep, out var analyticsPort))
		{
			foreach (var gateway in Named(n => n == AnalyticsGatewayStep))
			{
				AddJvmProperty(gateway, "service.port", analyticsPort.ToString());
			}

			CaddyUpstream(steps, CaddyAnalyticsGatewayPort, analyticsPort);
		}
	}

	private static Dictionary<string, string> Env(LocalStackStep step) => step.environment ??= new Dictionary<string, string>();

	/// <summary>The Scala services read system properties over their bundled config, and every JVM reads JAVA_TOOL_OPTIONS,
	/// so this reaches the JVM without touching the launch script baked into the settings.</summary>
	private static void AddJvmProperty(LocalStackStep step, string key, string value)
	{
		var env = Env(step);
		env.TryGetValue("JAVA_TOOL_OPTIONS", out var options);
		env["JAVA_TOOL_OPTIONS"] = ((options ?? string.Empty) + $" -D{key}={value}").Trim();
	}

	/// <summary>Caddy runs in the backend's docker step; compose hands it the port (and recreates it when that changes).</summary>
	private static void CaddyUpstream(List<LocalStackStep> steps, string variable, int port)
	{
		foreach (var docker in steps.Where(s => s.name == LocalStackTemplate.ApiDepsStepName))
		{
			Env(docker)[variable] = port.ToString();
		}
	}
}
