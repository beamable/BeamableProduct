using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class CliToken : IAccessToken
{
	public CliToken(TokenResponse response, string cid, string pid)
	{
		Token = response.access_token;
		RefreshToken = response.refresh_token;
		Cid = cid;
		Pid = pid;
		ExpiresAt = DateTime.FromFileTimeUtc(response.expires_in);
	}
	public string Token { get; }
	public string RefreshToken { get; }
	public DateTime ExpiresAt { get; }
	public string Cid { get; }
	public string Pid { get; }
}
