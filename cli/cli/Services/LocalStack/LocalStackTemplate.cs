namespace cli.Services.LocalStack;

/// <summary>
/// Builds a reference <see cref="LocalStackConfig"/> manifest that mirrors the proven
/// <c>scripts/run-local-stack.sh</c> bring-up order:
///   1. C# Gateway + Caddy proxy (BeamableAPI)   2. Scala backing services (BeamableBackend tools/*)
///   3. Portal frontend (Vite)                   4. Microservices     5. Portal extensions
///
/// Every path is a parameter so the produced manifest is machine-agnostic; unset directories are
/// written as <c>&lt;EDIT: ...&gt;</c> placeholders for the user to fill in.
/// </summary>
public static class LocalStackTemplate
{
	/// <summary>The Scala backing services started by default (curated set that actually needs to be up).</summary>
	public static readonly string[] DefaultScalaServices =
	{
		"dbflake", "gateway", "auth", "account", "session", "content", "stats", "beamo",
		"realms", "announcements", "events", "groups", "history", "leaderboards", "cloud-saving"
	};

	public class Options
	{
		public string host = "http://localhost:8080";
		public string portalUrl = "http://localhost:4950";
		public string gatewayUrl = "http://localhost:5000";
		public string apiDir;
		public string scalaDir;
		public string portalDir;
		public List<string> scalaServices;
		public List<string> services;
		public List<string> extensions;
	}

	private static string Dir(string value, string label) =>
		string.IsNullOrWhiteSpace(value) ? $"<EDIT: absolute path to {label}>" : value;

	public static LocalStackConfig Create(Options o)
	{
		var apiDir = Dir(o.apiDir, "BeamableAPI (C# gateway repo)");
		var scalaDir = Dir(o.scalaDir, "BeamableBackend (Scala repo)");
		var portalDir = Dir(o.portalDir, "portal frontend repo");
		var scalaServices = o.scalaServices is { Count: > 0 } ? o.scalaServices : DefaultScalaServices.ToList();
		// Microservices and extensions default to empty — the user opts in per project.
		var services = o.services ?? new List<string>();
		var extensions = o.extensions ?? new List<string>();

		var config = new LocalStackConfig { host = o.host, portalUrl = o.portalUrl };

		// 1. C# stack FIRST — docker deps + Caddy, then the built Gateway binary. The C# stack hosts
		//    the service-discovery the Scala services resolve against, so it must be up before them.
		config.steps.Add(new LocalStackStep
		{
			name = "docker: api deps + caddy",
			workingDirectory = apiDir,
			command = "docker",
			arguments = "compose up -d",
			waitForExit = true,
			readyTimeoutSeconds = 300
		});
		config.steps.Add(new LocalStackStep
		{
			name = "c# gateway",
			workingDirectory = Path.Combine(apiDir, "BeamableGateway", "bin", "Debug", "net10.0"),
			command = OperatingSystem.IsWindows() ? "BeamableGateway.exe" : "./BeamableGateway",
			environment = new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Local" },
			readyWhenHttpOk = o.gatewayUrl,
			readyTimeoutSeconds = 180
		});

		// 2. Scala backing services — one shell step each, reproducing the sh launch: resolve the jar +
		//    <mainClass> + a mvn-built classpath (cached), prepend the module target/classes (which hold
		//    rendered config resources the ~/.m2 jars lack), then run as a Temurin-8 host JVM.
		config.steps.Add(new LocalStackStep
		{
			name = "scala: redis",
			workingDirectory = Path.Combine(scalaDir, "docker", "local"),
			command = "docker",
			arguments = "compose up -d --no-deps redis",
			waitForExit = true,
			readyTimeoutSeconds = 120
		});
		foreach (var svc in scalaServices)
		{
			// BASIC/OBJECT service providers log "<type> Service Started: <name>" when they register.
			// HTTP gateway apps (com.*.gateway.App) never do — they log "Serving traffic at ..." on bind.
			var readyLog = svc.Contains("gateway", StringComparison.OrdinalIgnoreCase)
				? "Serving traffic"
				: "Service Started";
			config.steps.Add(new LocalStackStep
			{
				name = $"scala: {svc}",
				group = "scala", // launch all Scala services in parallel (they're independent backing services)
				workingDirectory = scalaDir,
				shell = true,
				arguments = ScalaLaunchShell(svc),
				readyWhenLogContains = readyLog,
				readyTimeoutSeconds = 120
			});
		}

		// 3. Portal frontend (Vite dev server).
		config.steps.Add(new LocalStackStep
		{
			name = "portal frontend",
			workingDirectory = portalDir,
			command = OperatingSystem.IsWindows() ? "npm.cmd" : "npm",
			arguments = "run dev",
			readyWhenHttpOk = o.portalUrl,
			readyTimeoutSeconds = 120
		});

		// 4. Microservices — via the current beam CLI (exe auto-resolved).
		foreach (var svc in services)
			config.steps.Add(MicroserviceStep(svc));

		// 5. Portal extensions — beam run with --portal-url so the landing URL points at the local portal.
		foreach (var ext in extensions)
			config.steps.Add(ExtensionStep(ext));

		return config;
	}

	/// <summary>Name prefix identifying microservice steps (used by the "update services" flow).</summary>
	public const string MicroservicePrefix = "microservice: ";

	/// <summary>Name prefix identifying portal-extension steps (used by the "update services" flow).</summary>
	public const string ExtensionPrefix = "portal extension: ";

	/// <summary>Builds the beam step that runs a microservice against the local backend.</summary>
	public static LocalStackStep MicroserviceStep(string svc) => new LocalStackStep
	{
		// No workingDirectory: beam steps run from the .beamable workspace `beam local up` is
		// invoked in (set one explicitly here only if the service lives in a different workspace).
		name = $"{MicroservicePrefix}{svc}",
		beam = true,
		arguments = $"project run --ids {svc} --host ${{host}} --logs v --no-log-file"
	};

	/// <summary>Builds the beam step that runs a portal extension, pointing its landing URL at the local portal.</summary>
	public static LocalStackStep ExtensionStep(string ext) => new LocalStackStep
	{
		// No workingDirectory: runs from the .beamable workspace `beam local up` is invoked in.
		name = $"{ExtensionPrefix}{ext}",
		beam = true,
		arguments = $"project run --ids {ext} --host ${{host}} --portal-url ${{portalUrl}} --logs v --no-log-file"
	};

	/// <summary>
	/// The inline shell used to launch one Scala tools/* service as a host JVM, matching
	/// <c>scripts/run-local-stack.sh</c>'s <c>launch_scala_service</c>.
	/// </summary>
	private static string ScalaLaunchShell(string svc)
	{
		// Single-quoted for the sh -c wrapper; keep it one logical line.
		return
			$"set -e; SVC={svc}; " +
			"J8=$(/usr/libexec/java_home -v 1.8); " +
			"JAR=$(ls tools/$SVC/target/*-1.0-SNAPSHOT.jar 2>/dev/null | grep -v sources | head -1); " +
			"MAIN=$(grep -m1 -oE '<mainClass>[^<]+</mainClass>' tools/$SVC/pom.xml | sed -E 's#</?mainClass>##g'); " +
			"CPF=\"${TMPDIR:-/tmp}/beam-scala-cp/cp-$SVC.txt\"; mkdir -p \"$(dirname \"$CPF\")\"; " +
			"[ -s \"$CPF\" ] || JAVA_HOME=$J8 mvn -q -pl tools/$SVC dependency:build-classpath -Dmdep.outputFile=\"$CPF\"; " +
			"CP=\"tools/$SVC/target/classes:core/target/classes:$JAR:$(cat \"$CPF\")\"; " +
			"exec \"$J8/bin/java\" -cp \"$CP\" \"$MAIN\"";
	}
}
