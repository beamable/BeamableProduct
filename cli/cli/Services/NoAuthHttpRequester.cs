using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System.Text;
using Beamable.Server;

namespace cli;

public delegate string HostFunction();


public class NoAuthHttpRequester : IRequester
{
	private const string CONTENT_TYPE = "application/json";
	private const string BASE_ADDRESS = "https://api.beamable.com";
	private readonly HttpClient _client;
	private readonly HostFunction _config;

	private string Host => _config?.Invoke();

	public NoAuthHttpRequester(ConfigService config)
	{
		_client = new HttpClient();
		_config = () => config.GetConfigString2(ConfigService.CFG_JSON_FIELD_HOST);
	}
	
	public NoAuthHttpRequester(string host)
	{
		_client = new HttpClient();
		_config = () => host;
	}

	public string EscapeURL(string url)
	{
		return System.Web.HttpUtility.UrlEncode(url);
	}

	public IAccessToken AccessToken => throw new NotImplementedException($"{nameof(NoAuthHttpRequester)} does not support access tokens as it doesn't require Authentication");
	public string Cid => throw new NotImplementedException($"{nameof(NoAuthHttpRequester)} does not have a CID as it doesn't require Authentication");
	public string Pid => throw new NotImplementedException($"{nameof(NoAuthHttpRequester)} does not have a PID as it doesn't require Authentication");

	public async Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false)
	{
		uri = Host + uri;
		var req = new HttpRequestMessage();
		req.Method = method switch
		{
			Method.GET => HttpMethod.Get,
			Method.PUT => HttpMethod.Put,
			Method.POST => HttpMethod.Post,
			Method.DELETE => HttpMethod.Delete,
			_ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
		};
		req.RequestUri = new Uri(uri);
		Log.Verbose($"no auth request uri=[{uri}]");

		if (body is string bodyStr)
		{
			req.Content = new StringContent(bodyStr, Encoding.UTF8, CONTENT_TYPE);
		}
		else if (body != null)
		{
			var json = JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance);
			req.Content = new StringContent(json, Encoding.UTF8, CONTENT_TYPE);
		}


		var res = await _client.SendAsync(req);

		var resBody = await res.Content.ReadAsStringAsync();

		if (!res.IsSuccessStatusCode)
		{
			throw new RequesterException("no-auth-http", method.ToString(), uri, (long)res.StatusCode,
				new BeamableRequestError { service = "", status = (long)res.StatusCode, error = res.ReasonPhrase, message = resBody }
			);
		}

		T result;
		if (parser == null)
		{
			result = JsonConvert.DeserializeObject<T>(resBody, UnitySerializationSettings.Instance);
		}
		else
		{
			result = parser(resBody);
		}

		return result;
	}

	public IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
	{
		throw new NotImplementedException($"{nameof(NoAuthHttpRequester)} does not support accepting a new access token as it doesn't require Authentication");
	}

	public Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req)
	{
		return Request<T>(req.Method, req.uri, req.body);
	}
}
