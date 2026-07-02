using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

/// <summary>The realm produced by <see cref="LocalRealmService.CreateRealmAsync"/>.</summary>
public class CreatedRealm
{
	public string cid;
	public string pid;
	public string alias;
}

/// <summary>
/// Bootstraps and validates the local realm/login for <c>beam local up</c>. The CLI can't create a realm
/// (<c>beam new</c> only opens the web signup), but the backend accepts a direct
/// <c>POST {host}/basic/realms/customer</c> that creates a customer + realm + account and returns the
/// cid/pid/token — which we write into the workspace config so subsequent beam commands (and the local
/// microservices/extensions) authenticate against the fresh realm.
/// </summary>
public static class LocalRealmService
{
	/// <summary>
	/// Ensures the local realm exists and the workspace is logged into it. Tries to create the customer/realm
	/// first; if it already exists (the backend returns 400 <c>InvalidAliasError</c>), logs into the existing
	/// realm with the same credentials instead. Either way, cid/pid/host + a valid token are written to config.
	/// </summary>
	public static async Task<CreatedRealm> EnsureRealmAsync(CommandArgs args, string host, RealmSeedOptions o)
	{
		var created = await TryCreateRealmAsync(args, host, o);
		if (created != null)
		{
			Log.Information($"Created local realm cid={created.cid} pid={created.pid} alias={created.alias} and wrote it to the workspace config.");
			return created;
		}

		Log.Information($"Local realm '{o.alias}' already exists — logging in with the local credentials.");
		return await LoginToExistingRealmAsync(args, host, o);
	}

	/// <summary>
	/// POSTs to create the customer/realm. On success, persists cid/pid/host + token and returns the realm.
	/// Returns null when the realm already exists (400 <c>InvalidAliasError</c>) so the caller can log in
	/// instead. Throws on any other failure.
	/// </summary>
	private static async Task<CreatedRealm> TryCreateRealmAsync(CommandArgs args, string host, RealmSeedOptions o)
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
		using var content = new StringContent(body, Encoding.UTF8, "application/json");
		content.Headers.TryAddWithoutValidation("accept", "application/json");

		HttpResponseMessage res;
		try
		{
			res = await http.PostAsync(url, content, args.Lifecycle.CancellationToken);
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to reach the local backend to create a realm ({url}): {e.Message}");
		}

		var payload = await res.Content.ReadAsStringAsync();
		if (!res.IsSuccessStatusCode)
		{
			// Already created by a previous run — not an error; the caller will log in to it.
			if ((int)res.StatusCode == 400 &&
			    (payload.Contains("InvalidAliasError") || payload.Contains("already being used")))
				return null;

			throw new CliException($"Realm creation failed ({(int)res.StatusCode}) at {url}: {Trim(payload)}");
		}

		JObject json;
		try { json = JObject.Parse(payload); }
		catch { throw new CliException($"Could not parse realm-creation response: {Trim(payload)}"); }

		var cid = json["cid"]?.ToString();
		var pid = json["pid"]?.ToString();
		var accessToken = json["token"]?["access_token"]?.ToString();
		var refreshToken = json["token"]?["refresh_token"]?.ToString();
		var alias = json["alias"]?.ToString() ?? o.alias;

		if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(pid))
			throw new CliException($"Realm-creation response missing cid/pid: {Trim(payload)}");

		var config = args.ConfigService;
		config.WriteConfigString(ConfigService.CFG_JSON_FIELD_HOST, host);
		config.WriteConfigString(ConfigService.CFG_JSON_FIELD_CID, cid);
		config.WriteConfigString(ConfigService.CFG_JSON_FIELD_PID, pid);
		config.SaveTokenToFile(new CliToken(accessToken, refreshToken, cid, pid));

		return new CreatedRealm { cid = cid, pid = pid, alias = alias };
	}

	/// <summary>
	/// Logs into an already-existing local realm: resolves the alias to a cid (scoping the requester at
	/// <paramref name="host"/>), logs in with the credentials, picks the realm, and persists cid/pid/host + token.
	/// </summary>
	private static async Task<CreatedRealm> LoginToExistingRealmAsync(CommandArgs args, string host, RealmSeedOptions o)
	{
		// Point the requester at the local host and resolve the alias → cid.
		try
		{
			await args.AppContext.Set(o.alias, string.Empty, host);
		}
		catch (Exception e)
		{
			throw new CliException($"Could not resolve local realm alias '{o.alias}' at {host}: {e.Message}");
		}

		TokenResponse resp;
		try
		{
			// customer-scoped (so we can then list the realms under the customer)
			resp = await args.AuthApi.Login(o.email, o.password, false, true);
		}
		catch (Exception e)
		{
			throw new CliException($"Could not log in to local realm '{o.alias}' as {o.email}: {e.Message}");
		}

		args.AppContext.SetToken(resp);
		var cid = args.AppContext.Cid;

		// Pick a realm under the customer — prefer a dev realm, else the first non-archived one.
		var games = await args.RealmsApi.GetGames();
		if (games == null || games.Count == 0)
			throw new CliException($"Local customer '{o.alias}' has no games/realms to select.");
		var realms = await args.RealmsApi.GetRealms(games[0]);
		var realm = realms?.FirstOrDefault(r => r.IsDev && !r.Archived)
		            ?? realms?.FirstOrDefault(r => !r.Archived)
		            ?? realms?.FirstOrDefault();
		if (realm == null)
			throw new CliException($"Local customer '{o.alias}' has no realms to select.");

		// Re-scope to the chosen realm and persist cid/pid/host + token.
		await args.AppContext.Set(cid, realm.Pid, host);
		var config = args.ConfigService;
		config.WriteConfigString(ConfigService.CFG_JSON_FIELD_HOST, host);
		config.WriteConfigString(ConfigService.CFG_JSON_FIELD_CID, cid);
		config.WriteConfigString(ConfigService.CFG_JSON_FIELD_PID, realm.Pid);
		config.SaveTokenToFile(new CliToken(resp.access_token, resp.refresh_token, cid, realm.Pid));

		Log.Information($"Logged in to local realm cid={cid} pid={realm.Pid} ({realm.ProjectName}).");
		return new CreatedRealm { cid = cid, pid = realm.Pid, alias = o.alias };
	}

	/// <summary>
	/// Returns true if the saved login still resolves against the local backend. Reuses the existing
	/// cid/pid + saved refresh token and refreshes it via <see cref="IAuthApi.LoginRefreshToken"/> (the same
	/// path the CLI uses at startup); a refreshed token is saved back. Returns false when there is nothing to
	/// reuse or the refresh fails (e.g. the realm was wiped).
	/// </summary>
	public static async Task<bool> IsLoginValidAsync(CommandArgs args)
	{
		var config = args.ConfigService;
		var cid = config.GetConfigString(ConfigService.CFG_JSON_FIELD_CID);
		var pid = config.GetConfigString(ConfigService.CFG_JSON_FIELD_PID);
		if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(pid))
			return false;

		if (!config.ReadTokenFromFile(out var token) || string.IsNullOrEmpty(token?.RefreshToken))
			return false;

		try
		{
			var resp = await args.AuthApi.LoginRefreshToken(token.RefreshToken);
			config.SaveTokenToFile(new CliToken(resp.access_token, resp.refresh_token, cid, pid));
			return true;
		}
		catch (Exception e)
		{
			Log.Verbose($"local login validation failed: {e.Message}");
			return false;
		}
	}

	private static string Trim(string s) => string.IsNullOrEmpty(s) || s.Length <= 300 ? s : s.Substring(0, 300);
}
