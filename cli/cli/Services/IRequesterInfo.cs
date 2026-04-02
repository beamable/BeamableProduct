using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public interface IRequesterInfo
{
	IAccessToken Token { get; }
	string Host { get; }
	bool IsDryRun { get; }
	/// <summary>
	/// Sets the active token that we use to make authenticated requests. Again, only at runtime. This does not affect the files inside the '.beamable' folder.
	/// </summary>
	void SetToken(TokenResponse tokenResponse);
	
	/// <summary>
	/// Persist the current token to the file system. 
	/// </summary>
	/// <param name="tokenResponse"></param>
	void SaveCurrentTokenToFile();
}

public class RequesterInfo : IRequesterInfo
{
	public IAccessToken Token => _token;
	public string Host => _host;
	public bool IsDryRun => false;

	private IAccessToken _token;
	public string _host;

	public RequesterInfo(string cid, string pid, string accessToken, string refreshToken, string host)
	{
		_token = new CliToken(accessToken, refreshToken, cid, pid);
		_host = host;
	}
	
	public void SetToken(TokenResponse tokenResponse)
	{
		_token = new CliToken(tokenResponse, _token.Cid, _token.Pid);
	}

	public void SaveCurrentTokenToFile()
	{
		throw new NotImplementedException();
	}
}
