using Beamable.Common.Api.Auth;
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
	/// Creates a local customer/realm/account via the backend and persists cid/pid/host + the returned token
	/// into the workspace config. Throws <see cref="CliException"/> if the backend call fails.
	/// </summary>
	public static async Task<CreatedRealm> CreateRealmAsync(CommandArgs args, string host, RealmSeedOptions o)
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
			throw new CliException($"Realm creation failed ({(int)res.StatusCode}) at {url}: {Trim(payload)}");

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

		Log.Information($"Created local realm cid={cid} pid={pid} alias={alias} and wrote it to the workspace config.");
		return new CreatedRealm { cid = cid, pid = pid, alias = alias };
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
