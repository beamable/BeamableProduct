using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Beamable.Server;

public class MicroserviceHttpRequester : IHttpRequester
{
	private readonly HttpClient _client;

	public MicroserviceHttpRequester(HttpClient client)
	{
		_client = client;
	}
	
	public async Promise<T> ManualRequest<T>(Method method, string url, object body = null, Dictionary<string, string> headers = null,
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

	public string EscapeURL(string url)
	{
		return System.Web.HttpUtility.UrlEncode(url);
	}
}
