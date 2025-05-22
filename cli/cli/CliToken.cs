using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Newtonsoft.Json;
namespace cli;

public class CliToken : IAccessToken
{
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
	[JsonProperty("access_token")]
	public string Token { get; set; }
	[JsonProperty("refresh_token")]
	public string RefreshToken { get; set; }
	[JsonProperty("expires_at")]
	public DateTime ExpiresAt { get; set; }
	[JsonProperty("cid")]
	public string Cid { get; set; }
	[JsonProperty("pid")]
	public string Pid { get; set; }
}
