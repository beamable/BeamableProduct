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
		/// <summary>The Scala backing services to launch, with their discovered main classes. When null/empty
		/// (e.g. no <c>scalaDir</c> given), <see cref="DefaultScalaServices"/> is used with no baked main class.</summary>
		public List<ScalaToolInfo> scalaTools;
		public List<string> services;
		public List<string> extensions;
		/// <summary>Service-group names to run as a whole via <c>project run --with-group</c>.</summary>
		public List<string> groups;
		/// <summary>Java 8 JAVA_HOME to bake into the manifest (stored in <see cref="LocalStackConfig.javaHome"/>). Null = omit from manifest and resolve at run time.</summary>
		public string javaHome;
	}

	/// <summary>A discovered Scala <c>tools/*</c> service: its folder name, resolved main class, and metadata.</summary>
	public class ScalaToolInfo
	{
		public string name;
		public string mainClass;
		public bool isEssential;
		public bool hasBasic;
		public bool hasObject;
	}

	/// <summary>
	/// Discovers the Scala backing services under <c>&lt;scalaDir&gt;/tools/*</c> by scanning each folder's
	/// <c>*.scala</c> for <c>object &lt;Name&gt; extends MicroService</c> (→ fully-qualified main class), and
	/// merging the <c>profiles</c> / <c>x-beam-services</c> metadata from <c>docker/local/docker-compose.yml</c>
	/// when present (BeamableBackend PR#632). Ported from #4258 <c>BackendListToolsCommand</c>. Returns an
	/// empty list when <paramref name="scalaDir"/> is unset or has no <c>tools</c> directory.
	/// </summary>
	public static List<ScalaToolInfo> DiscoverScalaTools(string scalaDir)
	{
		var result = new List<ScalaToolInfo>();
		if (string.IsNullOrWhiteSpace(scalaDir)) return result;

		var toolsDir = Path.Combine(scalaDir, "tools");
		if (!Directory.Exists(toolsDir)) return result;

		var compose = DockerComposeModel.TryLoad(scalaDir);

		foreach (var toolFolder in Directory.GetDirectories(toolsDir).OrderBy(p => p))
		{
			var name = Path.GetFileName(toolFolder);
			var mainClass = FindScalaMainClass(toolFolder);
			if (mainClass == null) continue; // not a launchable MicroService folder

			var info = new ScalaToolInfo { name = name, mainClass = mainClass };
			if (compose?.services != null && compose.services.TryGetValue(name, out var svc) && svc != null)
			{
				info.isEssential = svc.HasProfile("essential");
				info.hasBasic = svc.beamServices?.ContainsKey("basic") == true;
				info.hasObject = svc.beamServices?.ContainsKey("object") == true;
			}

			result.Add(info);
		}

		return result;
	}

	/// <summary>Scans a tool folder's Scala sources for the first <c>object X extends MicroService</c> and
	/// returns its fully-qualified name (<c>package.X</c>), or null if none is found.</summary>
	private static string FindScalaMainClass(string toolFolder)
	{
		foreach (var srcFile in Directory.EnumerateFiles(toolFolder, "*.scala", SearchOption.AllDirectories))
		{
			var package = "";
			foreach (var raw in File.ReadLines(srcFile))
			{
				var line = raw.Trim();
				if (line.StartsWith("package "))
				{
					package = line.Substring("package ".Length).Trim();
					continue;
				}

				var idxObject = line.IndexOf("object ", StringComparison.Ordinal);
				var idxExtends = line.IndexOf("extends MicroService", StringComparison.Ordinal);
				if (idxObject < 0 || idxExtends <= idxObject) continue;

				var start = idxObject + "object ".Length;
				var className = line.Substring(start, idxExtends - start).Trim();
				if (className.Length == 0) continue;
				return string.IsNullOrEmpty(package) ? className : $"{package}.{className}";
			}
		}

		return null;
	}

	private static string Dir(string value, string label) =>
		string.IsNullOrWhiteSpace(value) ? $"<EDIT: absolute path to {label}>" : value;

	public static LocalStackConfig Create(Options o)
	{
		var apiDir = Dir(o.apiDir, "BeamableAPI (C# gateway repo)");
		var scalaDir = Dir(o.scalaDir, "BeamableBackend (Scala repo)");
		var portalDir = Dir(o.portalDir, "portal frontend repo");
		// Prefer discovered tools (name + main class); fall back to the curated default names (no main class,
		// so the launch shell greps pom.xml at runtime) when nothing was discovered.
		var scalaTools = o.scalaTools is { Count: > 0 }
			? o.scalaTools
			: DefaultScalaServices.Select(n => new ScalaToolInfo { name = n }).ToList();
		// Microservices and extensions default to empty — the user opts in per project.
		var services = o.services ?? new List<string>();
		var extensions = o.extensions ?? new List<string>();

		var config = new LocalStackConfig { host = o.host, portalUrl = o.portalUrl, javaHome = o.javaHome };

		// 1. C# stack FIRST — docker deps + Caddy, then the built Gateway binary. The C# stack hosts
		//    the service-discovery the Scala services resolve against, so it must be up before them.
		config.steps.Add(new LocalStackStep
		{
			name = "docker: api deps + caddy",
			workingDirectory = apiDir,
			command = "docker",
			// --wait blocks until the containers are running/healthy (uses the broker healthcheck), so the
			// gateway doesn't start before its dependencies are actually up.
			arguments = "compose up -d --wait",
			stopArguments = "compose down",
			waitForExit = true,
			readyTimeoutSeconds = 300
		});
		config.steps.Add(new LocalStackStep
		{
			name = "c# gateway",
			workingDirectory = Path.Combine(apiDir, "BeamableGateway", "bin", "Debug", "net10.0"),
			command = OperatingSystem.IsWindows() ? "BeamableGateway.exe" : "./BeamableGateway",
			environment = new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Local" },
			// Require a real 200 from the gateway's /health endpoint (UseHealthChecks) rather than any
			// response on the root — otherwise a not-yet-serving gateway looks "ready after 0s".
			readyWhenHttp200 = $"{o.gatewayUrl}/health",
			// The gateway can crash on startup if Mongo hasn't finished initializing its users yet
			// (MongoAuthenticationException). Relaunch it a few times — it succeeds once Mongo is ready.
			readyRetries = 5,
			readyTimeoutSeconds = 180
		});

		// 2. Portal frontend (Vite dev server). Placed BEFORE the Scala group because it only serves the
		//    frontend (the browser talks to the backend at runtime) — so it comes up in ~1s instead of waiting
		//    behind the Scala services' readiness.
		config.steps.Add(new LocalStackStep
		{
			name = "portal frontend",
			workingDirectory = portalDir,
			command = OperatingSystem.IsWindows() ? "npm.cmd" : "npm",
			arguments = "run dev",
			readyWhenHttpOk = o.portalUrl,
			readyTimeoutSeconds = 120
		});

		// 3. Scala backing services — one shell step each, reproducing the sh launch: resolve the jar +
		//    <mainClass> + a mvn-built classpath (cached), prepend the module target/classes (which hold
		//    rendered config resources the ~/.m2 jars lack), then run as a Temurin-8 host JVM.
		config.steps.Add(new LocalStackStep
		{
			name = "scala: redis",
			workingDirectory = Path.Combine(scalaDir, "docker", "local"),
			command = "docker",
			arguments = "compose up -d --no-deps redis",
			stopArguments = "compose down",
			waitForExit = true,
			readyTimeoutSeconds = 120
		});
		foreach (var tool in scalaTools)
		{
			var isGateway = tool.name.Contains("gateway", StringComparison.OrdinalIgnoreCase);
			// Emit the launch script in the shell that matches THIS machine's OS: PowerShell on Windows
			// (cmd.exe can't run the POSIX-sh script), sh on macOS/Linux. `up` reads `shellKind` to pick
			// the interpreter. The manifest already holds absolute machine-specific paths, so being
			// OS-specific here is not a new portability constraint — re-run `init` per machine.
			var onWindows = OperatingSystem.IsWindows();
			var step = new LocalStackStep
			{
				name = $"scala: {tool.name}",
				group = "scala", // launch all Scala services in parallel (they're independent backing services)
				workingDirectory = scalaDir,
				shell = true,
				shellKind = onWindows ? "powershell" : "sh",
				mainClass = tool.mainClass,
				arguments = onWindows
					? ScalaLaunchPowerShell(tool.name, tool.mainClass)
					: ScalaLaunchShell(tool.name, tool.mainClass),
				// BASIC/OBJECT service providers log "<type> Service Started: <name>" when they register.
				// HTTP gateway apps (com.*.gateway.App) never do — they log "Serving traffic at ..." on bind.
				readyWhenLogContains = isGateway ? "Serving traffic" : "Service Started",
				readyTimeoutSeconds = 120
			};
			// The gateway exposes /metadata (PR#632) once it is serving; use it as a stronger, backend-confirmed
			// readiness gate, with the log substring above as fallback.
			if (isGateway)
				step.readyWhenHttp200 = "${host}/metadata";
			config.steps.Add(step);
		}

		// 4. Microservices — via the current beam CLI (exe auto-resolved). After Scala so the backend they
		//    call is up.
		foreach (var svc in services)
			config.steps.Add(MicroserviceStep(svc));

		// 5. Portal extensions — beam run with --portal-url so the landing URL points at the local portal.
		foreach (var ext in extensions)
			config.steps.Add(ExtensionStep(ext));

		// 6. Service groups — run every member (microservices + extensions) of the group in one beam invocation.
		foreach (var group in o.groups ?? new List<string>())
			config.steps.Add(GroupStep(group));

		return config;
	}

	/// <summary>Name prefix identifying microservice steps (used by the "update services" flow).</summary>
	public const string MicroservicePrefix = "microservice: ";

	/// <summary>Name prefix identifying portal-extension steps (used by the "update services" flow).</summary>
	public const string ExtensionPrefix = "portal extension: ";

	/// <summary>Name prefix identifying service-group steps (used by the "update services" flow).</summary>
	public const string GroupPrefix = "group: ";

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

	/// <summary>Builds the beam step that runs a whole service group (all its microservices + extensions).</summary>
	public static LocalStackStep GroupStep(string group) => new LocalStackStep
	{
		// Groups can contain portal extensions, so pass --portal-url too; harmless for microservice-only groups.
		name = $"{GroupPrefix}{group}",
		beam = true,
		arguments = $"project run --with-group {group} --host ${{host}} --portal-url ${{portalUrl}} --logs v --no-log-file"
	};

	/// <summary>
	/// The inline shell used to launch one Scala tools/* service as a host JVM, matching
	/// <c>scripts/run-local-stack.sh</c>'s <c>launch_scala_service</c>. Uses the cross-platform Java 8 home
	/// resolved into the <c>${java}</c> token (replacing the macOS-only <c>/usr/libexec/java_home</c>), and the
	/// <paramref name="mainClass"/> discovered at <c>init</c> time — falling back to grepping <c>pom.xml</c>
	/// when it is unknown.
	/// </summary>
	private static string ScalaLaunchShell(string svc, string mainClass)
	{
		// Single-quoted for the sh -c wrapper; keep it one logical line. ${java} is substituted by up.
		return
			$"set -e; SVC={svc}; " +
			"JHOME=\"${java}\"; " +
			"JAR=$(ls tools/$SVC/target/*-1.0-SNAPSHOT.jar 2>/dev/null | grep -v sources | head -1); " +
			$"MAIN='{mainClass ?? string.Empty}'; " +
			"[ -n \"$MAIN\" ] || MAIN=$(grep -m1 -oE '<mainClass>[^<]+</mainClass>' tools/$SVC/pom.xml | sed -E 's#</?mainClass>##g'); " +
			"CPF=\"${TMPDIR:-/tmp}/beam-scala-cp/cp-$SVC.txt\"; mkdir -p \"$(dirname \"$CPF\")\"; " +
			// Rebuild the cached classpath when it is missing/empty OR older than core/pom.xml (so a dep newly
			// added to core lands on it). `-am` builds `core` in the reactor and resolves its transitive deps
			// from the CURRENT source pom instead of a possibly-stale ~/.m2 install — otherwise a dep added to
			// core (e.g. zstd-jni) is silently dropped and the service dies with NoClassDefFoundError at runtime.
			"{ [ -s \"$CPF\" ] && [ \"$CPF\" -nt core/pom.xml ]; } || JAVA_HOME=\"$JHOME\" mvn -q -pl tools/$SVC -am dependency:build-classpath -Dmdep.outputFile=\"$CPF\"; " +
			"CP=\"tools/$SVC/target/classes:core/target/classes:$JAR:$(cat \"$CPF\")\"; " +
			"exec \"$JHOME/bin/java\" -cp \"$CP\" \"$MAIN\"";
	}

	/// <summary>
	/// The Windows/PowerShell equivalent of <see cref="ScalaLaunchShell"/>: it performs the same steps
	/// (resolve the service jar excluding the <c>-sources</c> jar, fall back to the <c>pom.xml</c>
	/// <c>&lt;mainClass&gt;</c>, build+cache the mvn classpath, then run a Temurin-8 host JVM) but with
	/// PowerShell cmdlets and the Windows classpath separator (<c>;</c>). Relative <c>tools/…</c> paths
	/// resolve against the step's <c>workingDirectory</c> (the Scala repo), exactly like the sh version.
	/// <c>${java}</c> is substituted to the resolved Java 8 home by <c>up</c>.
	/// </summary>
	private static string ScalaLaunchPowerShell(string svc, string mainClass)
	{
		// Written verbatim to a .launch.ps1 and run via `powershell -File`. Keep it dependency-free
		// (only cmdlets + mvn/java on PATH) so it works on stock Windows PowerShell 5.1.
		return string.Join("\n", new[]
		{
			"$ErrorActionPreference = 'Stop'",
			$"$svc = '{svc}'",
			"$jhome = '${java}'",
			"$jar = Get-ChildItem -Path \"tools/$svc/target\" -Filter '*-1.0-SNAPSHOT.jar' -ErrorAction SilentlyContinue |",
			"       Where-Object { $_.Name -notlike '*sources*' } | Select-Object -First 1 -ExpandProperty FullName",
			$"$main = '{mainClass ?? string.Empty}'",
			"if (-not $main) { $main = (Select-String -Path \"tools/$svc/pom.xml\" -Pattern '<mainClass>([^<]+)</mainClass>' |",
			"                  Select-Object -First 1).Matches.Groups[1].Value }",
			"$cpf = Join-Path $env:TEMP \"beam-scala-cp/cp-$svc.txt\"",
			"New-Item -ItemType Directory -Force -Path (Split-Path $cpf) | Out-Null",
			// Rebuild the cached classpath when missing/empty OR older than core/pom.xml. `-am` resolves the
			// intra-repo `core` from the reactor's CURRENT pom instead of a possibly-stale ~/.m2 install, so a
			// dep newly added to core (e.g. zstd-jni) is included rather than dropped (NoClassDefFoundError).
			"$stale = (-not (Test-Path $cpf)) -or ((Get-Item $cpf).Length -eq 0) -or ((Get-Item 'core/pom.xml').LastWriteTime -gt (Get-Item $cpf).LastWriteTime)",
			"if ($stale) {",
			"  $env:JAVA_HOME = $jhome; mvn -q -pl \"tools/$svc\" -am dependency:build-classpath \"-Dmdep.outputFile=$cpf\" }",
			"$cp = \"tools/$svc/target/classes;core/target/classes;$jar;\" + ((Get-Content $cpf -Raw).Trim())",
			"& \"$jhome\\bin\\java.exe\" -cp $cp $main",
			"exit $LASTEXITCODE",
		});
	}
}
