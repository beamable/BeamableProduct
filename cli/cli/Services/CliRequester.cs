using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using System.Text;
using Beamable.Server.Common;
using Newtonsoft.Json;

namespace cli;

public class CliRequester : IBeamableRequester
{
	private const string BASE_PATH = "https://dev.api.beamable.com";
	public IAccessToken AccessToken => Token;
	private CliToken Token { get; set; }
	private string Pid { get; set; }
	private string Cid { get; set; }

	public CliRequester()
	{
		Token = null;
	}

	public void UpdateToken(TokenResponse response) =>
		Token = new CliToken(response, Cid, Pid);

	public async Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false)
	{
		Console.WriteLine($"{method} call: {uri}");
		using HttpClient client = GetClient(includeAuthHeader, Token?.Pid ?? Pid, Token?.Cid ?? Cid, Token);
		var request = PrepareRequest(method, uri, body);

		Console.WriteLine($"Calling: {request}");
		var result = await client.SendAsync(request);
		Console.WriteLine($"RESULT: {result}");

		T parsed = default(T);
		if (result.Content != null)
		{
			await using Stream stream = await result.Content.ReadAsStreamAsync();
			using var reader = new StreamReader(stream, Encoding.UTF8);
			var rawResponse = await reader.ReadToEndAsync();
			if (parser != null)
			{
				// if there is a custom parser, use that.
				parsed = parser(rawResponse);
			}
			else
			{
				// otherwise use JSON
				parsed = JsonConvert.DeserializeObject<T>(rawResponse, UnitySerializationSettings.Instance);
			}
		}
		return parsed;
	}

	private static HttpRequestMessage PrepareRequest(Method method, string uri, object body = null)
	{
		var request = new HttpRequestMessage(FromMethod(method), BASE_PATH + uri);

		if (body == null)
		{
			return request;
		}

		if (body is string s)
		{
			byte[] bodyBytes = Encoding.UTF8.GetBytes(s);
			request.Content = new ByteArrayContent(bodyBytes);
		}
		else
		{
			var ss = JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance);
			request.Content = new StringContent(ss, Encoding.UTF8, "application/json");
		}
		return request;
	}

	private static HttpClient GetClient(bool includeAuthHeader, string pid, string cid, CliToken? token)
	{
		var client = new HttpClient();
		client.DefaultRequestHeaders.Add("contentType", "application/json"); // confirm that it is required
		
		if (!string.IsNullOrEmpty(cid))
		{
			client.DefaultRequestHeaders.Add("X-KS-CLIENTID", cid);
		}
		if (!string.IsNullOrEmpty(pid))
		{
			client.DefaultRequestHeaders.Add("X-KS-PROJECTID", pid);
		}

		if (includeAuthHeader && !string.IsNullOrWhiteSpace(token?.Token))
		{
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
		}

		return client;
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

	public void SetPidAndCid(string cid, string pid)
	{
		Cid = cid;
		Pid = pid;
	}
}
