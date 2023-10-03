using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System.Text;

namespace cli;

public class CliRequester : IRequester
{
	private readonly IAppContext _ctx;
	public IAccessToken AccessToken => _ctx.Token;
	public string Pid => AccessToken.Pid;
	public string Cid => AccessToken.Cid;

	public CliRequester(IAppContext ctx)
	{
		_ctx = ctx;
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
		BeamableLogger.Log($"{method} call: {uri}");

		using HttpClient client = GetClient(includeAuthHeader, AccessToken?.Pid ?? Pid, AccessToken?.Cid ?? Cid, AccessToken, customerScoped);
		var request = PrepareRequest(method, _ctx.Host, uri, body);
		if (customHeaders != null)
		{
			foreach (string customHeader in customHeaders)
			{
				var headers = customHeader.Split('=');
				if (headers.Length == 2)
				{
					request.Headers.Add(headers[0], headers[1]);
				}
			}
		}

		CliSerilogProvider.Instance.Debug($"Calling: {request}");

		if (_ctx.IsDryRun)
		{
			BeamableLogger.Log($"DRYRUN ENABLED: NO NETWORKING ALLOWED.");
			return default(T);
		}

		var result = await client.SendAsync(request);

		CliSerilogProvider.Instance.Debug($"RESULT: {result}");

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
		return CustomRequest(method, uri, body, includeAuthHeader, parser, false).
		RecoverWith(error =>
		{
			switch (error)
			{
				case RequesterException e when e.RequestError.error is "TimeOutError":
					BeamableLogger.LogWarning("Timeout error, retrying in few seconds... ");
					return Task.Delay(TimeSpan.FromSeconds(5)).ToPromise().FlatMap(_ =>
							Request<T>(method, uri, body, includeAuthHeader, parser, useCache));
				case RequesterException e when e.RequestError.error is "InvalidTokenError" or "ExpiredTokenError" ||
											   e.Status == 403 ||
											   (!string.IsNullOrWhiteSpace(AccessToken.RefreshToken) &&
												AccessToken.ExpiresAt < DateTime.Now):
					BeamableLogger.Log(
						"Got failure for token " + AccessToken.Token + " because " + e.RequestError.error);
					var authService = new AuthApi(this);
					return authService.LoginRefreshToken(AccessToken.RefreshToken).Map(rsp =>
						{
							BeamableLogger.Log(
								$"Got new token: access=[{rsp.access_token}] refresh=[{rsp.refresh_token}] type=[{rsp.token_type}] ");
							_ctx.UpdateToken(rsp);
							return PromiseBase.Unit;
						})
						.FlatMap(_ => Request<T>(method, uri, body, includeAuthHeader, parser, useCache));
				case RequesterException { Status: > 500 and < 510 }:
					BeamableLogger.LogWarning($"Problems with host {_ctx.Host}, trying again in few seconds...");
					return Task.Delay(TimeSpan.FromSeconds(5)).ToPromise().FlatMap(_ =>
						Request<T>(method, uri, body, includeAuthHeader, parser, useCache));
			}

			return Promise<T>.Failed(error);
		});
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
		}
		else
		{
			var ss = JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance);
			request.Content = new StringContent(ss, Encoding.UTF8, "application/json");
		}
		return request;
	}

	private static HttpClient GetClient(bool includeAuthHeader, string pid, string cid, IAccessToken token, bool customerScoped)
	{
		var client = new HttpClient();
		client.DefaultRequestHeaders.Add("contentType", "application/json"); // confirm that it is required
		client.DefaultRequestHeaders.Add("X-DE-SCOPE", customerScoped ? cid : $"{cid}.{pid}");

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
