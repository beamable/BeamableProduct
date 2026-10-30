using Beamable.Common.Api.Realms;
using Beamable.Server;
using cli.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace cli.Services.LocalStack;

/// <summary>Parameters for bootstrapping a fresh local customer/realm (defaults match the reference setup).</summary>
public class RealmSeedOptions
{
	public string customerName = "beam";
	public string projectName = "beam-project";
	public string email = "beam@beamable.com";
	public string alias = "beam-project";
	public string password = "123456";
}

/// <summary>
/// A stack's login to its target backend: what its <c>beam</c> steps get as <c>BEAM_HOST</c>, <c>BEAM_CID</c>,
/// <c>BEAM_PID</c> and <c>BEAM_REFRESH_TOKEN</c>. Saved per target under <c>.beamable/local/targets/&lt;name&gt;/</c>,
/// never in the workspace's own config, so a <c>beam</c> command typed by hand keeps the workspace's login.
/// </summary>
public class LocalStackTargetLogin
{
	public string host;
	public string cid;
	public string pid;
	public string accessToken;
	public string refreshToken;
	public DateTime savedUtc;
}

/// <summary>
/// Bootstraps and validates the local realm for <c>beam local run</c>. The CLI can't create a realm
/// (<c>beam new</c> only opens the web signup), but the backend accepts a direct
/// <c>POST {host}/basic/realms/customer</c> that creates a customer + realm + account and returns the
/// cid/pid/token. The login is saved as the <c>local</c> target's (see <see cref="LocalStackTargets"/>), so the
/// stack's microservices and extensions authenticate against the fresh realm while the workspace's own login
/// stays as it was.
/// </summary>
public static class LocalRealmService
{
	/// <summary>
	/// Ensures the local realm exists and returns a login to it. Tries to create the customer/realm first; if it
	/// already exists (the backend returns 400 <c>InvalidAliasError</c>), logs into the existing realm with the
	/// same credentials instead.
	/// </summary>
	public static async Task<LocalStackTargetLogin> EnsureRealmAsync(string host, RealmSeedOptions o, CancellationToken token)
	{
		var created = await TryCreateRealmAsync(host, o, token);
		if (created != null)
		{
			Log.Information($"Created local realm cid={created.cid} pid={created.pid} alias={o.alias}.");
			return created;
		}

		Log.Information($"Local realm '{o.alias}' already exists — logging in with the local credentials.");
		return await LoginToExistingRealmAsync(host, o, token);
	}

	/// <summary>
	/// POSTs to create the customer/realm and returns a login to it. Returns null when the realm already exists
	/// (400 <c>InvalidAliasError</c>) so the caller can log in instead. Throws on any other failure.
	/// </summary>
	private static async Task<LocalStackTargetLogin> TryCreateRealmAsync(string host, RealmSeedOptions o, CancellationToken token)
	{
		var url = $"{host.TrimEnd('/')}/basic/realms/customer";
		var body = JsonConvert.SerializeObject(new
		{
			customerName = o.customerName,
			projectName = o.projectName,
			email = o.email,
			alias = o.alias,
			password = o.password
		});

		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

		// Tried again while the backend warms up (see LocalStackTargets' requests): a second try at a realm the first
		// one created answers that it exists, which logs in to it.
		HttpResponseMessage res = null;
		var until = DateTime.UtcNow + TimeSpan.FromMinutes(3);
		for (var attempt = 1; res == null; attempt++)
		{
			using var content = new StringContent(body, Encoding.UTF8, "application/json");
			content.Headers.TryAddWithoutValidation("accept", "application/json");
			try
			{
				res = await http.PostAsync(url, content, token);
			}
			catch (Exception e) when (e is not OperationCanceledException || !token.IsCancellationRequested)
			{
				if (DateTime.UtcNow >= until)
					throw new CliException($"Failed to reach the local backend to create a realm ({url}) in {attempt} tries: {e.Message}");
				Log.Information($"{url} didn't answer ({e.Message}); trying again (try {attempt}).");
				await Task.Delay(TimeSpan.FromSeconds(5), token);
			}
		}

		var payload = await res.Content.ReadAsStringAsync();
		if (!res.IsSuccessStatusCode)
		{
			// Already created by a previous run — not an error; the caller will log in to it.
			if ((int)res.StatusCode == 400 &&
			    (payload.Contains("InvalidAliasError") || payload.Contains("already being used")))
				return null;

			throw new CliException($"Realm creation failed ({(int)res.StatusCode}) at {url}: {LocalStackTargets.Trim(payload)}");
		}

		JObject json;
		try { json = JObject.Parse(payload); }
		catch { throw new CliException($"Could not parse realm-creation response: {LocalStackTargets.Trim(payload)}"); }

		var cid = json["cid"]?.ToString();
		var pid = json["pid"]?.ToString();
		if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(pid))
			throw new CliException($"Realm-creation response missing cid/pid: {LocalStackTargets.Trim(payload)}");

		return new LocalStackTargetLogin
		{
			host = host, cid = cid, pid = pid,
			accessToken = json["token"]?["access_token"]?.ToString(),
			refreshToken = json["token"]?["refresh_token"]?.ToString(),
			savedUtc = DateTime.UtcNow
		};
	}

	/// <summary>
	/// Logs into an already-existing local realm: resolves the alias to a cid, logs in with the credentials, and
	/// picks a realm under the customer (a dev realm when there is one).
	/// </summary>
	private static async Task<LocalStackTargetLogin> LoginToExistingRealmAsync(string host, RealmSeedOptions o, CancellationToken token)
	{
		var cid = await LocalStackTargets.ResolveCid(host, o.alias, token);
		var login = await LocalStackTargets.Login(host, cid, o.email, o.password, token);
		var realm = LocalStackTargets.LocalRealm(await LocalStackTargets.Realms(host, login, token));
		if (realm == null)
			throw new CliException($"Local customer '{o.alias}' has no realms to select.");

		login.pid = realm.Pid;
		Log.Information($"Logged in to local realm cid={cid} pid={realm.Pid} ({realm.ProjectName}).");
		return login;
	}
}

/// <summary>
/// The backends a stack can run against. <c>local</c> is the settings' <c>host</c> and the local realm; <c>lan</c> is
/// the same backend at this machine's LAN address; <c>dev</c>, <c>staging</c> and <c>prod</c> are Beamable's; the
/// settings' <c>targets</c> add more or change those, and a stack can also name a backend by its address.
/// Each target's login is kept in its own folder under <c>.beamable/local/targets/</c> and handed to a stack's
/// <c>beam</c> steps as environment overrides, so nothing in the workspace's <c>.beamable</c> config changes.
/// All calls here are plain HTTP against the target's host: the running command's own login is never switched.
/// </summary>
public static class LocalStackTargets
{
	public const string Local = "local";
	public const string Lan = "lan";

	/// <summary>Targets every workspace has without settings.</summary>
	public static readonly IReadOnlyDictionary<string, LocalStackTarget> BuiltIn =
		new Dictionary<string, LocalStackTarget>(StringComparer.OrdinalIgnoreCase)
		{
			// No host: it is the settings' host with this machine's LAN address, found when the stack runs.
			[Lan] = new LocalStackTarget { local = true },
			["dev"] = new LocalStackTarget { host = "https://dev.api.beamable.com" },
			["staging"] = new LocalStackTarget { host = Constants.PLATFORM_STAGING },
			["prod"] = new LocalStackTarget { host = Constants.PLATFORM_PRODUCTION, confirm = true }
		};

	/// <summary>True when <paramref name="name"/> is a backend's address (http or https) rather than a target's name.</summary>
	public static bool IsAddress(string name) =>
		Uri.TryCreate(name?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

	/// <summary>True when a stack can run against <paramref name="name"/>: a target's name or a backend's address.</summary>
	public static bool IsKnown(string name, LocalStackConfig config) =>
		string.IsNullOrWhiteSpace(name) || IsAddress(name) || Names(config).Contains(name.Trim(), StringComparer.OrdinalIgnoreCase);

	/// <summary>True when <paramref name="name"/> is this workspace's own backend (<c>local</c>, <c>lan</c>, or a
	/// settings target marked <c>local</c>), so the stack runs it and logs into the local realm.</summary>
	public static bool RunsLocal(string name, LocalStackConfig config)
	{
		if (string.IsNullOrWhiteSpace(name) || string.Equals(name, Local, StringComparison.OrdinalIgnoreCase)) return true;
		if (IsAddress(name)) return false;
		var key = config?.targets?.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
		BuiltIn.TryGetValue(name, out var builtIn);
		return (key != null ? config.targets[key].local : null) ?? builtIn?.local ?? false;
	}

	/// <summary>
	/// This machine's LAN address: the first IPv4 address of a network adapter that is up and has a gateway, leaving
	/// out loopback, tunnels and virtual adapters (Hyper-V, WSL, Docker, VirtualBox, VMware, VPNs), whose addresses
	/// other devices can't reach. Null when there is none.
	/// </summary>
	public static string LanAddress()
	{
		try
		{
			foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.OperationalStatus != OperationalStatus.Up
				    || nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel
				    || IsVirtualAdapter(nic.Name, nic.Description))
					continue;

				var ip = nic.GetIPProperties();
				if (!ip.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)))
					continue;

				var address = ip.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork
				                                                      && !IPAddress.IsLoopback(a.Address))?.Address;
				if (address != null) return address.ToString();
			}
		}
		catch (NetworkInformationException e)
		{
			Log.Verbose($"Could not list the network adapters: {e.Message}");
		}

		return null;
	}

	/// <summary>True for an adapter other devices can't reach this machine through, by its name or description.</summary>
	public static bool IsVirtualAdapter(string name, string description)
	{
		var text = $"{name} {description}";
		return new[] { "vEthernet", "Hyper-V", "WSL", "Docker", "VirtualBox", "VMware", "VPN", "TAP-", "Tailscale", "ZeroTier", "Bluetooth" }
			.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary><paramref name="host"/> with its machine name replaced by <paramref name="address"/>, keeping scheme and port.</summary>
	public static string WithAddress(string host, string address)
	{
		if (!Uri.TryCreate(host, UriKind.Absolute, out var uri))
			throw new LocalStackStackException($"The settings' host '{host}' isn't an address, so it can't be moved to {address}.");
		return new UriBuilder(uri) { Host = address }.Uri.ToString().TrimEnd('/');
	}

	/// <summary>Every target name a stack can use: <c>local</c>, the built-in ones and the settings' own.</summary>
	public static List<string> Names(LocalStackConfig config) =>
		new[] { Local }.Concat(BuiltIn.Keys).Concat(config?.targets?.Keys ?? Enumerable.Empty<string>())
			.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

	/// <summary>
	/// The target named <paramref name="name"/>: the settings' entry over the built-in one. <c>local</c> is the
	/// settings' host; <c>lan</c> is that host at this machine's LAN address unless the settings give it one; an
	/// address is a backend of its own.
	/// </summary>
	public static LocalStackTarget Get(string name, LocalStackConfig config, Func<string> lanAddress = null)
	{
		if (string.IsNullOrWhiteSpace(name) || string.Equals(name, Local, StringComparison.OrdinalIgnoreCase))
			return new LocalStackTarget { host = config?.host, local = true };
		if (IsAddress(name))
			return new LocalStackTarget { host = name.Trim().TrimEnd('/') };

		LocalStackTarget own = null;
		var key = config?.targets?.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
		if (key != null) own = config.targets[key];
		BuiltIn.TryGetValue(name, out var builtIn);
		if (own == null && builtIn == null)
			throw new LocalStackStackException(
				$"No target named '{name}'. Targets: {string.Join(", ", Names(config))}. Add one under \"targets\" in the settings, with its \"host\".");

		var local = own?.local ?? builtIn?.local;
		var host = own?.host ?? builtIn?.host;
		if (string.IsNullOrWhiteSpace(host) && local == true)
		{
			var address = (lanAddress ?? LanAddress)();
			if (string.IsNullOrWhiteSpace(address))
				throw new LocalStackStackException(
					$"No LAN address found for '{name}'. Set it under \"targets\" in the settings: \"{name}\": {{ \"host\": \"http://<this machine's address>:<port>\" }}.");
			host = WithAddress(config?.host, address);
		}

		if (string.IsNullOrWhiteSpace(host))
			throw new LocalStackStackException($"Target '{name}' has no \"host\" in the settings.");
		return new LocalStackTarget
		{
			host = host.TrimEnd('/'), cid = own?.cid ?? builtIn?.cid, pid = own?.pid ?? builtIn?.pid,
			local = local, confirm = own?.confirm ?? builtIn?.confirm
		};
	}

	/// <summary>
	/// The folder holding a target's login: <c>&lt;root&gt;/.beamable/local/targets/&lt;name&gt;</c>. A backend named by its
	/// address gets its host and port as the folder name (<c>gw.example.com_443</c>).
	/// </summary>
	public static string Dir(string workspaceRoot, string name) =>
		Path.Combine(workspaceRoot, ".beamable", "local", "targets", FolderName(name));

	public static string FolderName(string name) =>
		IsAddress(name) && Uri.TryCreate(name.Trim(), UriKind.Absolute, out var uri)
			? $"{uri.Host}_{uri.Port}".ToLowerInvariant()
			: name.Trim().ToLowerInvariant();

	public static LocalStackTargetLogin Load(string workspaceRoot, string name)
	{
		var file = Path.Combine(Dir(workspaceRoot, name), "login.json");
		try
		{
			return File.Exists(file) ? JsonConvert.DeserializeObject<LocalStackTargetLogin>(File.ReadAllText(file)) : null;
		}
		catch
		{
			return null;
		}
	}

	public static void Save(string workspaceRoot, string name, LocalStackTargetLogin login)
	{
		var dir = Dir(workspaceRoot, name);
		Directory.CreateDirectory(dir);
		login.savedUtc = DateTime.UtcNow;
		File.WriteAllText(Path.Combine(dir, "login.json"), JsonConvert.SerializeObject(login, Formatting.Indented));
	}

	/// <summary>
	/// The environment a stack's <c>beam</c> steps run with, so <c>project run</c>, the services it starts and
	/// their <c>generate-env</c> use the target (see <see cref="ConfigService.EnvironmentOverridable"/>). The access
	/// token is left out: it expires, and the CLI gets a fresh one from the refresh token.
	/// </summary>
	public static Dictionary<string, string> Environment(LocalStackTargetLogin login)
	{
		var env = new Dictionary<string, string>();
		void Add(string option, string value)
		{
			if (!string.IsNullOrWhiteSpace(value)) env[ConfigService.EnvironmentVariableFor(option)] = value;
		}

		Add(ConfigService.CFG_JSON_FIELD_HOST, login?.host);
		Add(ConfigService.CFG_JSON_FIELD_CID, login?.cid);
		Add(ConfigService.CFG_JSON_FIELD_PID, login?.pid);
		Add(ConfigService.CFG_TOKEN_JSON_FIELD_REFRESH_TOKEN, login?.refreshToken);
		return env;
	}

	/// <summary>
	/// The saved login for <paramref name="name"/> when it is for <paramref name="host"/> and its refresh token still
	/// works (refreshed and saved again); null otherwise.
	/// </summary>
	public static async Task<LocalStackTargetLogin> Saved(string workspaceRoot, string name, string host, CancellationToken token)
	{
		var saved = Load(workspaceRoot, name);
		if (saved == null || string.IsNullOrEmpty(saved.refreshToken) || string.IsNullOrEmpty(saved.cid) || string.IsNullOrEmpty(saved.pid)
		    || !string.Equals(saved.host?.TrimEnd('/'), host?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
			return null;

		try
		{
			var fresh = await Refresh(host, saved, token);
			Save(workspaceRoot, name, fresh);
			return fresh;
		}
		catch (Exception e)
		{
			Log.Verbose($"The saved {name} login no longer works: {e.Message}");
			return null;
		}
	}

	/// <summary>
	/// A login to a remote target: the saved one while it works, otherwise a new one, asked for in the terminal the
	/// first time (org, email, password, and the realm when the target doesn't name one). In a terminal, a saved login
	/// on a realm the target doesn't name is confirmed on every run, since its services register on that realm and
	/// it's easy to be on the wrong one. Without a terminal there is nobody to ask, so a new login fails with what to do.
	/// </summary>
	public static async Task<LocalStackTargetLogin> EnsureRemote(string workspaceRoot, string name, LocalStackTarget target,
		bool canAsk, CancellationToken token)
	{
		var saved = await Saved(workspaceRoot, name, target.host, token);
		if (saved != null && (string.IsNullOrEmpty(target.pid) || saved.pid == target.pid))
		{
			if (!canAsk || !string.IsNullOrEmpty(target.pid)) return saved;

			var realms = (await Realms(target.host, saved, token)).Where(r => !r.Archived).ToList();
			var current = realms.FirstOrDefault(r => r.Pid == saved.pid);
			if (current != null && AnsiConsole.Confirm(
				    $"Run against [green]{Markup.Escape(name)}[/] ({Markup.Escape(target.host)}) on realm [green]{Markup.Escape(current.DisplayPath())}[/] [grey]{Markup.Escape(current.Pid)}[/]?", defaultValue: true))
				return saved;

			saved.pid = PickRealm(realms, current ?? LocalRealm(realms), name).Pid;
			Save(workspaceRoot, name, saved);
			return saved;
		}

		if (!canAsk)
			throw new CliException(
				$"This stack runs against '{name}' ({target.host}), and this workspace has no login for it yet. Run it once in a terminal to log in; the login is saved in {Dir(workspaceRoot, name)}.");

		LocalStackStyle.H1($"Log in to {name}", target.host);
		AnsiConsole.MarkupLine($"[grey]Saved for this workspace's stacks in {Markup.Escape(Dir(workspaceRoot, name))}; your own `beam` login isn't changed.[/]");
		var org = !string.IsNullOrWhiteSpace(target.cid) ? target.cid : AnsiConsole.Ask<string>("Org (alias or cid):");
		var email = AnsiConsole.Ask<string>("Email:");
		var password = AnsiConsole.Prompt(new TextPrompt<string>("Password:").Secret());

		var cid = await ResolveCid(target.host, org.Trim(), token);
		var login = await Login(target.host, cid, email.Trim(), password, token);

		if (!string.IsNullOrWhiteSpace(target.pid))
		{
			login.pid = target.pid;
		}
		else
		{
			var realms = (await Realms(target.host, login, token)).Where(r => !r.Archived).ToList();
			login.pid = PickRealm(realms, LocalRealm(realms), name).Pid;
		}

		Save(workspaceRoot, name, login);
		return login;
	}

	/// <summary>Asks which realm a stack uses, <paramref name="first"/> (the current one, or a dev realm) offered first.</summary>
	private static RealmView PickRealm(List<RealmView> realms, RealmView first, string name)
	{
		if (realms.Count == 0)
			throw new CliException($"The org has no realms on {name}.");
		return AnsiConsole.Prompt(new SelectionPrompt<RealmView>()
			.Title($"Which realm should the stack use on [green]{Markup.Escape(name)}[/]? [grey](its services register there)[/]")
			.PageSize(15)
			.AddChoices(RealmChoices(realms, first))
			.UseConverter(r => $"{Markup.Escape(r.DisplayPath())} [grey]{Markup.Escape(r.Pid)}[/]")
			.AddBeamHightlight());
	}

	/// <summary>The realms to pick from, <paramref name="first"/> first, then each game's realms from dev up to prod.</summary>
	public static List<RealmView> RealmChoices(IEnumerable<RealmView> realms, RealmView first) =>
		realms.OrderBy(r => r == first ? 0 : 1).ThenBy(r => r.GamePid).ThenByDescending(r => r.Depth).ToList();

	/// <summary>The cid of <paramref name="cidOrAlias"/>: as is when numeric, else looked up as an alias.</summary>
	public static async Task<string> ResolveCid(string host, string cidOrAlias, CancellationToken token)
	{
		if (cidOrAlias.All(char.IsDigit)) return cidOrAlias;

		var json = await Request(HttpMethod.Get, $"{host.TrimEnd('/')}/basic/realms/customer/alias/available?alias={Uri.EscapeDataString(cidOrAlias)}",
			null, null, null, token);
		return CidOfAlias(json) ?? throw new CliException($"No org with the alias '{cidOrAlias}' at {host}.");
	}

	/// <summary>
	/// The org's cid in an alias lookup's answer, or null when there is none. The cid decides, not <c>available</c>:
	/// the local backend answers an alias in use with its cid and <c>"available": true</c>, and reading that as "no
	/// such org" left the stack without a login to the local realm.
	/// </summary>
	public static string CidOfAlias(JObject answer) =>
		answer?["cid"] is { Type: not JTokenType.Null } cid && cid.ToString().Length > 0 ? cid.ToString() : null;

	/// <summary>Logs in to the org with a password; the login has no realm yet.</summary>
	public static async Task<LocalStackTargetLogin> Login(string host, string cid, string email, string password, CancellationToken token)
	{
		var json = await Request(HttpMethod.Post, $"{host.TrimEnd('/')}/basic/auth/token", cid, null,
			new { grant_type = "password", username = email, password, customerScoped = true }, token);
		return new LocalStackTargetLogin
		{
			host = host, cid = cid,
			accessToken = json["access_token"]?.ToString(), refreshToken = json["refresh_token"]?.ToString()
		};
	}

	/// <summary>A fresh access token (and refresh token, when the backend rotates it) for a saved login.</summary>
	public static async Task<LocalStackTargetLogin> Refresh(string host, LocalStackTargetLogin login, CancellationToken token)
	{
		var json = await Request(HttpMethod.Post, $"{host.TrimEnd('/')}/basic/auth/token", login.cid, null,
			new { grant_type = "refresh_token", refresh_token = login.refreshToken }, token);
		return new LocalStackTargetLogin
		{
			host = host, cid = login.cid, pid = login.pid,
			accessToken = json["access_token"]?.ToString(),
			refreshToken = json["refresh_token"]?.ToString() ?? login.refreshToken
		};
	}

	/// <summary>The zones the local org gets, named after the realm tier bound to each.</summary>
	public static readonly string[] ZoneTiers = { "dev", "staging", "prod" };

	/// <summary>The zone tier of a realm: a game's root realm is prod, its child staging, and below that dev.</summary>
	public static string TierOf(RealmView realm) => realm.IsProduction ? "prod" : realm.IsStaging ? "staging" : "dev";

	/// <summary>
	/// What the local org is missing of <see cref="ZoneTiers"/>: the zones to create (out of the names it has) and, for
	/// each realm still in use, the tier whose zone it is bound to.
	/// </summary>
	public static (List<string> create, List<(string pid, string tier)> bind) ZonePlan(IEnumerable<RealmView> realms, IEnumerable<string> zoneNames)
	{
		var have = zoneNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return (ZoneTiers.Where(t => !have.Contains(t)).ToList(),
			realms.Where(r => !r.Archived).Select(r => (r.Pid, TierOf(r))).ToList());
	}

	/// <summary>
	/// Gives the login's org the zones dev, staging and prod and binds every realm to its tier's zone, so zone-scoped
	/// microservices and extensions find a zone locally as they do on dev. Asking for the zones creates dev and prod
	/// the first time; staging is created here. Binding again is harmless. These are C# gateway calls.
	/// </summary>
	public static async Task EnsureZones(string host, LocalStackTargetLogin login, CancellationToken token)
	{
		var zonesUrl = $"{host.TrimEnd('/')}/api/customers/{login.cid}/zones";
		async Task<List<JToken>> Zones() =>
			((await Request(HttpMethod.Get, zonesUrl, login.cid, login.accessToken, null, token))["zones"] as JArray ?? new JArray())
			.Where(z => z["archived"]?.Value<bool>() != true).ToList();

		var zones = await Zones();
		var (create, bind) = ZonePlan(await Realms(host, login, token), zones.Select(z => (string)z["displayName"]));
		foreach (var tier in create)
			await Request(HttpMethod.Post, zonesUrl, login.cid, login.accessToken, new { name = tier }, token);
		if (create.Count > 0) zones = await Zones();

		foreach (var (pid, tier) in bind)
		{
			var zone = zones.FirstOrDefault(z => string.Equals((string)z["displayName"], tier, StringComparison.OrdinalIgnoreCase))
			           ?? throw new CliException($"The local org has no '{tier}' zone after creating it.");
			await Request(HttpMethod.Put, $"{host.TrimEnd('/')}/api/customers/{login.cid}/realms/{pid}/zone", login.cid, login.accessToken,
				new { zoneId = (string)zone["zoneId"] }, token);
		}
	}

	/// <summary>The realm a local stack runs on: a dev realm when there is one, else any realm in use.</summary>
	public static RealmView LocalRealm(IReadOnlyList<RealmView> realms) =>
		realms.FirstOrDefault(r => r.IsDev && !r.Archived) ?? realms.FirstOrDefault(r => !r.Archived) ?? realms.FirstOrDefault();

	/// <summary>
	/// Every realm of every game in the login's org. From the customer's listing: <c>/basic/realms/games</c> lists
	/// only each game's root (prod), so a dev realm was never found and the login, and every service started with it,
	/// landed on prod, and only prod got its zone.
	/// </summary>
	public static async Task<List<RealmView>> Realms(string host, LocalStackTargetLogin login, CancellationToken token)
	{
		var json = await Request(HttpMethod.Get, $"{host.TrimEnd('/')}/basic/realms/customer", login.cid, login.accessToken, null, token);
		return RealmsOf(login.cid, json);
	}

	/// <summary>The realms in a <c>/basic/realms/customer</c> answer, each with its depth (prod, staging, dev).</summary>
	public static List<RealmView> RealmsOf(string cid, JObject customer) =>
		RealmsService.ProcessProjects(cid, customer?["customer"]?["projects"]?.ToObject<List<ProjectViewDTO>>() ?? new List<ProjectViewDTO>());

	private static string DisplayPath(this RealmView realm) =>
		realm.Parent == null ? realm.ProjectName : $"{realm.Parent.DisplayPath()} / {realm.ProjectName}";

	/// <summary>How long a call keeps being tried while the backend doesn't answer.</summary>
	private static readonly TimeSpan WarmUp = TimeSpan.FromMinutes(3);

	private static async Task<JObject> Request(HttpMethod method, string url, string scope, string bearer, object body, CancellationToken token)
	{
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
		HttpResponseMessage response = null;
		// Tried again while the backend doesn't answer, for a few minutes: the login runs right after the Scala services
		// come up, and its first requests there took longer than the timeout while later ones answered in a second. Each
		// call here is safe to repeat: a grant has no side effect, and creating a realm that exists says so.
		var until = DateTime.UtcNow + WarmUp;
		for (var attempt = 1; response == null; attempt++)
		{
			using var request = new HttpRequestMessage(method, url);
			request.Headers.TryAddWithoutValidation("accept", "application/json");
			if (!string.IsNullOrEmpty(scope)) request.Headers.TryAddWithoutValidation("X-BEAM-SCOPE", scope);
			if (!string.IsNullOrEmpty(bearer)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
			if (body != null) request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

			try
			{
				response = await http.SendAsync(request, token);
				if (DateTime.UtcNow < until && (int)response.StatusCode is 502 or 503 or 504)
				{
					Log.Information($"{url} answered {(int)response.StatusCode}; trying again (try {attempt}).");
					response.Dispose();
					response = null;
					await Task.Delay(TimeSpan.FromSeconds(5), token);
				}
			}
			catch (Exception e) when (e is not OperationCanceledException || !token.IsCancellationRequested)
			{
				if (DateTime.UtcNow >= until)
					throw new CliException($"Could not reach {url} in {attempt} tries over {WarmUp.TotalMinutes:0} minutes: {e.Message}");
				Log.Information($"{url} didn't answer ({e.Message}); trying again (try {attempt}).");
				await Task.Delay(TimeSpan.FromSeconds(5), token);
			}
		}

		var text = await response.Content.ReadAsStringAsync(token);
		if (!response.IsSuccessStatusCode)
			throw new CliException($"{method} {url} failed ({(int)response.StatusCode}): {Trim(text)}");
		try
		{
			return JObject.Parse(text);
		}
		catch
		{
			throw new CliException($"{url} answered with something that isn't JSON: {Trim(text)}");
		}
	}

	internal static string Trim(string s) => string.IsNullOrEmpty(s) || s.Length <= 300 ? s : s.Substring(0, 300);
}
