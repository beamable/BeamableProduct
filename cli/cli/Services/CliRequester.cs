using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System.Text;
using Beamable.Server;
using System.Net.Http.Headers;
using TokenResponse = Beamable.Common.Api.Auth.TokenResponse;

namespace cli;

public class CliRequester : IRequester
{

	private readonly int ProgressiveDelayIncreaser = 5;
	private readonly IRequesterInfo _requesterInfo;
	
	public IAccessToken AccessToken => _requesterInfo.Token;
	public string Pid => AccessToken.Pid;
	public string Cid => AccessToken.Cid;

	public Dictionary<string, string> GlobalHeaders { get; } = new Dictionary<string, string>();

	public CliRequester(IRequesterInfo requesterInfo)
	{
		_requesterInfo = requesterInfo;
	}

	public async Promise<T> RequestJson<T>(Method method, string uri, JsonSerializable.ISerializable body,
		bool includeAuthHeader = true)
	{
		var jsonFields = JsonSerializable.Serialize(body);
		var json = Json.Serialize(jsonFields, new StringBuilder());

		return await CustomRequest<T>(method, uri, json, includeAuthHeader);
	}

	public async Promise<T> CustomRequest<T>(Method method, string uri, object body = null, bool includeAuthHeader = true,
										  Func<string, T> parser = null, bool customerScoped = false, IEnumerable<string> customHeaders = null)
	{
		Log.Verbose($"{method} call: {uri}");
		
		using HttpClient client = GetClient(_requesterInfo.Host, includeAuthHeader, AccessToken?.Pid ?? Pid, AccessToken?.Cid ?? Cid, AccessToken, customerScoped);
		var request = PrepareRequest(method, _requesterInfo.Host, uri, body);
		
		if (GlobalHeaders != null)
		{
			foreach (var kvp in GlobalHeaders)
			{
				request.Headers.Add(kvp.Key, kvp.Value);
			}
		}
		
		if (customHeaders != null)
		{
			foreach (string customHeader in customHeaders)
			{
				var headers = customHeader.Split('=');
				var key = headers[0];
				var value = headers[1];
				if (headers.Length != 2)
					continue;
				
				if (request.Headers.Contains(key))
				{
					request.Headers.Remove(key);
				}
				request.Headers.Add(key, value);
			}
		}


		Log.Verbose($"Calling: {request}");

		if (_requesterInfo.IsDryRun)
		{
			Log.Verbose($"DRYRUN ENABLED: NO NETWORKING ALLOWED.");
			return default(T);
		}

		var result = await client.SendAsync(request);

		Log.Verbose($"RESULT: {result}");

		if (!result.IsSuccessStatusCode)
		{
			await using Stream stream = await result.Content.ReadAsStreamAsync();
			using var reader = new StreamReader(stream, Encoding.UTF8);
			var rawResponse = await reader.ReadToEndAsync();
			throw new RequesterException("Cli", method.ToReadableString(), uri, (int)result.StatusCode, rawResponse);
		}

		T parsed;
		{
			await using Stream stream = await result.Content.ReadAsStreamAsync();
			using var reader = new StreamReader(stream, Encoding.UTF8);
			var rawResponse = await reader.ReadToEndAsync();
			Log.Verbose($"RESPONSE BODY:\n{rawResponse}");

			if (typeof(T) == typeof(string) && rawResponse is T response)
			{
				return response;
			}
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

	public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false)
	{
		return InternalRequest<T>(method, uri, body, includeAuthHeader, parser, useCache, 0);
	}

	private Promise<T> InternalRequest<T>(Method method, string uri, object body = null, bool includeAuthHeader = true,
		Func<string, T> parser = null,
		bool useCache = false, int retryCount = 0)
	{
		return CustomRequest(method, uri, body, includeAuthHeader, parser, false).RecoverWith(error =>
		{
			switch (error)
			{
				case RequesterException e when e.Status == 401:
					Log.Warning(
						$"Unauthorized access with token: [{AccessToken.Token}], please make sure you're logged in");

					if (retryCount >= 1 || string.IsNullOrEmpty(AccessToken.RefreshToken))
					{
						break;
					}

					return GetTokenAndRetry<T>(method, uri, body, includeAuthHeader, parser, useCache, retryCount);
				case RequesterException e when e.RequestError.error is "TimeOutError":
					BeamableLogger.LogWarning("Timeout error, retrying in few seconds... ");
					return Task.Delay(TimeSpan.FromSeconds(ProgressiveDelayIncreaser * (retryCount + 1))).ToPromise().FlatMap(_ =>
						Request<T>(method, uri, body, includeAuthHeader, parser, useCache));
				case RequesterException e when e.RequestError.error is "ExpiredTokenError" ||
				                               e.Status == 403 ||
				                               (!string.IsNullOrWhiteSpace(AccessToken.RefreshToken) &&
				                                AccessToken.ExpiresAt < DateTime.Now):
					Log.Debug(
						"Got failure for token " + AccessToken.Token + " because " + e.RequestError.error);

					if (retryCount >= 1 || string.IsNullOrEmpty(AccessToken.RefreshToken))
					{
						break;
					}

					return GetTokenAndRetry<T>(method, uri, body, includeAuthHeader, parser, useCache, retryCount);
				case RequesterException e when e.Status == 502:
					BeamableLogger.LogWarning(
						$"Problems with host {_requesterInfo.Host}. Got a [{e.Status}] and Message = [{e.Message}]");
					if (retryCount >= 5 || string.IsNullOrEmpty(AccessToken.RefreshToken))
					{
						break;
					}

					return GetTokenAndRetry<T>(method, uri, body, includeAuthHeader, parser, useCache, retryCount);
				case RequesterException e when e.Status > 500 && e.Status < 510:
					BeamableLogger.LogWarning(
						$"Problems with host {_requesterInfo.Host}. Got a [{e.Status}] and Message = [{e.Message}]");
					break;
			}

			return Promise<T>.Failed(error);
		}).ReportInnerException();
	}

	private async Promise<T> GetTokenAndRetry<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false, int retryCount = 0)
	{
		var authService = new AuthApi(this);

		TokenResponse tokenResponse = await authService.LoginRefreshToken(AccessToken.RefreshToken);
		Log.Debug(
			$"Got new token: access=[{tokenResponse.access_token}] refresh=[{tokenResponse.refresh_token}] type=[{tokenResponse.token_type}] ");
		
		_requesterInfo.SetToken(tokenResponse);
		_requesterInfo.SaveCurrentTokenToFile();
		
		return await InternalRequest<T>(method, uri, body, includeAuthHeader, parser, useCache, ++retryCount);
	}

	private static HttpRequestMessage PrepareRequest(Method method, string basePath, string uri, object body = null)
	{
		var address = uri.Contains("://") ? uri : $"{basePath}{uri}";
		var request = new HttpRequestMessage(FromMethod(method), address);

		if (body == null)
		{
			return request;
		}

		if (body is string s)
		{
			byte[] bodyBytes = Encoding.UTF8.GetBytes(s);
			request.Content = new ByteArrayContent(bodyBytes);
			request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
		}
		else
		{
			var ss = JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance);
			request.Content = new StringContent(ss, Encoding.UTF8, "application/json");
		}
		return request;
	}

	static string GetSuffixFilter(string host)
	{
		if (string.IsNullOrEmpty(host) || host.Contains("localhost"))
		{
			return ".beamable.com";
		}

		var hostLike = host.Replace("dev.api", "dev-api");
		var filter = "." + string.Join(".", hostLike
			.Split(".")
			.Skip(1));
			   
		// trim off any pathing in the host 
		int idx = filter.IndexOf('/');
		if (idx != -1)
			filter = filter.Substring(0, idx);
		return filter;
	}
	
	private static HttpClient GetClient(string host, bool includeAuthHeader, string pid, string cid, IAccessToken token, bool customerScoped)
	{
		var handler = new HttpClientHandler();
		handler.ServerCertificateCustomValidationCallback = (message, _, _, _) =>
		{
			if (message?.RequestUri == null) return false;
			
			var filter = GetSuffixFilter(host);
			var isBeamableEndpoint = message.RequestUri.Host.EndsWith(filter);
			var isContentRelated = message.RequestUri.Fragment.Contains("content/", StringComparison.InvariantCultureIgnoreCase);

			var shouldAvoidSslDueToContentInfra = isBeamableEndpoint && isContentRelated;
			if (shouldAvoidSslDueToContentInfra)
			{
				return false;
			}
			
			return true;
		};

		var client = new HttpClient(handler);

		client.DefaultRequestHeaders.Add("contentType", "application/json"); // confirm that it is required

		var scope = string.IsNullOrEmpty(pid) ? cid : $"{cid}.{pid}";
		if (!string.IsNullOrEmpty(scope))
		{
			client.DefaultRequestHeaders.Add("X-BEAM-SCOPE", scope);
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

	public Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req)
	{
		return Request<T>(req.Method, req.uri, req.body, req.includeAuthHeader, req.parser, req.useCache);
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
