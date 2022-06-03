using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
		// Console.WriteLine($"Would have sent a {method} to {uri} but not implemented yet");
		Console.WriteLine($"{method} call: {uri}");
		// throw new NotImplementedException();
		var client = new HttpClient();
		var request = new HttpRequestMessage(FromMethod(method), BASE_PATH + uri);
		request.Headers.Add("contentType", "application/json");
		if (body != null)
		{
			if (body is string s)
			{
				byte[] bodyBytes = Encoding.UTF8.GetBytes(s);
				request.Content = new ByteArrayContent(bodyBytes);
			}
			else
			{
				string ss = JsonSerializer.Serialize(body, new JsonSerializerOptions(){ IncludeFields = true});
				request.Content = new StringContent(ss, Encoding.UTF8, "application/json");
			}
		}
		
		if (!string.IsNullOrEmpty(Token?.Cid))
		{
			client.DefaultRequestHeaders.Add("X-KS-CLIENTID", Token.Cid);
		}
		if (!string.IsNullOrEmpty(Token?.Pid))
		{
			client.DefaultRequestHeaders.Add("X-KS-PROJECTID", Token.Pid);
		}

		if (includeAuthHeader && !string.IsNullOrWhiteSpace(Token?.Token))
		{
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken.Token}");
		}
		Console.WriteLine($"Calling: {request.ToString()}");
		var result = client.Send(request);
		Console.WriteLine("RESULT: " + result.ToString());
		if (result.Content != null)
		{
			var stream = result.Content.ReadAsStream();
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
			{
				Console.WriteLine($"Content: {reader.ReadToEnd()}");
			}
		}
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

	private static HttpMethod FromMethod(Method method)
	{
		return method switch
		{
			Method.GET => HttpMethod.Get,
			Method.POST => HttpMethod.Post,
			Method.PUT => HttpMethod.Put,
			Method.DELETE => HttpMethod.Delete,
			_ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
		};
	}
}
