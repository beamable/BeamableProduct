using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using System.Net;

namespace cli;

public class CliRequester : IBeamableRequester
{
	private const string BASE_PATH = "https://dev.api.beamable.com";
	public IAccessToken AccessToken => Token;
	private CliToken Token { get; set; }

	public CliRequester()
	{
		Token = null;
	}

	public void UpdateToken(TokenResponse response, string cid, string pid) =>
		Token = new CliToken(response, cid, pid);
	
	public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false)
	{
		Console.WriteLine($"Would have sent a {method} to {uri} but not implemented yet");
		// throw new NotImplementedException();
		var client = new HttpClient();
		client.DefaultRequestHeaders.Add("contentType", "application/json");
		var request = new HttpRequestMessage(HttpMethod.Get, BASE_PATH + uri);
		request.Headers.Add("contentType", "application/json");
		if (body != null)
		{
			// request.Content = new 
		}

		if (includeAuthHeader && !string.IsNullOrWhiteSpace(Token?.Token))
		{
			request.Headers.Add("Authorization", $"Bearer {AccessToken.Token}");
		}
		var result = client.Send(request);
		Console.WriteLine("RESULT: " + result.ToString());
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
