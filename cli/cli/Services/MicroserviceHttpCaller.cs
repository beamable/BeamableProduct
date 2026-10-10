using System.Text;

namespace cli.Services;

public class MicroserviceHttpResponse
{
	public int status;
	public string body;
}

/// <summary>
/// Sends a raw HTTP request with caller-chosen headers and returns the status and body without
/// throwing on a non-2xx status. <see cref="CliRequester"/> always authenticates as the CLI user,
/// so it cannot make a request as a player; <c>beam project call</c> uses this instead.
/// Methods are virtual so tests can substitute the transport.
/// </summary>
public class MicroserviceHttpCaller
{
	public virtual async Task<MicroserviceHttpResponse> Send(HttpMethod method, string url, string jsonBody,
		IReadOnlyDictionary<string, string> headers, CancellationToken token = default)
	{
		using var client = new HttpClient();
		using var request = new HttpRequestMessage(method, url);
		if (headers != null)
		{
			foreach (var kvp in headers)
			{
				request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
			}
		}

		if (jsonBody != null)
		{
			request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
		}

		using var response = await client.SendAsync(request, token);
		var body = await response.Content.ReadAsStringAsync(token);
		return new MicroserviceHttpResponse { status = (int)response.StatusCode, body = body };
	}
}
