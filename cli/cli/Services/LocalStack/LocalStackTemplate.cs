namespace cli.Services.LocalStack;

/// <summary>
/// Builds a reference <see cref="LocalStackConfig"/> manifest that mirrors the proven
/// <c>scripts/run-local-stack.sh</c> bring-up order:
///   1. C# Gateway + Caddy proxy, plus the backend workers the gateway hands work off to — the
///      message-rail runtime and the campaign runtime (all BeamableAPI)
///   2. Scala backing services (BeamableBackend tools/*)
///   3. Portal frontend (Vite)                   4. Microservices     5. Portal extensions
///
/// Every path is a parameter so the produced manifest is machine-agnostic; unset directories are
/// written as <c>&lt;EDIT: ...&gt;</c> placeholders for the user to fill in.
/// </summary>
public static class LocalStackTemplate
{
	/// <summary>
	/// Heap flags every Scala service JVM is launched with. These are small backing services; the cap exists to
	/// stop JDK 8's "quarter of physical RAM" default from making ~18 concurrent JVMs unschedulable.
	/// </summary>
	public const string DefaultScalaJvmArgs = "-Xmx512m -Xms256m";

	/// <summary>
	/// Manifest tokens for the toolchain-provisioned commands. <c>up</c> substitutes each to an absolute
	/// executable inside the toolchain, or to the bare command name when no toolchain was provisioned — so the
	/// same manifest works with and without <c>beam local setup</c> (see
	/// <see cref="LocalStackConfigIO.Substitute"/>).
	/// </summary>
	public const string MavenToken = "${maven}";

	/// <inheritdoc cref="MavenToken"/>
	public const string NpmToken = "${npm}";

	/// <inheritdoc cref="MavenToken"/>
	public const string DotnetToken = "${dotnet}";

	/// <summary>
	/// The Scala service that hands out dbids (<c>DBIDProvider</c>). Every other service fetches one at boot, so
	/// it is launched — and awaited — before the rest instead of taking its alphabetical slot in the parallel
	/// group, where six of its dependents started ahead of it and spent ~15s timing out on `Failed to fetch DBIDs`.
	/// </summary>
	public const string ScalaDbidService = "dbflake";

	/// <summary>The Scala backing services started by default (curated set that actually needs to be up).</summary>
	public static readonly string[] DefaultScalaServices =
	{
		"dbflake", "gateway", "auth", "account", "session", "content", "stats", "beamo",
		// No "realms": the Scala realms service is retired. BeamableBackend PR#747 dropped it from the
		// tools/pom.xml reactor, so `-pl tools/realms` fails the whole build with "Could not find the
		// selected project in the reactor". Its routes are served by the C# stack — core's RealmsTransformers
		// proxies the "realms" service with defaultProxy = true, which is what `POST /basic/realms/customer`
		// (LocalRealmService) hits.
		"announcements", "events", "groups", "history", "leaderboards", "cloud-saving",
		// Message rails: "mail" serves /basic/mail/bulk (in-game inbox); "messaging" serves
		// email.basic (/basic/email/direct). Required by the player-engagement In-Game and Email rails.
		// "notification" serves notification.basic, which the content service broadcasts to when
		// committing a manifest — without it `content publish` fails server-side.
		"mail", "messaging", "notification",
		// Analytics ingest: serves POST /report/custom_batch/{cid}/{pid}/{gamerTag}, the route every
		// client SDK (Unity, web, and the native iOS/Android push funnels) posts core events to. It
		// binds :9003 — a DIFFERENT listener from the gateway's :9002 — so Caddy needs a matching
		// `handle /report/*` block or the requests fall through to the C# gateway and 404.
		"analytics-gateway"
	};

	public class Options
	{
		public string host = "http://localhost:8080";
		public string portalUrl = "http://localhost:4950";
		public string gatewayUrl = "http://localhost:5000";
		// The message-rail runtime binds its own port (distinct from the gateway's 5000) — it is run as a
		// binary, so ASPNETCORE_URLS is set explicitly rather than via launchSettings.
		public string messageRailUrl = "http://localhost:5030";
		// Likewise the campaign runtime, on its own port again so the three .NET hosts can coexist. Picking one
		// took two tries: NOT 5040, because on Windows the Connected Devices Platform service (svchost) holds
		// 0.0.0.0:5040 with an exclusive bind, so whichever starts first wins and the runtime intermittently fails
		// to bind (presenting as a campaign that publishes and never advances); and NOT 5050 or 5031, which
		// BeamableAPI's own launchSettings already claim (BeamableScheduler.Loader/.Dispatcher default to 5050).
		// 5045 is unused across BeamableAPI and this CLI.
		public string campaignRuntimeUrl = "http://localhost:5045";
		// The analytics loader. 5020 is not an arbitrary pick like the two above: it is the port the project's
		// OWN launchSettings.json already declares, so it is reserved for this process across BeamableAPI and
		// cannot collide with the gateway (5000), the message rail (5030) or the campaign runtime (5045).
		public string analyticsLoaderUrl = "http://localhost:5020";
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

		/// <summary>
		/// The toolchain <c>beam local setup</c> installed, adopted by <c>init</c> rather than prompted for. Null
		/// when setup has not run yet, in which case the manifest carries no toolchain and every command token
		/// falls back to the bare name resolved through PATH.
		/// </summary>
		public LocalStackToolchain toolchain;

		/// <summary>
		/// JVM flags every Scala service is launched with. Defaults to <see cref="DefaultScalaJvmArgs"/> — a heap
		/// cap, which matters because JDK 8 defaults <c>-Xmx</c> to a QUARTER of physical RAM per JVM (≈32 GB each
		/// on a 128 GB box). Launch ~18 of those and they cannot reserve address space: "Error occurred during
		/// initialization of VM — Could not reserve enough space for object heap", or a native-OOM crash of the
		/// whole Akka platform JVM, which then reads as Caddy 502s and microservices failing to fetch dbids.
		/// </summary>
		public string scalaJvmArgs = DefaultScalaJvmArgs;

		/// <summary>
		/// The port the Scala <c>gateway</c> service binds (its own listener, distinct from the Caddy host it is
		/// reached through). Only used to detect a port conflict before launching it.
		/// </summary>
		public int scalaGatewayPort = 9002;

		/// <summary>
		/// The port the Scala <c>analytics-gateway</c> binds (its own <c>server.conf</c>: 9003). Distinct from
		/// <see cref="scalaGatewayPort"/> — it is a separate HTTP app serving the analytics ingest route.
		/// Only used to detect a port conflict and to probe readiness before launching it.
		/// </summary>
		public int analyticsGatewayPort = 9003;
		/// <summary>
		/// Whether to EMIT the local web package registry steps (Verdaccio + local-unpkg) at all.
		/// <c>beam local init</c> always sets this, so the steps are always in the manifest and can be turned
		/// on later without regenerating it; whether they actually RUN is <see cref="webRegistry"/>. Only a
		/// caller that wants a manifest structurally without them (the tests) leaves this false.
		/// </summary>
		public bool includeWebRegistry;

		/// <summary>
		/// The standing choice to record in <see cref="LocalStackConfig.webRegistry"/>: whether
		/// <c>beam local up</c> runs the web-registry steps without being asked to. Only meaningful together
		/// with <see cref="includeWebRegistry"/> — there is nothing to run when the steps were not emitted.
		/// </summary>
		public bool webRegistry;

		/// <summary>
		/// The <c>portal-localdev</c> directory holding the web registry's docker-compose file. Only read when
		/// <see cref="includeWebRegistry"/> is set; empty writes an <c>&lt;EDIT: ...&gt;</c> placeholder.
		/// </summary>
		public string webRegistryDir;
	}

	/// <summary>
	/// Finds the portal extensions in a portal checkout by scanning for <c>package.json</c> files whose
	/// <c>beamable.portalExtension</c> flag is set, returning their package names and the service groups each
	/// declares.
	///
	/// This exists because the beamo manifest only covers the workspace the command is RUN in. Running
	/// <c>beam local init</c> anywhere other than the portal repo therefore discovered nothing, and the extension
	/// picker came up empty — which looks exactly like "this workspace has no extensions" rather than "you are
	/// standing in the wrong folder". Scanning the checkout that was just pointed at with <c>--portal-dir</c> is
	/// both what the user meant and independent of where they happen to be.
	/// </summary>
	public static (List<string> extensions, Dictionary<string, string[]> groups) DiscoverPortalExtensions(string portalDir)
	{
		var extensions = new List<string>();
		var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

		if (string.IsNullOrWhiteSpace(portalDir)
		    || portalDir.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal)
		    || !Directory.Exists(portalDir))
		{
			return (extensions, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));
		}

		// Extensions live under bundles/, but scan the whole checkout so a non-standard layout still works.
		foreach (var packageJson in EnumeratePackageJsonFiles(portalDir))
		{
			try
			{
				var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(packageJson));
				var beamable = json["beamable"];
				if ((bool?)beamable?["portalExtension"] != true) continue;

				var name = (string)json["name"];
				if (string.IsNullOrWhiteSpace(name)) continue;

				extensions.Add(name);
				foreach (var group in beamable["serviceGroups"] ?? Enumerable.Empty<Newtonsoft.Json.Linq.JToken>())
				{
					var groupName = (string)group;
					if (string.IsNullOrWhiteSpace(groupName)) continue;

					if (!groups.TryGetValue(groupName, out var members))
						groups[groupName] = members = new List<string>();

					members.Add(name);
				}
			}
			catch
			{
				// A malformed package.json is that extension's problem, not a reason to discover nothing.
			}
		}

		extensions.Sort(StringComparer.OrdinalIgnoreCase);
		return (extensions, groups.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Walks a checkout for <c>package.json</c> files, skipping the directories that would make this
	/// pathologically slow and can only contain false positives (<c>node_modules</c>, build output, VCS data).
	/// </summary>
	private static IEnumerable<string> EnumeratePackageJsonFiles(string root)
	{
		var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"node_modules", "dist", "build", ".git", ".beamable", "bin", "obj", "target", ".vite"
		};

		var stack = new Stack<string>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			var dir = stack.Pop();

			string[] files;
			string[] subdirectories;
			try
			{
				files = Directory.GetFiles(dir, "package.json");
				subdirectories = Directory.GetDirectories(dir);
			}
			catch
			{
				continue; // unreadable directory — skip it rather than abort the scan
			}

			foreach (var file in files) yield return file;

			foreach (var subdirectory in subdirectories)
			{
				var name = Path.GetFileName(subdirectory);
				if (!skip.Contains(name)) stack.Push(subdirectory);
			}
		}
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

	/// <summary>
	/// The <c>tools/*</c> folders Maven will actually accept in <c>-pl</c>: the <c>&lt;module&gt;</c> entries of
	/// the <c>tools/pom.xml</c> aggregator. A folder can hold a pom and a launchable MicroService yet not be
	/// registered in the reactor (e.g. <c>disruptor-bot</c>, <c>gatling</c>), and passing one to <c>-pl</c>
	/// fails the *entire* build with "Could not find the selected project in the reactor" — which aborts
	/// <c>up --build</c> and rolls the whole stack back. Returns an empty set when the aggregator pom is
	/// missing or holds no modules; callers treat that as "don't filter", so a parse problem can never
	/// silently drop every module and turn a working build into a no-op.
	/// </summary>
	public static HashSet<string> ReadScalaReactorModules(string scalaDir) =>
		string.IsNullOrWhiteSpace(scalaDir)
			? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			: ReadReactorModules(Path.Combine(scalaDir, "tools", "pom.xml"));

	/// <summary>
	/// The modules of the Scala repo's <b>root</b> aggregator (<c>core</c>, <c>tools</c>, <c>rest</c>) — a
	/// different pom from <see cref="ReadScalaReactorModules"/>'s <c>tools/pom.xml</c>. Used to confirm
	/// <c>core</c> is reactor-registered before adding it to <c>-pl</c>, under the same rule: an unregistered
	/// <c>-pl</c> entry fails the entire build.
	/// </summary>
	public static HashSet<string> ReadRootReactorModules(string scalaDir) =>
		string.IsNullOrWhiteSpace(scalaDir)
			? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			: ReadReactorModules(Path.Combine(scalaDir, "pom.xml"));

	/// <summary>Reads the <c>&lt;module&gt;</c> entries out of a Maven aggregator pom; empty when it is missing
	/// or holds none.</summary>
	private static HashSet<string> ReadReactorModules(string pomPath)
	{
		var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (!File.Exists(pomPath)) return modules;

		foreach (var raw in File.ReadLines(pomPath))
		{
			var line = raw.Trim();
			var idxOpen = line.IndexOf("<module>", StringComparison.Ordinal);
			if (idxOpen < 0) continue;

			var start = idxOpen + "<module>".Length;
			var idxClose = line.IndexOf("</module>", start, StringComparison.Ordinal);
			if (idxClose <= start) continue;

			var name = line.Substring(start, idxClose - start).Trim();
			if (name.Length > 0) modules.Add(name);
		}

		return modules;
	}

	/// <summary>The Scala repo's shared <c>core</c> module — a root-aggregator module every <c>tools/*</c>
	/// service is launched against (<c>core/target/classes</c> is on every launch classpath).</summary>
	public const string ScalaCoreModule = "core";

	/// <summary>The target framework the BeamableAPI .NET hosts build to — their output path is
	/// <c>bin/Debug/&lt;tfm&gt;/</c>, which both the build step's declared output and the run step's working
	/// directory are derived from.</summary>
	private const string DotnetHostTfm = "net10.0";

	/// <summary>
	/// Adds the build+run pair for one of the BeamableAPI .NET hosts (gateway / message-rail runtime /
	/// campaign runtime): <c>dotnet build &lt;project&gt; -c Debug</c>, then the produced binary run from its
	/// own output directory (so the <c>appsettings*.json</c> copied there are found).
	///
	/// Both steps come from one place on purpose: the build step declares the binary as its
	/// <see cref="LocalStackStep.requiredOutput"/>, so <c>up</c> builds it when it is missing even without
	/// <c>--build</c> — and that path can never drift from the path the run step actually launches.
	/// <paramref name="aspnetUrls"/> is null for the gateway, which takes the ASPNETCORE_URLS default; the
	/// workers each bind a port of their own so all three can coexist.
	/// </summary>
	private static void AddDotnetHost(LocalStackConfig config, string apiDir, string label, string project,
		string healthUrl, string aspnetUrls)
	{
		var outDir = Path.Combine(apiDir, project, "bin", "Debug", DotnetHostTfm);
		config.steps.Add(new LocalStackStep
		{
			name = $"build: {label}",
			workingDirectory = apiDir,
			// ${dotnet} is the toolchain's pinned SDK when `beam local setup` provisioned one, and plain `dotnet`
			// otherwise. BeamableAPI carries no global.json, so an unpinned build follows whichever SDK happens to
			// be newest on the machine.
			command = DotnetToken,
			arguments = $"build {project} -c Debug",
			build = true,
			// The apphost `dotnet build` produces: <project>.exe on Windows, an extension-less binary elsewhere.
			requiredOutput = Path.Combine(outDir, OperatingSystem.IsWindows() ? project + ".exe" : project),
			waitForExit = true,
			readyTimeoutSeconds = 300
		});

		var environment = new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Local" };
		if (!string.IsNullOrEmpty(aspnetUrls))
		{
			environment["ASPNETCORE_URLS"] = aspnetUrls;
		}

		config.steps.Add(new LocalStackStep
		{
			name = label,
			workingDirectory = outDir,
			command = OperatingSystem.IsWindows() ? project + ".exe" : "./" + project,
			environment = environment,
			// The gateway binds the port its /health lives on; the workers bind the one in ASPNETCORE_URLS.
			port = PortOf(aspnetUrls ?? healthUrl),
			// Require a real 200 from the /health endpoint (UseHealthChecks) rather than any response on the
			// root — otherwise a not-yet-serving host looks "ready after 0s".
			readyWhenHttp200 = $"{healthUrl}/health",
			// These can crash on startup if Mongo hasn't finished initializing its users yet
			// (MongoAuthenticationException). Relaunch a few times — they succeed once Mongo is ready.
			readyRetries = 5,
			readyTimeoutSeconds = 180
		});
	}

	/// <summary>
	/// The explicit port of a <c>http://host:port</c> URL, or 0 when it has none (so no conflict check runs).
	/// <c>Uri.Port</c> never reports 0 for http/https — it substitutes the scheme default — so a URL written
	/// without a port must be recognized via <c>IsDefaultPort</c>, otherwise the guard would probe :80/:443.
	/// </summary>
	private static int PortOf(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsDefaultPort && uri.Port > 0 ? uri.Port : 0;

	private static string Dir(string value, string label) =>
		string.IsNullOrWhiteSpace(value)
			? $"{LocalStackConfigIO.EditPlaceholder} absolute path to {label}>"
			: value;

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

		var config = new LocalStackConfig
		{
			host = o.host, portalUrl = o.portalUrl, javaHome = o.javaHome, toolchain = o.toolchain,
			// The standing web-registry choice. Only recorded when the steps exist to be run: a manifest
			// without them would otherwise claim a choice that has nothing to act on.
			webRegistry = o.includeWebRegistry ? o.webRegistry : null
		};

		// Documentation-only metadata, recorded so the generated agent skill can name the repos this manifest
		// spans. Uses the same already-placeholdered values the steps below get, so the two can never disagree.
		config.repos = new LocalStackRepos
		{
			apiDir = apiDir,
			scalaDir = scalaDir,
			portalDir = portalDir,
			// Only meaningful when the web-registry steps are written; left null otherwise so the skill can say
			// "not part of this stack" rather than pointing at a path nothing uses.
			webRegistryDir = o.includeWebRegistry ? Dir(o.webRegistryDir, "portal-localdev (local web package registry)") : null,
			productDir = o.includeWebRegistry ? Dir(WebProductDir(o.webRegistryDir), "BeamableProduct (web packages repo)") : null,
		};

		// 0. Local web package registry. Always written by `beam local init`; whether it RUNS is the
		//    `webRegistry` choice above (see LocalStackUpCommand.ResolveNoWebRegistry). Placed first
		//    because `build: portal deps` and the portal extension steps below run npm installs that may
		//    need to resolve locally published @beamable packages from it. Independent of everything else
		//    and fast to come up, so it costs nothing to have early.
		if (o.includeWebRegistry)
		{
			config.steps.Add(new LocalStackStep
			{
				name = WebRegistryStepName,
				workingDirectory = Dir(o.webRegistryDir, "portal-localdev (local web package registry)"),
				command = "docker",
				arguments = "compose up -d --wait",
				stopArguments = "compose stop",
				purgeStopArguments = "compose down -v",
				waitForExit = true,
				// Verdaccio answers its web UI on the root once it is serving; any response is enough.
				readyWhenHttpOk = WebRegistryReadyUrl,
				readyTimeoutSeconds = 180
			});
		}

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
			// `compose stop`, NOT `compose down`: mongo_master's data lives in anonymous volumes, which
			// `down` deletes along with the containers — that wiped accounts/customers/realms on every
			// stop/up cycle and forced a new CID each time. `stop` leaves the containers (and data) intact
			// and the next `compose up -d --wait` just restarts them. Use `stop --purge` for a clean slate.
			stopArguments = "compose stop",
			purgeStopArguments = "compose down -v",
			waitForExit = true,
			readyTimeoutSeconds = 300
		});
		// The C# gateway. Its build step declares the binary as its requiredOutput, so `beam local up` builds
		// it when it is missing even without --build (see AddDotnetHost).
		AddDotnetHost(config, apiDir, "c# gateway", "BeamableGateway", o.gatewayUrl, aspnetUrls: null);

		// The message-rail runtime — a dedicated backend worker (sibling to the gateway) that drains the
		// message-rail Mongo staging and delivers to the last-mile federations. Without it, message-rail
		// sends stage but never deliver. Modeled on the gateway steps; run as a binary with an explicit
		// ASPNETCORE_URLS so it doesn't collide with the gateway's :5000.
		AddDotnetHost(config, apiDir, "c# message rail runtime", "BeamableMessageRailRuntime",
			o.messageRailUrl, o.messageRailUrl);

		// The campaign runtime — the worker that actually *runs* campaigns. The gateway only authors them
		// (validate, store the graph, write the directory row); enrolling the entry segment, walking each
		// lane, and handing sends to the message rail all happen here, as does the Launching -> Active
		// transition. Without it a campaign publishes successfully and then does nothing at all, which
		// looks identical to a broken campaign — so it belongs in the default stack rather than being a
		// thing you have to know to start. Same shape as the message-rail runtime: joins the actor cluster
		// as a Member (so it needs Mongo + ActiveMQ from the docker step above), run as a binary with its
		// own ASPNETCORE_URLS, ready on /health.
		AddDotnetHost(config, apiDir, "c# campaign runtime", "BeamableCampaignRuntime",
			o.campaignRuntimeUrl, o.campaignRuntimeUrl);

		// The analytics loader — the competing consumer that drains the analytics event stream and lands it as
		// Parquet in S3, which the gateway's commit timer then folds into the Iceberg tables Athena reads.
		// Without it, events reach ActiveMQ and stop there: `POST /analytics/query` returns nothing and anything
		// built on the warehouse (the Campaign builder's analytics-event picker, Campaign Analytics) is empty
		// locally for no visible reason.
		//
		// This is the ONE step in the stack that talks to real AWS. There is no local emulator for S3 Tables or
		// Athena, so it uses a dedicated shared `local` analytics environment (the beamable-local-analytics*
		// buckets and the beamable-analytics-local workgroups; see BeamableAPI's appsettings.Local.json and its
		// README's "Analytics in Local Development"). That means it needs credentials that can assume the
		// analytics roles — run `beam local setup --only aws` to check. Without them the host still starts and
		// serves /health; it just logs AWS errors and lands nothing. To opt out entirely, set this step's
		// "enabled": false in the manifest.
		AddDotnetHost(config, apiDir, "c# analytics loader", "BeamableAnalyticsLoader",
			o.analyticsLoaderUrl, o.analyticsLoaderUrl);

		// 2. Portal frontend (Vite dev server). Placed BEFORE the Scala group because it only serves the
		//    frontend (the browser talks to the backend at runtime) — so it comes up in ~1s instead of waiting
		//    behind the Scala services' readiness.
		// Install the portal's node deps before `npm run dev` — only when `beam local up --build` is passed.
		config.steps.Add(new LocalStackStep
		{
			name = "build: portal deps",
			workingDirectory = portalDir,
			// ${npm} — the toolchain's pinned Node when there is one. The portal is built against Node 22
			// (its Dockerfile is node:22-alpine); a newer major on PATH installs different transitive deps.
			command = NpmToken,
			arguments = "install",
			build = true,
			// Declaring node_modules as the output makes `up` run this WITHOUT --build when it is missing — the
			// same self-heal the .NET hosts get. Otherwise a fresh clone launches the Vite step against a portal
			// with no dependencies and dies with `Cannot find package 'vite'`, which reads as a broken portal
			// rather than "npm install was never run here".
			requiredOutput = Path.Combine(portalDir, "node_modules"),
			waitForExit = true,
			readyTimeoutSeconds = 600
		});
		config.steps.Add(new LocalStackStep
		{
			name = "portal frontend",
			workingDirectory = portalDir,
			command = NpmToken,
			arguments = "run dev",
			readyWhenHttpOk = o.portalUrl,
			// Vite is configured with strictPort, so a squatter here is a hard failure rather than a port bump.
			port = PortOf(o.portalUrl),
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
			stopArguments = "compose stop",
			purgeStopArguments = "compose down -v",
			waitForExit = true,
			readyTimeoutSeconds = 120
		});
		// Compile `core` plus the selected Scala services so target/classes + jars exist before the JVMs
		// launch — only when `beam local up --build` is passed.
		// Only hand Maven the folders its reactor knows about: one unregistered folder in `-pl` fails the
		// whole build and rolls the stack back. An unreadable aggregator pom yields an empty set, which
		// deliberately falls through to the old unfiltered list rather than building nothing.
		var reactorModules = ReadScalaReactorModules(scalaDir);
		var buildableTools = reactorModules.Count > 0
			? scalaTools.Where(t => reactorModules.Contains(t.name)).ToList()
			: scalaTools;
		if (buildableTools.Count > 0)
		{
			// `core` explicitly, not just implied by `-am`: every launch script prepends `core/target/classes`
			// to the classpath (it holds rendered config resources the ~/.m2 jar lacks), yet `-am` only pulls
			// core into the reactor when a selected tool declares a direct dependency on it. It lives in the
			// ROOT aggregator (not tools/pom.xml), and is only passed when that pom actually registers it —
			// same rule as above, an unregistered `-pl` entry would fail the whole build.
			var modules = new List<string>();
			if (ReadRootReactorModules(scalaDir).Contains(ScalaCoreModule))
			{
				modules.Add(ScalaCoreModule);
			}

			foreach (var tool in buildableTools)
			{
				var module = $"tools/{tool.name}";
				if (!modules.Contains(module, StringComparer.OrdinalIgnoreCase))
				{
					modules.Add(module);
				}
			}

			config.steps.Add(new LocalStackStep
			{
				name = "build: scala",
				workingDirectory = scalaDir,
				// ${maven} — the toolchain's pinned Maven when there is one. Which Maven runs matters twice over:
				// the reactor build itself, and the fact that Maven picks its JDK from JAVA_HOME/PATH, so an
				// unpinned mvn can compile Scala 2.11 sources under a JDK 17/21 it was never meant to see.
				command = MavenToken,
				// The modules this reactor build produces output for. `up` runs the step without --build when any
				// of them has never been compiled, so a first `beam local up` on a fresh clone builds itself
				// instead of launching JVMs against empty target/classes.
				scalaModules = new List<string>(modules),
				// `clean` so a shared-module API change can't leave cross-module classes skewed (NoSuchMethodError).
				// `install`, not `package` — this is what BeamableBackend's own README prescribes (`mvn install`), and
				// the per-service launcher below depends on it: `dependency:build-classpath` resolves
				// `com.kickstand:core` as a Maven ARTIFACT, so core has to be in the local repository. `package`
				// leaves core's jar in core/target only, and on a machine whose ~/.m2 has never seen it the
				// classpath step falls back to the remote nexus, fails, and Maven CACHES the miss:
				//   "core:jar:1.0-SNAPSHOT was not found ... resolution is not reattempted until the update
                //    interval has elapsed"
				// which then breaks every service launch. It works on a long-lived machine only because some
				// earlier manual `mvn install` populated ~/.m2.
				//
				// `-U` (--update-snapshots) forces Maven to re-check remote for SNAPSHOTs even when
				// `resolver-status.properties` says "recently checked", which is what invalidates the cached miss
				// on a machine that has ALREADY been trapped by an older CLI whose `build: scala` ran `package`
				// instead of `install` (so `core` was never installed to ~/.m2 and every subsequent resolve short-
				// circuits on the cached 404). The upgraded CLI alone can't undo that history without `-U`.
				arguments = $"-q -U -pl {string.Join(",", modules)} -am clean install -DskipTests",
				// Scala runs under Java 8; ${java} is substituted to that JAVA_HOME by `up` (as for the launch shells).
				environment = new Dictionary<string, string> { ["JAVA_HOME"] = "${java}" },
				build = true,
				waitForExit = true,
				// A cold `clean package` of core + ~17 tools can run past 15 minutes, and a readiness timeout
				// only warns and continues — which would launch the JVMs against half-built jars.
				readyTimeoutSeconds = 1800
			});
		}

		LocalStackStep ScalaStep(ScalaToolInfo tool, bool grouped)
		{
			// EXACT match, not Contains: `analytics-gateway` is a different HTTP app that happens to end in
			// "gateway". Matching loosely gave it the real gateway's :9002 in the port guard and a
			// `/metadata` readiness probe it does not serve — so the guard flagged a phantom conflict against
			// the actual gateway, and readiness passed only because the OTHER service answered the probe.
			var isGateway = tool.name.Equals("gateway", StringComparison.OrdinalIgnoreCase);
			// Both are akka-http apps that log "Serving traffic at ..." on bind rather than registering as a
			// BASIC/OBJECT provider, so the log gate below is shared.
			var isHttpApp = isGateway
				|| tool.name.Equals("analytics-gateway", StringComparison.OrdinalIgnoreCase);
			// Emit the launch script in the shell that matches THIS machine's OS: PowerShell on Windows
			// (cmd.exe can't run the POSIX-sh script), sh on macOS/Linux. `up` reads `shellKind` to pick
			// the interpreter. The manifest already holds absolute machine-specific paths, so being
			// OS-specific here is not a new portability constraint — re-run `init` per machine.
			var onWindows = OperatingSystem.IsWindows();
			var step = new LocalStackStep
			{
				name = $"scala: {tool.name}",
				// Grouped steps launch in parallel (they're independent backing services). dbflake is emitted
				// ungrouped and first, so `up` has it ready before the group that depends on it starts.
				group = grouped ? "scala" : null,
				workingDirectory = scalaDir,
				shell = true,
				shellKind = onWindows ? "powershell" : "sh",
				mainClass = tool.mainClass,
				arguments = onWindows
					? ScalaLaunchPowerShell(tool.name, tool.mainClass, o.scalaJvmArgs)
					: ScalaLaunchShell(tool.name, tool.mainClass, o.scalaJvmArgs),
				// BASIC/OBJECT service providers log "<type> Service Started: <name>" when they register.
				// HTTP gateway apps (com.*.gateway.App) never do — they log "Serving traffic at ..." on bind.
				readyWhenLogContains = isHttpApp ? "Serving traffic" : "Service Started",
				readyTimeoutSeconds = 120,
				// The first Scala services to boot (e.g. realms, session) can lose a startup race with Mongo:
				// they connect before the replica set has a writable primary and die on their startup index
				// writes (MongoTimeoutException on a w=majority write). Relaunch a few times — the RS becomes
				// writable within seconds, exactly like the C# gateway step above.
				readyRetries = 5
			};
			if (isGateway)
			{
				// The gateway exposes /metadata (PR#632) once it is serving; use it as a stronger, backend-confirmed
				// readiness gate, with the log substring above as fallback. Its readiness URL is the Caddy host, so
				// name the port it actually binds separately.
				step.readyWhenHttp200 = "${host}/metadata";
				step.port = o.scalaGatewayPort;
			}
			else if (tool.name.Equals("analytics-gateway", StringComparison.OrdinalIgnoreCase))
			{
				// Serves the analytics ingest route every client SDK posts core events to. `/report/ping` is its
				// own liveness route (App.scala) — probed DIRECTLY on the port it binds, not through Caddy, so a
				// healthy proxy or the real gateway can never make it look up when it isn't.
				step.readyWhenHttp200 = $"http://localhost:{o.analyticsGatewayPort}/report/ping";
				step.port = o.analyticsGatewayPort;
			}

			return step;
		}

		// dbflake serves the dbids every other Scala service fetches at boot (DBIDProvider), so it goes first and
		// on its own: awaiting its readiness gate before the group removes the ~15s window in which every
		// alphabetically-earlier service logs `Failed to fetch DBIDs / ServiceClient timeout` and retries. Only
		// dbflake is hoisted — the rest are dbid *consumers* and stay parallel.
		var dbid = scalaTools.FirstOrDefault(t =>
			string.Equals(t.name, ScalaDbidService, StringComparison.OrdinalIgnoreCase));
		if (dbid != null)
		{
			config.steps.Add(ScalaStep(dbid, grouped: false));
		}

		foreach (var tool in scalaTools.Where(t => t != dbid))
		{
			config.steps.Add(ScalaStep(tool, grouped: true));
		}

		// 4. Local web packages — only under `--build`, and only when the web registry is part of this stack.
		//    Publishes @beamable/sdk + @beamable/portal-toolkit as the local-dev version and refreshes the
		//    projects that consume them, so the extension steps below build against what was just built.
		//
		//    Placed here, after the Scala group, for two reasons: `beam local up` runs EnsureRealmAndLogin
		//    before the first `beam` step, and that authenticates through the Scala auth service — putting
		//    these earlier would trigger a login against a backend that isn't up yet. And extensions must be
		//    refreshed before `project run` builds them, which happens immediately below.
		//
		//    Paths go in workingDirectory, never in arguments: the runner splits arguments on whitespace with
		//    no quote handling, so a path containing a space would be torn into separate argv entries. Both
		//    commands default to their working directory, so nothing is lost.
		if (o.includeWebRegistry)
		{
			var productDir = Dir(WebProductDir(o.webRegistryDir), "BeamableProduct (web packages repo)");

			config.steps.Add(new LocalStackStep
			{
				name = WebPublishStepName,
				workingDirectory = productDir,
				beam = true,
				arguments = "web publish",
				build = true,
				waitForExit = true,
				// Two tsdown builds plus two publishes; generous, and a non-zero exit aborts the stack rather
				// than letting extensions build against a stale toolkit.
				readyTimeoutSeconds = 900
			});
			config.steps.Add(new LocalStackStep
			{
				name = WebRefreshStepName,
				// The repo holding the extensions to repoint — the same one the portal frontend runs from.
				workingDirectory = portalDir,
				beam = true,
				arguments = "web use",
				build = true,
				waitForExit = true,
				readyTimeoutSeconds = 600
			});
		}

		// 5. Microservices — via the current beam CLI (exe auto-resolved). After Scala so the backend they
		//    call is up.
		foreach (var svc in services)
			config.steps.Add(MicroserviceStep(svc));

		// 6. Portal extensions — beam run with --portal-url so the landing URL points at the local portal.
		foreach (var ext in extensions)
			config.steps.Add(ExtensionStep(ext));

		// 7. Service groups — run every member (microservices + extensions) of the group in one beam invocation.
		foreach (var group in o.groups ?? new List<string>())
			config.steps.Add(GroupStep(group));

		return config;
	}

	/// <summary>
	/// Name of the optional local web package registry step (Verdaccio + local-unpkg). Not a prefix: there
	/// is exactly one, and the "update services" flow leaves it alone because it matches none of the
	/// microservice/extension/group prefixes below.
	/// </summary>
	public const string WebRegistryStepName = "docker: web registry";

	/// <summary>Readiness probe for <see cref="WebRegistryStepName"/> — Verdaccio's default address.</summary>
	public const string WebRegistryReadyUrl = "http://localhost:4873";

	/// <summary>
	/// Names of the optional web-package steps. Like <see cref="WebRegistryStepName"/> these deliberately
	/// avoid the microservice/extension/group prefixes below, so <c>beam local init --update-services</c>
	/// leaves them alone.
	/// </summary>
	public const string WebPublishStepName = "build: web packages";

	/// <inheritdoc cref="WebPublishStepName"/>
	public const string WebRefreshStepName = "build: web extension pins";

	/// <summary>
	/// True when <paramref name="name"/> is one of the three local web-registry steps. Both
	/// <see cref="WebPublishStepName"/> and <see cref="WebRefreshStepName"/> are build steps with no
	/// <c>requiredOutput</c>, so <c>beam local up</c> cannot self-heal them the way it does a missing binary —
	/// <c>--with-web-registry</c> uses this to opt them in explicitly.
	/// </summary>
	public static bool IsWebStep(string name) =>
		string.Equals(name, WebRegistryStepName, StringComparison.OrdinalIgnoreCase)
		|| string.Equals(name, WebPublishStepName, StringComparison.OrdinalIgnoreCase)
		|| string.Equals(name, WebRefreshStepName, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// The product repo holding <c>web/</c> and <c>beam-portal-toolkit/</c>, derived from the
	/// <c>portal-localdev</c> path so <c>beam local init</c> needs no extra option for it.
	/// </summary>
	public static string WebProductDir(string webRegistryDir) =>
		string.IsNullOrWhiteSpace(webRegistryDir) ? null : Path.GetDirectoryName(webRegistryDir);

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
		arguments = $"project run --ids {svc} --host ${{host}} --logs v --no-log-file",
		// These launch LAST, as a burst of a dozen `beam project run` processes each doing its own
		// restore/build, and a service that loses that scramble EXITS rather than hanging — which is the
		// one condition `readyRetries` reliably catches (the hung path needs a `port`, and these declare
		// none). Without it a single transient "failed to start all services" leaves the stack up minus
		// that microservice, and the next thing to notice is a device call 404ing hours later.
		readyRetries = 3
	};

	/// <summary>Builds the beam step that runs a portal extension, pointing its landing URL at the local portal.</summary>
	public static LocalStackStep ExtensionStep(string ext) => new LocalStackStep
	{
		// No workingDirectory: runs from the .beamable workspace `beam local up` is invoked in.
		name = $"{ExtensionPrefix}{ext}",
		beam = true,
		arguments = $"project run --ids {ext} --host ${{host}} --portal-url ${{portalUrl}} --logs v --no-log-file",
		// Same rationale as MicroserviceStep: these run last and die outright when `${host}` is briefly
		// unreachable — a Docker Desktop restart mid-bring-up is enough, and Caddy returning ~1 minute
		// later is well inside 3 retries at 3s plus each attempt's own build time.
		readyRetries = 3
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
	private static string ScalaLaunchShell(string svc, string mainClass, string jvmArgs)
	{
		// Single-quoted for the sh -c wrapper; keep it one logical line. ${java} is substituted by up.
		return
			$"set -e; SVC={svc}; " +
			"JHOME=\"${java}\"; " +
			$"JVM_ARGS='{jvmArgs}'; " +
			"JAR=$(ls tools/$SVC/target/*-1.0-SNAPSHOT.jar 2>/dev/null | grep -v sources | head -1); " +
			$"MAIN='{mainClass ?? string.Empty}'; " +
			"[ -n \"$MAIN\" ] || MAIN=$(grep -m1 -oE '<mainClass>[^<]+</mainClass>' tools/$SVC/pom.xml | sed -E 's#</?mainClass>##g'); " +
			// The module's OWN compiled output has to exist too — a perfectly valid dependency classpath still
			// dies with a bare "Could not find or load main class" when target/classes is empty and no jar was
			// built (tools/cloud-saving ships sources but a plain `up` never compiles it). Say that plainly.
			// Deliberately DIAGNOSE-ONLY: do not run maven here. `mvn -pl tools/$SVC -am ...` also rebuilds
			// `core`, and a freshly built core against day-old tools/* classes makes every other service die
			// with `NoSuchMethodError: com.kickstand.core.RequestContext.copy(...)` the moment it handles a
			// request — the whole stack proxies through gateway, so one stray -am build silently bricks it.
			// Whole-reactor rebuilds belong to the `build: scala` step (`--build`), which keeps core and every
			// tools/* module binary-consistent.
			"CLASSES=\"tools/$SVC/target/classes\"; " +
			"HAS_CLASSES=$(find \"$CLASSES\" -name '*.class' -print -quit 2>/dev/null); " +
			"{ [ -n \"$HAS_CLASSES\" ] || [ -n \"$JAR\" ]; } || { echo \"beam: tools/$SVC has no compiled output (target/classes is empty and no jar) — re-run 'beam local up --build' to build the Scala reactor. Do NOT build this module alone with -am: that rebuilds core and breaks every other already-built service.\" >&2; exit 1; }; " +
			"CPF=\"${TMPDIR:-/tmp}/beam-scala-cp/cp-$SVC.txt\"; mkdir -p \"$(dirname \"$CPF\")\"; " +
			// Rebuild the cached classpath when it is missing/empty OR older than core/pom.xml (so a dep newly
			// added to core lands on it). `-am` builds `core` in the reactor and resolves its transitive deps
			// from the CURRENT source pom instead of a possibly-stale ~/.m2 install — otherwise a dep added to
			// core (e.g. zstd-jni) is silently dropped and the service dies with NoClassDefFoundError at runtime.
			// `|| true` so `set -e` does not abort here on a failed mvn: the explicit guard below has to be
			// reached, or the only output is raw Maven noise and the step just "exited early (code 1)".
			// MVN is the ${maven} token, not a bare `mvn`: this classpath must be resolved by the SAME Maven (and
			// therefore the same ~/.m2 layout and JDK) as the `build: scala` reactor step above. Two different
			// Mavens here produce a classpath that does not match the compiled classes.
			// `-o` (offline) so a stale `_remote.repositories` / `resolver-status.properties` entry — the one
			// that says "com.kickstand:core was not found in nexus during a previous attempt" — cannot short-
			// circuit the resolve. Every dep this step needs is already in ~/.m2 (public artifacts from earlier
			// resolves, `core` from `build: scala`'s `mvn install`), so touching remote here has no upside and
			// one very expensive downside.
			"{ [ -s \"$CPF\" ] && [ \"$CPF\" -nt core/pom.xml ]; } || JAVA_HOME=\"$JHOME\" \"" + MavenToken + "\" -q -o -pl tools/$SVC -am dependency:build-classpath -Dmdep.outputFile=\"$CPF\" || true; " +
			// An empty cache means the mvn above failed. Launching anyway starts a JVM with only the module's own
			// classes on the classpath, which dies deep in classloading — say what actually went wrong instead.
			"[ -s \"$CPF\" ] || { echo \"beam: classpath cache $CPF is empty — offline 'mvn dependency:build-classpath' failed for tools/$SVC. The usual cause is that com.kickstand:core is not in your local Maven repository (~/.m2/repository/com/kickstand/core/1.0-SNAPSHOT/). Fix: (1) run 'beam local up --build' (the reactor uses -U and 'mvn install', which invalidates any cached miss and writes core to ~/.m2). If that ALSO fails, (2) delete ~/.m2/repository/com/kickstand/ (locally-built artifacts only, safe to remove) and re-run 'beam local up --build'. Second cause: Maven cannot resolve the 'dependency' plugin prefix OFFLINE (its plugin metadata was never cached) — '--build' does NOT fix that. Fix: run once online from this repo: mvn -U dependency:build-classpath -pl tools/$SVC ; then re-run 'beam local up'. If it still fails, delete ~/.m2/repository/org/apache/maven/plugins/maven-dependency-plugin/*/*.lastUpdated and retry.\" >&2; exit 1; }; " +
			"CP=\"tools/$SVC/target/classes:core/target/classes:$JAR:$(cat \"$CPF\")\"; " +
			// $JVM_ARGS unquoted on purpose: it must word-split into separate flags.
			"exec \"$JHOME/bin/java\" $JVM_ARGS -cp \"$CP\" \"$MAIN\"";
	}

	/// <summary>
	/// The Windows/PowerShell equivalent of <see cref="ScalaLaunchShell"/>: it performs the same steps
	/// (resolve the service jar excluding the <c>-sources</c> jar, fall back to the <c>pom.xml</c>
	/// <c>&lt;mainClass&gt;</c>, build+cache the mvn classpath, then run a Temurin-8 host JVM) but with
	/// PowerShell cmdlets and the Windows classpath separator (<c>;</c>). Relative <c>tools/…</c> paths
	/// resolve against the step's <c>workingDirectory</c> (the Scala repo), exactly like the sh version.
	/// <c>${java}</c> is substituted to the resolved Java 8 home by <c>up</c>.
	/// </summary>
	private static string ScalaLaunchPowerShell(string svc, string mainClass, string jvmArgs)
	{
		// Written verbatim to a .launch.ps1 and run via `powershell -File`. Keep it dependency-free
		// (only cmdlets + mvn/java on PATH) so it works on stock Windows PowerShell 5.1.
		return string.Join("\n", new[]
		{
			"$ErrorActionPreference = 'Stop'",
			$"$svc = '{svc}'",
			"$jhome = '${java}'",
			// An array so it splats as separate arguments to java.exe below; empty stays empty (an empty string
			// element would reach java as an unrecognized "" option).
			"$jvmArgs = @(" + string.Join(",", SplitJvmArgs(jvmArgs).Select(a => $"'{a}'")) + ")",
			"$jar = Get-ChildItem -Path \"tools/$svc/target\" -Filter '*-1.0-SNAPSHOT.jar' -ErrorAction SilentlyContinue |",
			"       Where-Object { $_.Name -notlike '*sources*' } | Select-Object -First 1 -ExpandProperty FullName",
			$"$main = '{mainClass ?? string.Empty}'",
			"if (-not $main) { $main = (Select-String -Path \"tools/$svc/pom.xml\" -Pattern '<mainClass>([^<]+)</mainClass>' |",
			"                  Select-Object -First 1).Matches.Groups[1].Value }",
			// The module's OWN compiled output has to exist too — a perfectly valid dependency classpath still
			// dies with a bare "Could not find or load main class" when target/classes is empty and no jar was
			// built (tools/cloud-saving ships sources but a plain `up` never compiles it). Say that plainly.
			// Deliberately DIAGNOSE-ONLY: do not run maven here. `mvn -pl tools/$svc -am ...` also rebuilds
			// `core`, and a freshly built core against day-old tools/* classes makes every other service die
			// with `NoSuchMethodError: com.kickstand.core.RequestContext.copy(...)` the moment it handles a
			// request — the whole stack proxies through gateway, so one stray -am build silently bricks it.
			// Whole-reactor rebuilds belong to the `build: scala` step (`--build`), which keeps core and every
			// tools/* module binary-consistent.
			"$classes = \"tools/$svc/target/classes\"",
			"$hasClasses = (Test-Path $classes) -and $null -ne (Get-ChildItem -Path $classes -Filter '*.class' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1)",
			"if (-not $hasClasses -and -not $jar) {",
			"  Write-Host \"beam: tools/$svc has no compiled output (target/classes is empty and no jar) - re-run 'beam local up --build' to build the Scala reactor. Do NOT build this module alone with -am: that rebuilds core and breaks every other already-built service.\"",
			"  exit 1 }",
			"$cpf = Join-Path $env:TEMP \"beam-scala-cp/cp-$svc.txt\"",
			"New-Item -ItemType Directory -Force -Path (Split-Path $cpf) | Out-Null",
			// Rebuild the cached classpath when missing/empty OR older than core/pom.xml. `-am` resolves the
			// intra-repo `core` from the reactor's CURRENT pom instead of a possibly-stale ~/.m2 install, so a
			// dep newly added to core (e.g. zstd-jni) is included rather than dropped (NoClassDefFoundError).
			"$stale = (-not (Test-Path $cpf)) -or ((Get-Item $cpf).Length -eq 0) -or ((Get-Item 'core/pom.xml').LastWriteTime -gt (Get-Item $cpf).LastWriteTime)",
			"if ($stale) {",
			// The ${maven} token, not a bare `mvn`: this classpath has to be resolved by the SAME Maven (and
				// therefore the same ~/.m2 layout and JDK) as the `build: scala` reactor step, or it will not match
				// the classes that were compiled. `&` because the substituted value is an absolute path.
				// `-o` (offline) so a stale `_remote.repositories` entry saying "com.kickstand:core was not found
				// in nexus" cannot short-circuit the resolve. Every dep is already in ~/.m2 (public artifacts from
				// earlier resolves, `core` from `build: scala`'s `mvn install`) — touching remote here can only hurt.
				"  $env:JAVA_HOME = $jhome; & '" + MavenToken + "' -q -o -pl \"tools/$svc\" -am dependency:build-classpath \"-Dmdep.outputFile=$cpf\" }",
			// An empty/missing cache means that mvn failed. Reading it with `(Get-Content -Raw).Trim()` threw
			// "You cannot call a method on a null-valued expression" and the service silently never launched —
			// so read it defensively and report what actually broke.
			"$deps = ''",
			"if (Test-Path $cpf) { $raw = Get-Content $cpf -Raw; if ($raw) { $deps = $raw.Trim() } }",
			"if (-not $deps) {",
			"  Write-Host \"beam: classpath cache $cpf is empty - offline 'mvn dependency:build-classpath' failed for tools/$svc. The usual cause is that com.kickstand:core is not in your local Maven repository ($env:USERPROFILE\\.m2\\repository\\com\\kickstand\\core\\1.0-SNAPSHOT\\). Fix: (1) run 'beam local up --build' (the reactor uses -U and 'mvn install', which invalidates any cached miss and writes core to ~/.m2). If that ALSO fails, (2) delete $env:USERPROFILE\\.m2\\repository\\com\\kickstand\\ (locally-built artifacts only, safe to remove) and re-run 'beam local up --build'. Second cause: Maven cannot resolve the 'dependency' plugin prefix OFFLINE (its plugin metadata was never cached) - '--build' does NOT fix that. Fix: run once online from this repo: mvn -U dependency:build-classpath -pl tools/$svc ; then re-run 'beam local up'. If it still fails, delete $env:USERPROFILE\\.m2\\repository\\org\\apache\\maven\\plugins\\maven-dependency-plugin\\*\\*.lastUpdated and retry.\"",
			"  exit 1 }",
			"$cp = \"tools/$svc/target/classes;core/target/classes;$jar;\" + $deps",
			"& \"$jhome\\bin\\java.exe\" @jvmArgs -cp $cp $main",
			"exit $LASTEXITCODE",
		});
	}

	/// <summary>Splits a JVM argument string into individual flags (for PowerShell array splatting).</summary>
	private static string[] SplitJvmArgs(string jvmArgs) =>
		(jvmArgs ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
