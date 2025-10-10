using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Beamable.Common.Content;
using NotImplementedException = System.NotImplementedException;

namespace Beamable.Server;

public class MicroserviceHttpRequester : IHttpRequester, IRequester
{
	private readonly HttpClient _client;
	private readonly string _baseAddr;
	public OptionalString ScopeHeader = new OptionalString();

	public MicroserviceHttpRequester(IMicroserviceArgs args, HttpClient client)
	{
		_client = client;
		_baseAddr = args.Host
			.Replace("ws://", "http://")
			.Replace("wss://", "https://")
			.Replace("/socket/", "")
			.Replace("/socket", "");
	}
	
	public virtual async Promise<T> ManualRequest<T>(Method method, string url, object body = null, Dictionary<string, string> headers = null,
		string contentType = "application/json", Func<string, T> parser = null)
	{
		var req = new HttpRequestMessage();
		req.Method = method switch
		{
			Method.GET => HttpMethod.Get,
			Method.PUT => HttpMethod.Put,
			Method.POST => HttpMethod.Post,
			Method.DELETE => HttpMethod.Delete,
			_ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
		};
		req.RequestUri = new Uri(url);

		if (headers != null)
		{
			foreach (var header in headers)
			{
				req.Headers.Add(header.Key, header.Value);
			}
		}

		if (ScopeHeader.TryGet(out var scope))
		{
			req.Headers.Add("X-BEAM-SCOPE", scope);
		}

		if (body is string bodyStr)
		{
			req.Content = new StringContent(bodyStr, Encoding.UTF8, contentType);
		}
		else if (body != null)
		{
			var json = JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance);
			req.Content = new StringContent(json, Encoding.UTF8, contentType);
		}

		
		var res = await _client.SendAsync(req);

		var resBody = await res.Content.ReadAsStringAsync();
		
		if (!res.IsSuccessStatusCode)
		{
			throw new RequesterException("microservice-http", method.ToString(), url, (long)res.StatusCode,
				new BeamableRequestError
				{
					service = "",
					status = (long)res.StatusCode,
					error = res.ReasonPhrase,
					message = resBody
				}
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

	public IAccessToken AccessToken =>
		throw new NotImplementedException($"{nameof(MicroserviceHttpRequester)} does not support access tokens");
	public string Cid => throw new NotImplementedException($"{nameof(MicroserviceHttpRequester)} does not contain cid");

	public string Pid => throw new NotImplementedException($"{nameof(MicroserviceHttpRequester)} does not contain pid");


	public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
		bool useCache = false)
	{
		return BeamableRequest<T>(new SDKRequesterOptions<T>
		{
			method = method,
			uri = uri,
			body = body,
			includeAuthHeader = includeAuthHeader,
			parser = parser,
			disableScopeHeaders = true,
			useCache = false,
			useConnectivityPreCheck = false
		});
	}

	public IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
	{
		throw new NotImplementedException($"{nameof(MicroserviceHttpRequester)} does not support accepting a new access token");
	}

	public string EscapeURL(string url)
	{
		return System.Web.HttpUtility.UrlEncode(url);
	}

	public virtual Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req)
	{
		var uri = _baseAddr + req.uri;
		return ManualRequest<T>(req.method, uri, req.body);
	}
}
