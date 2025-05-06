using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Newtonsoft.Json;

namespace cli;

public class CliToken : IAccessToken
{
	public const string CID_PROP = "cid";
	public const string PID_PROP = "pid";
	public const string ACCESS_TOKEN_PROP = "access_token";
	public const string REFRESH_TOKEN_PROP = "refresh_token";
	public const string EXPIRES_AT_PROP = "expires_at";

	public CliToken()
	{
		Token = RefreshToken = Cid = Pid = string.Empty;
		ExpiresAt = DateTime.Now + TimeSpan.FromMinutes(3);
	}

	public CliToken(TokenResponse response, string cid, string pid)
	{
		Token = response?.access_token ?? string.Empty;
		RefreshToken = response?.refresh_token ?? string.Empty;
		Cid = cid;
		Pid = pid;
		ExpiresAt = DateTime.Now + TimeSpan.FromMilliseconds(response?.expires_in ?? 0);
	}

	public CliToken(string accessToken, string refreshToken, string cid, string pid)
	{
		Token = accessToken;
		RefreshToken = refreshToken;
		Cid = cid;
		Pid = pid;
		ExpiresAt = DateTime.Now + TimeSpan.FromMinutes(3);
	}

	[JsonProperty(ACCESS_TOKEN_PROP)] public string Token { get; set; }
	[JsonProperty(REFRESH_TOKEN_PROP)] public string RefreshToken { get; set; }
	[JsonProperty(EXPIRES_AT_PROP)] public DateTime ExpiresAt { get; set; }
	[JsonProperty(CID_PROP)] public string Cid { get; set; }
	[JsonProperty(PID_PROP)] public string Pid { get; set; }
}
