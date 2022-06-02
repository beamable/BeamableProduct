using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using System.Net;

namespace cli;

public class CliRequester : IBeamableRequester
{
	public IAccessToken AccessToken { get; private set;  }

	public CliRequester()
	{
	}

	public void UpdateToken(TokenResponse response, string cid, string pid) =>
		AccessToken = new CliToken(response, cid, pid);
	
	public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false)
	{
		Console.WriteLine($"Would have sent a {method} to {uri} but not implemented yet");
		// throw new NotImplementedException();

		return Promise<T>.Successful(default(T));
	}

	public IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
	{
		throw new NotImplementedException();
	}

	public string EscapeURL(string url)
	{
		// TODO: Fix this/
		return url;
	}
}
