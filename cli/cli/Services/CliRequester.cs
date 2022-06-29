using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System.Text;

namespace cli;

public class CliRequester : IBeamableRequester
{
	private readonly IAppContext _ctx;
	public IAccessToken AccessToken => _ctx.Token;
	private string Pid => AccessToken.Pid;
	private string Cid => AccessToken.Cid;

	public CliRequester(IAppContext ctx)
	{
		_ctx = ctx;
	}
	public async Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false)
	{
		BeamableLogger.Log($"{method} call: {uri}");

		using HttpClient client = GetClient(includeAuthHeader, AccessToken?.Pid ?? Pid, AccessToken?.Cid ?? Cid, AccessToken);
		var request = PrepareRequest(method, _ctx.Host, uri, body);

		BeamableLogger.Log($"Calling: {request}");

		if (_ctx.IsDryRun)
		{
			BeamableLogger.Log($"DRYRUN ENABLED: NO NETWORKING ALLOWED.");
			return default(T);
		}

		var result = await client.SendAsync(request);

		BeamableLogger.Log($"RESULT: {result}");

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

	private static HttpRequestMessage PrepareRequest(Method method, string? basePath, string uri, object body = null)
	{
		var request = new HttpRequestMessage(FromMethod(method), basePath + uri);

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

	private static HttpClient GetClient(bool includeAuthHeader, string pid, string cid, IAccessToken? token)
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

}
