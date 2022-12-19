
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace beamWasm.BeamBlazor;

[Serializable]
public class AccessToken : IAccessToken
{
	public AccessToken()
	{
		Token = RefreshToken = Cid = Pid = string.Empty;
		ExpiresAt = DateTime.FromFileTimeUtc(0);
	}
	public AccessToken(TokenResponse response, string cid, string pid)
	{
		Token = response?.access_token ?? string.Empty;
		RefreshToken = response?.refresh_token ?? string.Empty;
		Cid = cid;
		Pid = pid;
		ExpiresAt = DateTime.FromFileTimeUtc(response?.expires_in ?? 0);
	}

	public AccessToken(string accessToken, string refreshToken, string cid, string pid)
	{
		Token = accessToken;
		RefreshToken = refreshToken;
		Cid = cid;
		Pid = pid;
		ExpiresAt = DateTime.Now + TimeSpan.FromMinutes(3);
	}
	public string Token { get; set; }
	public string RefreshToken { get; set; }
	public DateTime ExpiresAt { get; }
	public string Cid { get; set; }
	public string Pid { get; set; }
}
